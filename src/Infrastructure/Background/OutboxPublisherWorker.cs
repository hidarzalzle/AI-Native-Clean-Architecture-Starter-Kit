using Application.Abstractions;
using Infrastructure.Observability;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Common;

namespace Infrastructure.Background;

public class OutboxPublisherWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

                var pending = await db.OutboxSet
                    .Where(x => x.PublishedAtUtc == null && (!x.IsProcessing || (x.ProcessingStartedAtUtc != null && x.ProcessingStartedAtUtc < DateTime.UtcNow.AddMinutes(-5))) && x.Attempts < 10 && x.NextAttemptAtUtc <= DateTime.UtcNow)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    var claimed = await db.OutboxSet
                        .Where(x => x.Id == msg.Id && x.PublishedAtUtc == null && (!x.IsProcessing || (x.ProcessingStartedAtUtc != null && x.ProcessingStartedAtUtc < DateTime.UtcNow.AddMinutes(-5))))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.IsProcessing, true)
                            .SetProperty(x => x.ProcessingStartedAtUtc, DateTime.UtcNow), stoppingToken);

                    if (claimed == 0)
                    {
                        continue;
                    }

                    try
                    {
                        using var activity = AppDiagnostics.ActivitySource.StartActivity("outbox.publish");
                        activity?.SetTag("outbox.messageType", msg.Type);
                        await publisher.PublishAsync(msg.Type, msg.Payload, stoppingToken);
                        msg.PublishedAtUtc = DateTime.UtcNow;
                        msg.IsProcessing = false;
                        msg.ProcessingStartedAtUtc = null;
                        msg.NextAttemptAtUtc = DateTime.UtcNow;
                        AppMetrics.OutboxPublishedTotal.Add(1, new KeyValuePair<string, object?>("outbox.messageType", msg.Type));
                    }
                    catch (Exception ex)
                    {
                        msg.Attempts += 1;
                        msg.LastError = ex.Message;
                        msg.IsProcessing = false;
                        msg.ProcessingStartedAtUtc = null;
                        var backoffSeconds = Math.Min(300, (int)Math.Pow(2, Math.Min(msg.Attempts, 8)));
                        msg.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);
                        logger.LogWarning(ex, "Outbox publish failed {MessageId}", msg.Id);
                        AppMetrics.OutboxFailedTotal.Add(1, new KeyValuePair<string, object?>("outbox.messageType", msg.Type));
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("OutboxPublisherWorker cancellation requested. Stopping gracefully.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxPublisherWorker loop failed. Retrying in 5 seconds.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}

using System.Text.Json;
using Application.Abstractions;
using Domain.Enums;
using MediatR;
using SharedKernel.Common;

namespace Application.Commands;

public record ClassifyTicketWithAiCommand(Guid TicketId) : IRequest;

public class ClassifyTicketWithAiHandler(IApplicationDbContext db, IAiClient aiClient, IClock clock, IOutboxWriter outboxWriter) : IRequestHandler<ClassifyTicketWithAiCommand>
{
    public async Task Handle(ClassifyTicketWithAiCommand request, CancellationToken ct)
    {
        using var activity = AppDiagnostics.ActivitySource.StartActivity("ticket.classify.ai");
        var ticket = db.Tickets.FirstOrDefault(x => x.Id == request.TicketId)
            ?? throw new InvalidOperationException("Ticket not found.");

        activity?.SetTag("ticket.id", ticket.Id.ToString());

        var ai = await ExecuteWithRetryAsync(
            action: async retryCt =>
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(retryCt);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
                return await aiClient.ClassifyTicketAsync(ticket.Title, ticket.Description, timeoutCts.Token);
            },
            maxAttempts: 3,
            initialDelay: TimeSpan.FromMilliseconds(200),
            ct);

        activity?.SetTag("ai.provider", ai.Provider);
        activity?.SetTag("ai.model", ai.Model);

        var category = Enum.Parse<TicketCategory>(ai.Category, true);
        var priority = Enum.Parse<TicketPriority>(ai.Priority, true);
        ticket.Classify(category, priority, clock.UtcNow);

        var requestJson = JsonSerializer.Serialize(new
        {
            title = ticket.Title,
            description = ticket.Description,
            promptVersion = "v1"
        });

        var responseJson = JsonSerializer.Serialize(new
        {
            category = ai.Category,
            priority = ai.Priority,
            confidence = ai.Confidence,
            rationale = ai.Rationale
        });

        await db.AddAiAuditAsync(
            new AiAuditLog(
                Guid.NewGuid(),
                ticket.Id,
                ai.Provider,
                ai.Model,
                "v1",
                requestJson,
                responseJson,
                ai.PromptTokens,
                ai.CompletionTokens,
                clock.UtcNow),
            ct);

        await outboxWriter.WriteDomainEventsAsync(ticket.DomainEvents, ct);
        await db.SaveChangesAsync(ct);
        ticket.ClearDomainEvents();
    }

    private static async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> action, int maxAttempts, TimeSpan initialDelay, CancellationToken ct)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action(ct);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                last = ex;
                var delay = TimeSpan.FromMilliseconds(initialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, ct);
            }
        }

        throw new InvalidOperationException("AI classification failed after retries.", last);
    }
}

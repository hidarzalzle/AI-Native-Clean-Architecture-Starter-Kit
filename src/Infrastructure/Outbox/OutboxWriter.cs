using System.Text.Json;
using Application.Abstractions;

namespace Infrastructure.Outbox;

public class OutboxWriter(IApplicationDbContext db, IClock clock) : IOutboxWriter
{
    public async Task WriteDomainEventsAsync(IEnumerable<object> domainEvents, CancellationToken ct)
    {
        foreach (var @event in domainEvents)
        {
            await db.AddOutboxAsync(new OutboxMessage(Guid.NewGuid(), @event.GetType().Name, JsonSerializer.Serialize(@event), clock.UtcNow, null, 0, null), ct);
        }
    }
}

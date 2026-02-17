using Application.Abstractions;
using MediatR;

namespace Application.Commands;

public record ReceiveWebhookCommand(string Provider, string EventId, string Payload, string Signature, DateTime TimestampUtc) : IRequest<bool>, IIdempotentCommand
{
    public string IdempotencyKey => $"webhook:{Provider}:{EventId}";
}

public class ReceiveWebhookHandler(IApplicationDbContext db, IWebhookVerifier verifier, ICacheService cache, IClock clock) : IRequestHandler<ReceiveWebhookCommand, bool>
{
    public async Task<bool> Handle(ReceiveWebhookCommand request, CancellationToken ct)
    {
        if (!verifier.Verify(request.Provider, request.Payload, request.Signature, request.TimestampUtc))
            throw new UnauthorizedAccessException("Invalid signature");

        var first = await cache.TrySetIdempotencyAsync(request.IdempotencyKey, TimeSpan.FromHours(24), ct);
        if (!first) return false;

        await db.AddWebhookReceiptAsync(new WebhookReceipt(Guid.NewGuid(), request.Provider, request.EventId, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.Payload))), clock.UtcNow, null), ct);
        await db.AddOutboxAsync(new OutboxMessage(Guid.NewGuid(), "webhook.received", request.Payload, clock.UtcNow, null, 0, null), ct);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

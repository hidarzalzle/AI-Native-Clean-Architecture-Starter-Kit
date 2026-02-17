using Domain.Entities;
using MediatR;

namespace Application.Abstractions;

public interface IApplicationDbContext
{
    IQueryable<Ticket> Tickets { get; }
    IQueryable<OutboxMessage> OutboxMessages { get; }
    IQueryable<WebhookReceipt> WebhookReceipts { get; }
    IQueryable<AiAuditLog> AiAuditLogs { get; }
    Task AddTicketAsync(Ticket ticket, CancellationToken ct);
    Task AddOutboxAsync(OutboxMessage message, CancellationToken ct);
    Task AddWebhookReceiptAsync(WebhookReceipt receipt, CancellationToken ct);
    Task AddAiAuditAsync(AiAuditLog auditLog, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface IClock { DateTime UtcNow { get; } }

public interface IAiClient
{
    Task<AiClassificationResult> ClassifyTicketAsync(string title, string description, CancellationToken ct);
    Task<IReadOnlyCollection<AiToolCall>> InvokeWithToolsAsync(string prompt, IReadOnlyCollection<AiToolDefinition> tools, CancellationToken ct);
}

public interface IMessagePublisher
{
    Task PublishAsync(string messageType, string payload, CancellationToken ct);
}

public interface IExternalHttpClient
{
    Task<string> GetStringAsync(string url, CancellationToken ct);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

public interface ICacheService
{
    Task<bool> TrySetIdempotencyAsync(string key, TimeSpan ttl, CancellationToken ct);
}

public interface IWebhookVerifier
{
    bool Verify(string provider, string payload, string signatureHeader, DateTime timestampUtc);
}

public interface IOutboxWriter
{
    Task WriteDomainEventsAsync(IEnumerable<object> domainEvents, CancellationToken ct);
}

public record AiClassificationResult(string Category, string Priority, double Confidence, string Rationale, string Provider, string Model, int? PromptTokens, int? CompletionTokens);

public record AiToolDefinition(string Name, string Description, string JsonSchema);
public record AiToolCall(string Name, string ArgumentsJson);


public record OutboxMessage(Guid Id, string Type, string Payload, DateTime OccurredAtUtc, DateTime? PublishedAtUtc, int Attempts, string? LastError);
public record WebhookReceipt(Guid Id, string Provider, string EventId, string PayloadHash, DateTime ReceivedAtUtc, DateTime? ProcessedAtUtc);
public record AiAuditLog(Guid Id, Guid TicketId, string Provider, string Model, string PromptVersion, string RequestJson, string ResponseJson, int? PromptTokens, int? CompletionTokens, DateTime CreatedAtUtc);

public interface IIdempotentCommand { string IdempotencyKey { get; } }

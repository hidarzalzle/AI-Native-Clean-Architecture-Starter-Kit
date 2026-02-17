using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<Ticket> TicketSet => Set<Ticket>();
    public DbSet<OutboxMessageEntity> OutboxSet => Set<OutboxMessageEntity>();
    public DbSet<WebhookReceiptEntity> WebhookSet => Set<WebhookReceiptEntity>();
    public DbSet<AiAuditLogEntity> AiAuditSet => Set<AiAuditLogEntity>();

    public IQueryable<Ticket> Tickets => TicketSet;
    public IQueryable<OutboxMessage> OutboxMessages => OutboxSet.Select(x => new OutboxMessage(x.Id, x.Type, x.Payload, x.OccurredAtUtc, x.PublishedAtUtc, x.Attempts, x.LastError));
    public IQueryable<WebhookReceipt> WebhookReceipts => WebhookSet.Select(x => new WebhookReceipt(x.Id, x.Provider, x.EventId, x.PayloadHash, x.ReceivedAtUtc, x.ProcessedAtUtc));
    public IQueryable<AiAuditLog> AiAuditLogs => AiAuditSet.Select(x => new AiAuditLog(x.Id, x.TicketId, x.Provider, x.Model, x.PromptVersion, x.RequestJson, x.ResponseJson, x.PromptTokens, x.CompletionTokens, x.CreatedAtUtc));

    public Task AddTicketAsync(Ticket ticket, CancellationToken ct) => TicketSet.AddAsync(ticket, ct).AsTask();
    public Task AddOutboxAsync(OutboxMessage message, CancellationToken ct) => OutboxSet.AddAsync(OutboxMessageEntity.From(message), ct).AsTask();
    public Task AddWebhookReceiptAsync(WebhookReceipt receipt, CancellationToken ct) => WebhookSet.AddAsync(WebhookReceiptEntity.From(receipt), ct).AsTask();
    public Task AddAiAuditAsync(AiAuditLog auditLog, CancellationToken ct) => AiAuditSet.AddAsync(AiAuditLogEntity.From(auditLog), ct).AsTask();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.ToTable("Tickets");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
            e.Ignore(x => x.DomainEvents);
        });
        modelBuilder.Entity<OutboxMessageEntity>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PublishedAtUtc, x.IsProcessing, x.NextAttemptAtUtc, x.OccurredAtUtc }); });
        modelBuilder.Entity<WebhookReceiptEntity>(e => { e.ToTable("WebhookReceipts"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.Provider, x.EventId }).IsUnique(); });
        modelBuilder.Entity<AiAuditLogEntity>(e => { e.ToTable("AiAuditLogs"); e.HasKey(x => x.Id); });
    }
}

public class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public bool IsProcessing { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public static OutboxMessageEntity From(OutboxMessage m) => new() { Id = m.Id, Type = m.Type, Payload = m.Payload, OccurredAtUtc = m.OccurredAtUtc, PublishedAtUtc = m.PublishedAtUtc, Attempts = m.Attempts, LastError = m.LastError, IsProcessing = false, NextAttemptAtUtc = DateTime.UtcNow };
}
public class WebhookReceiptEntity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = default!;
    public string EventId { get; set; } = default!;
    public string PayloadHash { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public static WebhookReceiptEntity From(WebhookReceipt x) => new() { Id = x.Id, Provider = x.Provider, EventId = x.EventId, PayloadHash = x.PayloadHash, ReceivedAtUtc = x.ReceivedAtUtc, ProcessedAtUtc = x.ProcessedAtUtc };
}
public class AiAuditLogEntity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Provider { get; set; } = default!;
    public string Model { get; set; } = default!;
    public string PromptVersion { get; set; } = default!;
    public string RequestJson { get; set; } = default!;
    public string ResponseJson { get; set; } = default!;
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public static AiAuditLogEntity From(AiAuditLog x) => new() { Id = x.Id, TicketId = x.TicketId, Provider = x.Provider, Model = x.Model, PromptVersion = x.PromptVersion, RequestJson = x.RequestJson, ResponseJson = x.ResponseJson, PromptTokens = x.PromptTokens, CompletionTokens = x.CompletionTokens, CreatedAtUtc = x.CreatedAtUtc };
}

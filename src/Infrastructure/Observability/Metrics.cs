using System.Diagnostics.Metrics;

namespace Infrastructure.Observability;

public static class AppMetrics
{
    public static readonly Meter Meter = new("support-triage.metrics", "1.0");
    public static readonly Counter<long> OutboxPublishedTotal = Meter.CreateCounter<long>("outbox_published_total");
    public static readonly Counter<long> OutboxFailedTotal = Meter.CreateCounter<long>("outbox_failed_total");
    public static readonly Counter<long> AiCallsTotal = Meter.CreateCounter<long>("ai_calls_total");
    public static readonly Counter<long> WebhookDuplicatesTotal = Meter.CreateCounter<long>("webhook_duplicates_total");
}

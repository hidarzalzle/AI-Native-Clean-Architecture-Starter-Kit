using Application.Commands;
using Infrastructure.AI;
using Infrastructure.Caching;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IntegrationTests;

public class IntegrationSpecs
{
    private static AppDbContext CreateDb()
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        var db = new AppDbContext(opt);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Writing_domain_event_creates_outbox_message()
    {
        await using var db = CreateDb();
        var h = new CreateTicketHandler(db, new SystemClock(), new OutboxWriter(db, new SystemClock()));
        _ = await h.Handle(new CreateTicketCommand("Title", "Desc", "a@b.com", "k1"), default);
        Assert.True(db.OutboxSet.Any());
    }

    [Fact]
    public async Task Webhook_idempotency_blocks_duplicates()
    {
        await using var db = CreateDb();
        var conf = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Webhooks:Providers:test:Secret"] = "s" }).Build();
        var verifier = new HmacWebhookVerifier(conf);
        var cache = new InMemoryCacheService();
        var payload = "{}";
        var sig = Convert.ToHexString(new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("s")).ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var handler = new ReceiveWebhookHandler(db, verifier, cache, new SystemClock());
        var first = await handler.Handle(new ReceiveWebhookCommand("test", "evt1", payload, sig, DateTime.UtcNow), default);
        var second = await handler.Handle(new ReceiveWebhookCommand("test", "evt1", payload, sig, DateTime.UtcNow), default);
        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task Mock_ai_client_classifies_ticket()
    {
        var ai = new MockAiClient();
        var result = await ai.ClassifyTicketAsync("billing failed", "payment error", default);
        Assert.Equal("Bug", result.Category);
        Assert.Equal("High", result.Priority);
    }
}

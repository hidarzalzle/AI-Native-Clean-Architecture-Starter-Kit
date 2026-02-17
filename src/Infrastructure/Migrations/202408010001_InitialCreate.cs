using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tickets",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Title = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(nullable: false),
                CustomerEmail = table.Column<string>(maxLength: 256, nullable: false),
                Status = table.Column<int>(nullable: false),
                Priority = table.Column<int>(nullable: false),
                Category = table.Column<int>(nullable: false),
                CreatedAtUtc = table.Column<DateTime>(nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Tickets", x => x.Id));

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Type = table.Column<string>(nullable: false),
                Payload = table.Column<string>(nullable: false),
                OccurredAtUtc = table.Column<DateTime>(nullable: false),
                PublishedAtUtc = table.Column<DateTime>(nullable: true),
                Attempts = table.Column<int>(nullable: false),
                LastError = table.Column<string>(nullable: true),
                IsProcessing = table.Column<bool>(nullable: false),
                ProcessingStartedAtUtc = table.Column<DateTime>(nullable: true),
                NextAttemptAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AiAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                TicketId = table.Column<Guid>(nullable: false),
                Provider = table.Column<string>(nullable: false),
                Model = table.Column<string>(nullable: false),
                PromptVersion = table.Column<string>(nullable: false),
                RequestJson = table.Column<string>(nullable: false),
                ResponseJson = table.Column<string>(nullable: false),
                PromptTokens = table.Column<int>(nullable: true),
                CompletionTokens = table.Column<int>(nullable: true),
                CreatedAtUtc = table.Column<DateTime>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AiAuditLogs", x => x.Id));

        migrationBuilder.CreateTable(
            name: "WebhookReceipts",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Provider = table.Column<string>(nullable: false),
                EventId = table.Column<string>(nullable: false),
                PayloadHash = table.Column<string>(nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_WebhookReceipts", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_WebhookReceipts_Provider_EventId", table: "WebhookReceipts", columns: new[] { "Provider", "EventId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_OutboxMessages_PublishedAtUtc_IsProcessing_NextAttemptAtUtc_OccurredAtUtc", table: "OutboxMessages", columns: new[] { "PublishedAtUtc", "IsProcessing", "NextAttemptAtUtc", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AiAuditLogs");
        migrationBuilder.DropTable("OutboxMessages");
        migrationBuilder.DropTable("WebhookReceipts");
        migrationBuilder.DropTable("Tickets");
    }
}

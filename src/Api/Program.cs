using System.Text.Json;
using Application.Commands;
using Application.Common;
using Application.Validators;
using FluentValidation;
using Infrastructure;
using Infrastructure.Observability;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OpenTelemetry.Logs;
using SharedKernel.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.AddConsoleExporter();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTicketCommand).Assembly));
builder.Services.AddValidatorsFromAssemblyContaining<CreateTicketValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

app.UseMiddleware<ExceptionMappingMiddleware>();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/webhooks") || ctx.Request.Path.StartsWithSegments("/health"))
    {
        await next();
        return;
    }

    var enabled = app.Configuration.GetValue<bool>("ApiKeyAuth:Enabled");
    if (!enabled)
    {
        await next();
        return;
    }

    if (!ctx.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != app.Configuration["ApiKeyAuth:Key"])
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Title = "Unauthorized", Status = 401 });
        return;
    }

    await next();
});

app.MapHealthChecks("/health");
app.MapPost("/tickets", async (CreateTicketCommand cmd, ISender sender, CancellationToken ct)
    => Results.Ok(await sender.Send(cmd, ct)));

app.MapPost("/tickets/{id:guid}/classify", async (Guid id, ISender sender, CancellationToken ct) =>
{
    await sender.Send(new ClassifyTicketWithAiCommand(id), ct);
    return Results.Accepted();
});

app.MapGet("/tickets/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
{
    var t = await sender.Send(new Application.Queries.GetTicketQuery(id), ct);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

app.MapPost("/webhooks/{provider}", async (string provider, HttpRequest req, ISender sender, CancellationToken ct) =>
{
    using var activity = AppDiagnostics.ActivitySource.StartActivity("webhook.receive");
    activity?.SetTag("webhook.provider", provider);
    using var reader = new StreamReader(req.Body);
    var payload = await reader.ReadToEndAsync(ct);

    string? payloadEventId = null;
    try
    {
        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
        {
            payloadEventId = idProp.GetString();
        }
    }
    catch
    {
        // ignore payload parsing; header fallback below
    }

    var eventId = req.Headers["Idempotency-Key"].FirstOrDefault() ?? payloadEventId ?? Guid.NewGuid().ToString("N");
    var sig = req.Headers["X-Signature"].FirstOrDefault() ?? string.Empty;
    var ts = DateTime.TryParse(req.Headers["X-Timestamp"].FirstOrDefault(), out var d) ? d : DateTime.UtcNow;

    var first = await sender.Send(new ReceiveWebhookCommand(provider, eventId, payload, sig, ts), ct);
    if (!first) AppMetrics.WebhookDuplicatesTotal.Add(1, new KeyValuePair<string, object?>("webhook.provider", provider));
    return Results.Ok(new { processed = first });
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

try
{
    await app.RunAsync();
}
catch (TaskCanceledException)
{
    app.Logger.LogInformation("Host task cancellation observed during startup/shutdown.");
}
catch (OperationCanceledException)
{
    app.Logger.LogInformation("Host shutdown cancellation observed.");
}

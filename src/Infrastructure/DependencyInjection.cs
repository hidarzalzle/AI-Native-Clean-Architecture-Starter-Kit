using Application.Abstractions;
using Infrastructure.AI;
using Infrastructure.Background;
using Infrastructure.Caching;
using Infrastructure.External;
using Infrastructure.Messaging;
using Infrastructure.Outbox;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using SharedKernel.Common;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
        {
            var conn = config.GetConnectionString("Default") ?? "Server=localhost;Database=SupportTriage;User Id=sa;Password=Your_password123;TrustServerCertificate=true";
            opt.UseSqlServer(conn);
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IWebhookVerifier, HmacWebhookVerifier>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config["Redis:ConnectionString"] ?? "localhost:6379"));
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddHttpClient<IExternalHttpClient, ExternalHttpClient>();
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        var useRabbit = config.GetValue<bool>("Messaging:UseRabbitMq");
        if (useRabbit)
        {
            var host = config["Messaging:RabbitMq:Host"] ?? "rabbitmq";
            var exchange = config["Messaging:RabbitMq:Exchange"] ?? "outbox.events";
            services.AddSingleton<IMessagePublisher>(_ => new RabbitMqMessagePublisher(host, exchange));
        }
        else
        {
            services.AddSingleton<IMessagePublisher, LogMessagePublisher>();
        }
        services.Configure<HostOptions>(opt =>
        {
            opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
        });
        services.AddHostedService<OutboxPublisherWorker>();

        var provider = config["Ai:Provider"] ?? "Mock";
        services.AddHttpClient<OpenAiClient>().AddStandardResilienceHandler();
        services.AddHttpClient<GeminiAiClient>().AddStandardResilienceHandler();
        services.AddScoped<IAiClient>(sp => provider switch
        {
            "OpenAI" => sp.GetRequiredService<OpenAiClient>(),
            "Gemini" => sp.GetRequiredService<GeminiAiClient>(),
            _ => new MockAiClient()
        });

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("support-triage-api"))
            .WithTracing(t => t.AddSource(AppDiagnostics.ActivitySourceName).AddAspNetCoreInstrumentation().AddEntityFrameworkCoreInstrumentation().AddHttpClientInstrumentation().AddConsoleExporter())
            .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation().AddMeter("support-triage.metrics").AddConsoleExporter());
        return services;
    }
}

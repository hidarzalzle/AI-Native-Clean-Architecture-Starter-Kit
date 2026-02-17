using Application.Abstractions;

namespace Infrastructure.External;

public class ExternalHttpClient(HttpClient client) : IExternalHttpClient
{
    public Task<string> GetStringAsync(string url, CancellationToken ct) => client.GetStringAsync(url, ct);
}

public class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct) => Task.CompletedTask;
}

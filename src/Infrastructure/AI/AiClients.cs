using Application.Abstractions;
using Infrastructure.Observability;

namespace Infrastructure.AI;

public class MockAiClient : IAiClient
{
    public Task<AiClassificationResult> ClassifyTicketAsync(string title, string description, CancellationToken ct)
    {
        AppMetrics.AiCallsTotal.Add(1);
        return Task.FromResult(new AiClassificationResult("Bug", "High", 0.91, "Keyword heuristic", "Mock", "mock-v1", 10, 14));
    }

    public Task<IReadOnlyCollection<AiToolCall>> InvokeWithToolsAsync(string prompt, IReadOnlyCollection<AiToolDefinition> tools, CancellationToken ct)
        => Task.FromResult<IReadOnlyCollection<AiToolCall>>([]);
}

public class OpenAiClient(HttpClient httpClient) : IAiClient
{
    public async Task<AiClassificationResult> ClassifyTicketAsync(string title, string description, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        AppMetrics.AiCallsTotal.Add(1);
        return new AiClassificationResult("Other", "Medium", 0.5, "stub", "OpenAI", "gpt-4.1-mini", null, null);
    }

    public async Task<IReadOnlyCollection<AiToolCall>> InvokeWithToolsAsync(string prompt, IReadOnlyCollection<AiToolDefinition> tools, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        AppMetrics.AiCallsTotal.Add(1);
        return [];
    }
}

public class GeminiAiClient(HttpClient httpClient) : IAiClient
{
    public async Task<AiClassificationResult> ClassifyTicketAsync(string title, string description, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        AppMetrics.AiCallsTotal.Add(1);
        return new AiClassificationResult("Other", "Medium", 0.5, "stub", "Gemini", "gemini-1.5-flash", null, null);
    }

    public async Task<IReadOnlyCollection<AiToolCall>> InvokeWithToolsAsync(string prompt, IReadOnlyCollection<AiToolDefinition> tools, CancellationToken ct)
    {
        await Task.Delay(10, ct);
        AppMetrics.AiCallsTotal.Add(1);
        return [];
    }
}

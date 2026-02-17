using System.Security.Cryptography;
using System.Text;
using Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Webhooks;

public class HmacWebhookVerifier(IConfiguration configuration) : IWebhookVerifier
{
    public bool Verify(string provider, string payload, string signatureHeader, DateTime timestampUtc)
    {
        if (Math.Abs((DateTime.UtcNow - timestampUtc).TotalMinutes) > 5) return false;
        var secret = configuration[$"Webhooks:Providers:{provider}:Secret"] ?? "dev-secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return hash == signatureHeader.ToLowerInvariant();
    }
}

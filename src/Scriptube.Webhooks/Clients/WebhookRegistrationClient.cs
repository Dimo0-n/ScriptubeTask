using System.Net.Http.Json;
using System.Text.Json;

namespace Scriptube.Webhooks.Clients;

public sealed class WebhookRegistrationClient
{
    private readonly HttpClient _httpClient;

    public WebhookRegistrationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> RegisterAsync(
        string url,
        IReadOnlyCollection<string> events,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Webhook URL is required.", nameof(url));
        }

        if (events.Count == 0)
        {
            throw new ArgumentException("At least one webhook event is required.", nameof(events));
        }

        var request = new
        {
            url,
            events
        };

        return _httpClient.PostAsJsonAsync("/api/webhooks/register", request, cancellationToken);
    }

    public static string ExtractWebhookIdOrThrow(string registerResponseJson)
    {
        if (TryExtractWebhookId(registerResponseJson, out var webhookId))
        {
            return webhookId!;
        }

        throw new InvalidOperationException("Webhook register response does not include webhook id.");
    }

    public static bool TryExtractWebhookId(string registerResponseJson, out string? webhookId)
    {
        webhookId = null;
        if (string.IsNullOrWhiteSpace(registerResponseJson))
        {
            return false;
        }

        using var document = JsonDocument.Parse(registerResponseJson);
        return TryGetString(document.RootElement, "webhook_id", out webhookId) ||
               TryGetString(document.RootElement, "id", out webhookId);
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }
}

using System.Net.Http.Json;
using Scriptube.Api.Contracts;

namespace Scriptube.Api.Clients;

public sealed class WebhooksClient
{
    private readonly HttpClient _httpClient;

    public WebhooksClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> RegisterAsync(WebhookRegisterRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("/api/webhooks/register", request, cancellationToken);

    public Task<HttpResponseMessage> ListAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/webhooks", cancellationToken);

    public Task<HttpResponseMessage> GetAsync(string webhookId, CancellationToken cancellationToken = default)
        => _httpClient.GetAsync($"/api/webhooks/{webhookId}", cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string webhookId, CancellationToken cancellationToken = default)
        => _httpClient.DeleteAsync($"/api/webhooks/{webhookId}", cancellationToken);

    public Task<HttpResponseMessage> ListAvailableEventsAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/webhooks/events/available", cancellationToken);

    public Task<HttpResponseMessage> TriggerTestAsync(string webhookId, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"/api/webhooks/{webhookId}/test", content: null, cancellationToken);

    public Task<HttpResponseMessage> GetLogsAsync(string webhookId, CancellationToken cancellationToken = default)
        => _httpClient.GetAsync($"/api/webhooks/{webhookId}/logs", cancellationToken);

    public Task<HttpResponseMessage> RetryDeliveryAsync(string webhookId, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"/api/webhooks/{webhookId}/retry", content: null, cancellationToken);
}
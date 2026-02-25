using System.Net.Http.Json;
using Scriptube.Api.Contracts;

namespace Scriptube.Api.Clients;

public sealed class CreditsClient
{
    private readonly HttpClient _httpClient;

    public CreditsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetBalanceAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/credits/balance", cancellationToken);

    public Task<HttpResponseMessage> PrecheckAsync(CreditsPrecheckRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("/api/v1/credits/precheck", request, cancellationToken);

    public Task<HttpResponseMessage> EstimateAsync(CreditsEstimateRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("/api/v1/credits/estimate", request, cancellationToken);

    public Task<HttpResponseMessage> GetCostsAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/credits/costs", cancellationToken);

    public Task<HttpResponseMessage> GetHistoryAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/credits/history", cancellationToken);
}
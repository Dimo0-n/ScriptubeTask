namespace Scriptube.Api.Clients;

public sealed class HealthClient
{
    private readonly HttpClient _httpClient;

    public HealthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetPublicAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/health", cancellationToken);

    public Task<HttpResponseMessage> GetApiHealthAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/health", cancellationToken);
}
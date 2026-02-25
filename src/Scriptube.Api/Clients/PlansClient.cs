namespace Scriptube.Api.Clients;

public sealed class PlansClient
{
    private readonly HttpClient _httpClient;

    public PlansClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/plans", cancellationToken);
}
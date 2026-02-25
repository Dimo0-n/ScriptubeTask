namespace Scriptube.Api.Clients;

public sealed class UsageClient
{
    private readonly HttpClient _httpClient;

    public UsageClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/usage", cancellationToken);
}
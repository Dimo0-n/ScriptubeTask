namespace Scriptube.Api.Clients;

public sealed class UserClient
{
    private readonly HttpClient _httpClient;

    public UserClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> GetAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/user", cancellationToken);
}
using System.Net.Http.Json;
using Scriptube.Api.Contracts;

namespace Scriptube.Api.Clients;

public sealed class SeoToolsClient
{
    private readonly HttpClient _httpClient;

    public SeoToolsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> GetYoutubeTranscriptAsync(string videoUrl, CancellationToken cancellationToken = default)
    {
        var request = new SeoToolsYoutubeTranscriptRequest
        {
            Url = videoUrl
        };

        return await _httpClient.PostAsJsonAsync("/tools/youtube-transcript", request, cancellationToken);
    }
}
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
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("url", videoUrl)
        ]);

        return await _httpClient.PostAsync("/tools/youtube-transcript", content, cancellationToken);
    }
}
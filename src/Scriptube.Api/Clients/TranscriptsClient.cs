using System.Net.Http.Json;
using Scriptube.Api.Contracts;

namespace Scriptube.Api.Clients;

public sealed class TranscriptsClient
{
    private readonly HttpClient _httpClient;

    public TranscriptsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> SubmitAsync(TranscriptSubmitRequest request, CancellationToken cancellationToken = default)
        => _httpClient.PostAsJsonAsync("/api/v1/transcripts", request, cancellationToken);

    public Task<HttpResponseMessage> ListAsync(CancellationToken cancellationToken = default)
        => _httpClient.GetAsync("/api/v1/transcripts", cancellationToken);

    public Task<HttpResponseMessage> GetBatchAsync(string batchId, CancellationToken cancellationToken = default)
        => _httpClient.GetAsync($"/api/v1/transcripts/{batchId}", cancellationToken);

    public Task<HttpResponseMessage> ExportAsync(string batchId, string format, CancellationToken cancellationToken = default)
        => _httpClient.GetAsync($"/api/v1/transcripts/{batchId}/export?format={Uri.EscapeDataString(format)}", cancellationToken);

    public Task<HttpResponseMessage> DeleteAsync(string batchId, CancellationToken cancellationToken = default)
        => _httpClient.DeleteAsync($"/api/v1/transcripts/{batchId}", cancellationToken);

    public Task<HttpResponseMessage> CancelAsync(string batchId, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"/api/v1/transcripts/{batchId}/cancel", content: null, cancellationToken);

    public Task<HttpResponseMessage> RetryFailedAsync(string batchId, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"/api/v1/transcripts/{batchId}/retry-failed", content: null, cancellationToken);

    public Task<HttpResponseMessage> RerunAsync(string batchId, CancellationToken cancellationToken = default)
        => _httpClient.PostAsync($"/api/v1/transcripts/{batchId}/rerun", content: null, cancellationToken);
}
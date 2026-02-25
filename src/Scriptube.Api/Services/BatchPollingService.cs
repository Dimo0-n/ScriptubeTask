using System.Text.Json;
using Scriptube.Api.Clients;
using Scriptube.Core.Configuration;

namespace Scriptube.Api.Services;

public sealed class BatchPollingService
{
    private static readonly HashSet<string> TerminalStatuses =
    [
        "completed",
        "failed",
        "cancelled",
        "canceled"
    ];

    private readonly TranscriptsClient _transcriptsClient;
    private readonly ScriptubeSettings _settings;

    public BatchPollingService(TranscriptsClient transcriptsClient, ScriptubeSettings settings)
    {
        _transcriptsClient = transcriptsClient;
        _settings = settings;
    }

    public async Task<BatchPollingResult> WaitForFinalStatusAsync(string batchId, CancellationToken cancellationToken = default)
    {
        var timeline = new List<string>();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.PollingTimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        while (!linkedCts.IsCancellationRequested)
        {
            using var response = await _transcriptsClient.GetBatchAsync(batchId, linkedCts.Token);
            var payload = await response.Content.ReadAsStringAsync(linkedCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Polling failed for batch '{batchId}' with status {(int)response.StatusCode}. Payload: {payload}");
            }

            var status = TryReadStatus(payload);
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (timeline.Count == 0 || !string.Equals(timeline[^1], status, StringComparison.OrdinalIgnoreCase))
                {
                    timeline.Add(status);
                }

                if (TerminalStatuses.Contains(status.ToLowerInvariant()))
                {
                    return new BatchPollingResult(batchId, status, timeline, payload);
                }
            }

            await Task.Delay(_settings.PollingIntervalMs, linkedCts.Token);
        }

        throw new TimeoutException($"Polling timeout reached for batch '{batchId}'.");
    }

    private static string TryReadStatus(string payload)
    {
        try
        {
            using var json = JsonDocument.Parse(payload);
            if (json.RootElement.TryGetProperty("status", out var statusElement))
            {
                return statusElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
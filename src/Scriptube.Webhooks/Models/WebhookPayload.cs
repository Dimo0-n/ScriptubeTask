namespace Scriptube.Webhooks.Models;

public sealed class WebhookPayload
{
    public string BatchId { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public IReadOnlyCollection<WebhookPayloadItem> Items { get; init; } = [];

    public string RawJson { get; init; } = string.Empty;
}

public sealed class WebhookPayloadItem
{
    public string? Url { get; init; }

    public string? Status { get; init; }
}
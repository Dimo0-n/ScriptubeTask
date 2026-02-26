namespace Scriptube.Webhooks.Models;

public sealed class WebhookDelivery
{
    public required DateTimeOffset ReceivedAtUtc { get; init; }

    public required string Method { get; init; }

    public required string Path { get; init; }

    public required string Body { get; init; }

    public required IReadOnlyDictionary<string, string[]> Headers { get; init; }
}

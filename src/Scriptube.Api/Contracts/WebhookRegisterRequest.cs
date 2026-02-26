using System.Text.Json.Serialization;

namespace Scriptube.Api.Contracts;

public sealed class WebhookRegisterRequest
{
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("events")]
    public IReadOnlyCollection<string>? Events { get; init; }
}
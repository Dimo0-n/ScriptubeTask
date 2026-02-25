using System.Text.Json.Serialization;

namespace Scriptube.Api.Contracts;

public sealed class CreditsEstimateRequest
{
    [JsonPropertyName("video_ids")]
    public required IReadOnlyCollection<string> VideoIds { get; init; }

    [JsonPropertyName("translate_to_english")]
    public bool TranslateToEnglish { get; init; }

    [JsonPropertyName("use_byok")]
    public bool UseByok { get; init; }
}
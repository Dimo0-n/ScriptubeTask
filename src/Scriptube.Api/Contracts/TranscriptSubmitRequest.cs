using System.Text.Json.Serialization;

namespace Scriptube.Api.Contracts;

public sealed class TranscriptSubmitRequest
{
    [JsonPropertyName("urls")]
    public required IReadOnlyCollection<string> Urls { get; init; }

    [JsonPropertyName("translate_to_english")]
    public bool TranslateToEnglish { get; init; }

    [JsonPropertyName("use_byok")]
    public bool UseByok { get; init; }
}
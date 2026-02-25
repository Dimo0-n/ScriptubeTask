namespace Scriptube.Api.Services;

public sealed record BatchPollingResult(
    string BatchId,
    string FinalStatus,
    IReadOnlyCollection<string> StatusTimeline,
    string FinalPayloadJson);
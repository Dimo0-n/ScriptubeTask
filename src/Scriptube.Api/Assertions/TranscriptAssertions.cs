namespace Scriptube.Api.Assertions;

public static class TranscriptAssertions
{
    public static void ShouldBeSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Expected success status but got {(int)response.StatusCode} ({response.StatusCode}).");
        }
    }

    public static void ShouldBeTerminalStatus(string status)
    {
        var terminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "completed",
            "failed",
            "cancelled",
            "canceled"
        };

        if (!terminalStatuses.Contains(status))
        {
            throw new InvalidOperationException($"Status '{status}' is not a terminal status.");
        }
    }
}
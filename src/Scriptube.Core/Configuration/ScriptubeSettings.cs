namespace Scriptube.Core.Configuration;

public sealed class ScriptubeSettings
{
    public string BaseUrl { get; set; } = "https://scriptube.me";

    public string UiBaseUrl { get; set; } = "https://scriptube.me/ui";

    public int ApiTimeoutSeconds { get; set; } = 60;

    public int PollingIntervalMs { get; set; } = 1500;

    public int PollingTimeoutSeconds { get; set; } = 180;

    public int RetryCount { get; set; } = 2;

    public ScriptubeWebhookSettings Webhook { get; set; } = new();

    public ScriptubeCredentials Credentials { get; set; } = new();
}

public sealed class ScriptubeWebhookSettings
{
    public string ListenerBaseUrl { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}

public sealed class ScriptubeCredentials
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
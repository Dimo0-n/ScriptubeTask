using Microsoft.Extensions.Configuration;

namespace Scriptube.Core.Configuration;

public static class ScriptubeSettingsLoader
{
    public static ScriptubeSettings Load(string? environmentName = null, string? contentRoot = null)
    {
        var root = ResolveContentRoot(contentRoot);
        var configFolder = Path.Combine(root, "config");
        var env = (environmentName ?? Environment.GetEnvironmentVariable("SCRIPTUBE_ENV") ?? "local").ToLowerInvariant();

        var builder = new ConfigurationBuilder()
            .SetBasePath(root)
            .AddJsonFile(Path.Combine("config", "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine("config", $"appsettings.{env}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        if (Directory.Exists(configFolder))
        {
            var localConfigFile = Path.Combine(configFolder, "appsettings.local.json");
            if (File.Exists(localConfigFile))
            {
                builder.AddJsonFile(Path.Combine("config", "appsettings.local.json"), optional: true, reloadOnChange: false);
            }
        }

        var configuration = builder.Build();
        var settings = new ScriptubeSettings();
        configuration.GetSection("Scriptube").Bind(settings);

        settings.Credentials.Email =
            Environment.GetEnvironmentVariable("SCRIPTUBE_EMAIL")
            ?? settings.Credentials.Email;

        settings.Credentials.Password =
            Environment.GetEnvironmentVariable("SCRIPTUBE_PASSWORD")
            ?? settings.Credentials.Password;

        return settings;
    }

    private static string ResolveContentRoot(string? explicitContentRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitContentRoot))
        {
            return explicitContentRoot;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "config")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
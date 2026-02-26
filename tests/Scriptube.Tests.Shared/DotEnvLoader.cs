namespace Scriptube.Tests.Shared;

public static class DotEnvLoader
{
    private static int _initialized;

    public static void LoadFromWorkspaceRoot()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var envPath = Path.Combine(current.FullName, ".env");
            if (File.Exists(envPath))
            {
                LoadFile(envPath);
                return;
            }

            current = current.Parent;
        }
    }

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            value = Unquote(value);

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (string.Equals(key, "SCRPIPTUBE_API_KEY", StringComparison.OrdinalIgnoreCase))
            {
                key = "SCRIPTUBE_API_KEY";
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2)
        {
            var startsWithDouble = value.StartsWith('"');
            var endsWithDouble = value.EndsWith('"');
            if (startsWithDouble && endsWithDouble)
            {
                return value[1..^1];
            }

            var startsWithSingle = value.StartsWith('\'');
            var endsWithSingle = value.EndsWith('\'');
            if (startsWithSingle && endsWithSingle)
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
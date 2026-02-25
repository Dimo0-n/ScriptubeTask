namespace Scriptube.Core.Utilities;

public static class SensitiveDataMasker
{
    public static string MaskApiKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= 6)
        {
            return "***";
        }

        return $"{value[..3]}***{value[^3..]}";
    }
}
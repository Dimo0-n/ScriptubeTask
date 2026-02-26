using System.Collections.Concurrent;
using System.Text;
using Scriptube.Core.Logging;

namespace Scriptube.Tests.Shared;

public sealed class HttpLogCollector : IRequestLogSink
{
    private readonly ConcurrentQueue<string> _entries = new();

    public void Write(string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            _entries.Enqueue(content);
        }
    }

    public bool HasEntries => !_entries.IsEmpty;

    public string GetCombinedLog()
    {
        var builder = new StringBuilder();
        foreach (var entry in _entries)
        {
            builder.AppendLine(entry);
            builder.AppendLine(new string('-', 80));
        }

        return builder.ToString();
    }
}

public static class HttpLogAttachmentHelper
{
    public static void AttachToCurrentTest(string fileName, string content)
    {
        AllureAttachmentHelper.AttachText(fileName, content);
    }
}
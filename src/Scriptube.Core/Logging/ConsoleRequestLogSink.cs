namespace Scriptube.Core.Logging;

public sealed class ConsoleRequestLogSink : IRequestLogSink
{
    public void Write(string content)
    {
        Console.WriteLine(content);
    }
}
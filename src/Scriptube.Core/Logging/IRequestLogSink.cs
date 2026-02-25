namespace Scriptube.Core.Logging;

public interface IRequestLogSink
{
    void Write(string content);
}
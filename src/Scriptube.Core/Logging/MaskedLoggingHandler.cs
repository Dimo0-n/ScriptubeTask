using System.Net.Http.Headers;
using System.Text;
using Scriptube.Core.Utilities;

namespace Scriptube.Core.Logging;

public sealed class MaskedLoggingHandler : DelegatingHandler
{
    private readonly IRequestLogSink _sink;

    public MaskedLoggingHandler(IRequestLogSink sink)
    {
        _sink = sink;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestLog = await BuildRequestLogAsync(request, cancellationToken);
        _sink.Write(requestLog);

        var response = await base.SendAsync(request, cancellationToken);

        var responseLog = await BuildResponseLogAsync(response, cancellationToken);
        _sink.Write(responseLog);

        return response;
    }

    private static async Task<string> BuildRequestLogAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[HTTP REQUEST] {request.Method} {request.RequestUri}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, request.Headers);
        if (request.Content is not null)
        {
            AppendHeaders(builder, request.Content.Headers);
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                builder.AppendLine("Body:");
                builder.AppendLine(body);
            }
        }

        return builder.ToString();
    }

    private static async Task<string> BuildResponseLogAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"[HTTP RESPONSE] {(int)response.StatusCode} {response.ReasonPhrase}");
        builder.AppendLine("Headers:");
        AppendHeaders(builder, response.Headers);
        if (response.Content is not null)
        {
            AppendHeaders(builder, response.Content.Headers);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(body))
            {
                builder.AppendLine("Body:");
                builder.AppendLine(body);
            }
        }

        return builder.ToString();
    }

    private static void AppendHeaders(StringBuilder builder, HttpHeaders headers)
    {
        foreach (var header in headers)
        {
            if (header.Key.Equals("X-API-Key", StringComparison.OrdinalIgnoreCase))
            {
                var raw = header.Value.FirstOrDefault();
                builder.AppendLine($"  {header.Key}: {SensitiveDataMasker.MaskApiKey(raw)}");
                continue;
            }

            builder.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
    }
}
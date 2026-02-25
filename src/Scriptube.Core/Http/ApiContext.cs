using Scriptube.Core.Configuration;
using Scriptube.Core.Logging;
using Scriptube.Core.Retry;

namespace Scriptube.Core.Http;

public static class ApiContext
{
    public static HttpClient CreateClient(
        ScriptubeSettings settings,
        ApiAuthenticationOptions? authOptions = null,
        IRequestLogSink? logSink = null,
        HttpMessageHandler? primaryHandler = null)
    {
        var sink = logSink ?? new ConsoleRequestLogSink();
        var retryPolicy = RetryPolicyFactory.CreateHttpPolicy(settings.RetryCount);

        var innerHandler = primaryHandler ?? new HttpClientHandler();
        var retryHandler = new RetryDelegatingHandler(retryPolicy)
        {
            InnerHandler = innerHandler
        };

        var loggingHandler = new MaskedLoggingHandler(sink)
        {
            InnerHandler = retryHandler
        };

        var client = new HttpClient(loggingHandler)
        {
            BaseAddress = new Uri(settings.BaseUrl),
            Timeout = TimeSpan.FromSeconds(settings.ApiTimeoutSeconds)
        };

        var apiKey = authOptions?.ApiKey ?? Environment.GetEnvironmentVariable("SCRIPTUBE_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Remove("X-API-Key");
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        return client;
    }
}
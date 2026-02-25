using Polly;
using Polly.Retry;

namespace Scriptube.Core.Retry;

public static class RetryPolicyFactory
{
    public static AsyncRetryPolicy<HttpResponseMessage> CreateHttpPolicy(int retryCount)
    {
        var count = Math.Max(retryCount, 0);

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response =>
                (int)response.StatusCode >= 500 ||
                response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(count, attempt => TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1)));
    }
}
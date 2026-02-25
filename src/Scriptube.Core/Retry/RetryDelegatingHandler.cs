using Polly.Retry;

namespace Scriptube.Core.Retry;

public sealed class RetryDelegatingHandler : DelegatingHandler
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;

    public RetryDelegatingHandler(AsyncRetryPolicy<HttpResponseMessage> policy)
    {
        _policy = policy;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _policy.ExecuteAsync(async token => await base.SendAsync(request, token), cancellationToken);
    }
}
using Scriptube.Core.Http;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Webhooks;

public abstract class WebhookTestBase : ScriptubeTestBase
{
    protected void RequireLiveApi()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_SCRIPTUBE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Set RUN_LIVE_SCRIPTUBE=true to run live webhook tests.");
        }
    }

    protected string RequireApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("SCRIPTUBE_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore("Set SCRIPTUBE_API_KEY to run webhook tests.");
        }

        return apiKey;
    }

    protected string RequireWebhookUrlOrIgnore()
    {
        var webhookUrl = Environment.GetEnvironmentVariable("SCRIPTUBE_WEBHOOK_TEST_URL");
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            Assert.Ignore("Set SCRIPTUBE_WEBHOOK_TEST_URL (HTTPS endpoint) to run webhook registration flows.");
        }

        return webhookUrl;
    }

    protected HttpClient CreateAuthenticatedClient()
    {
        var apiKey = RequireApiKey();
        return ApiContext.CreateClient(Settings, new ApiAuthenticationOptions { ApiKey = apiKey });
    }

    protected static void IgnoreIfEndpointUnavailable(HttpResponseMessage response, string endpoint)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.MethodNotAllowed)
        {
            Assert.Ignore($"Endpoint '{endpoint}' is not available in this Scriptube deployment version.");
        }
    }
}
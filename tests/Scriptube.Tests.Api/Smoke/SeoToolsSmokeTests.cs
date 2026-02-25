using FluentAssertions;
using Scriptube.Api.Clients;
using Scriptube.Core.Http;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Api.Smoke;

[TestFixture]
[Category("API")]
[Category("Smoke")]
public sealed class SeoToolsSmokeTests : ScriptubeTestBase
{
    [Test]
    public async Task PublicSeoTranscriptEndpoint_Should_ReturnSuccessStatusCode()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_SCRIPTUBE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Set RUN_LIVE_SCRIPTUBE=true to run live API smoke tests.");
        }

        using var httpClient = ApiContext.CreateClient(Settings, new ApiAuthenticationOptions { ApiKey = null });
        httpClient.DefaultRequestHeaders.Remove("X-API-Key");

        var client = new SeoToolsClient(httpClient);
        using var response = await client.GetYoutubeTranscriptAsync("https://www.youtube.com/watch?v=tstENMAN001");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
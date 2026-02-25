using FluentAssertions;
using Scriptube.Api.Clients;
using Scriptube.Core.Http;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Api.Smoke;

[TestFixture]
[Category("API")]
[Category("Smoke")]
public sealed class SeoToolsSmokeTests : ApiTestBase
{
    [Test]
    public async Task PublicHealthEndpoint_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateClientWithoutApiKey();
        var healthClient = new HealthClient(client);
        using var response = await healthClient.GetPublicAsync();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task PublicSeoTranscriptEndpoint_Should_ReturnSuccessStatusCode()
    {
        RequireLiveApi();

        using var httpClient = CreateClientWithoutApiKey();

        var client = new SeoToolsClient(httpClient);
        using var response = await client.GetYoutubeTranscriptAsync(TestDataCatalog.SuccessVideos.EnglishManual);

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task UserEndpoint_WithValidApiKey_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var userClient = new UserClient(client);
        using var response = await userClient.GetAsync();

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task CreditsBalance_WithValidApiKey_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var creditsClient = new CreditsClient(client);
        using var response = await creditsClient.GetBalanceAsync();

        response.IsSuccessStatusCode.Should().BeTrue();

        using var document = await ReadJsonAsync(response);
        var _ = TryGetBalance(document);
    }
}
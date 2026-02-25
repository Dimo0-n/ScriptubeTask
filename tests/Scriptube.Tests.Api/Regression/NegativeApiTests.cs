using System.Net;
using FluentAssertions;
using Scriptube.Api.Builders;
using Scriptube.Api.Clients;

namespace Scriptube.Tests.Api.Regression;

[TestFixture]
[Category("API")]
[Category("Regression")]
public sealed class NegativeApiTests : ApiTestBase
{
    [Test]
    public async Task NoApiKey_Should_ReturnUnauthorized_OnProtectedEndpoints()
    {
        RequireLiveApi();

        using var client = CreateClientWithoutApiKey();
        var userClient = new UserClient(client);
        var creditsClient = new CreditsClient(client);

        using var userResponse = await userClient.GetAsync();
        using var creditsResponse = await creditsClient.GetBalanceAsync();

        userResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        creditsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InvalidApiKey_Should_ReturnUnauthorized_OnProtectedEndpoints()
    {
        RequireLiveApi();

        using var client = CreateClientWithInvalidApiKey();
        var userClient = new UserClient(client);

        using var userResponse = await userClient.GetAsync();
        userResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task EmptyUrls_Should_ReturnValidationError()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);

        var request = new TranscriptRequestBuilder().BuildSubmit();
        using var response = await transcriptsClient.SubmitAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task InvalidUrl_Should_ReturnValidationError()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);

        var request = new TranscriptRequestBuilder().WithUrl("https://example.com/not-youtube").BuildSubmit();
        using var response = await transcriptsClient.SubmitAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Test]
    public async Task NonExistentBatchId_Should_ReturnNotFound()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);

        using var response = await transcriptsClient.GetBatchAsync("batch-does-not-exist");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
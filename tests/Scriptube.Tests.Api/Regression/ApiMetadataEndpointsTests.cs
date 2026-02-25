using FluentAssertions;
using Scriptube.Api.Builders;
using Scriptube.Api.Clients;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Api.Regression;

[TestFixture]
[Category("API")]
[Category("Regression")]
public sealed class ApiMetadataEndpointsTests : ApiTestBase
{
    [Test]
    public async Task UsageAndPlansEndpoints_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var usageClient = new UsageClient(client);
        var plansClient = new PlansClient(client);

        using var usageResponse = await usageClient.GetAsync();
        using var plansResponse = await plansClient.GetAsync();

        usageResponse.IsSuccessStatusCode.Should().BeTrue();
        plansResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task ListBatchesEndpoint_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);

        using var response = await transcriptsClient.ListAsync();
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task CreditsEstimateCostsAndHistory_Should_WorkWhenAvailable()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var creditsClient = new CreditsClient(client);

        var estimateRequest = new TranscriptRequestBuilder()
            .BuildEstimateByVideoIds(["tstENMAN001", "tstENAUT001"]);

        using (var estimateResponse = await creditsClient.EstimateAsync(estimateRequest))
        {
            IgnoreIfEndpointUnavailable(estimateResponse, "/api/v1/credits/estimate");
            estimateResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        using (var costsResponse = await creditsClient.GetCostsAsync())
        {
            IgnoreIfEndpointUnavailable(costsResponse, "/api/v1/credits/costs");
            costsResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        using (var historyResponse = await creditsClient.GetHistoryAsync())
        {
            IgnoreIfEndpointUnavailable(historyResponse, "/api/v1/credits/history");
            historyResponse.IsSuccessStatusCode.Should().BeTrue();
        }
    }
}
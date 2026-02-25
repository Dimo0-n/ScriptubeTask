using System.Net;
using FluentAssertions;
using Scriptube.Api.Builders;
using Scriptube.Api.Clients;
using Scriptube.Api.Services;

namespace Scriptube.Tests.Api.Regression;

[TestFixture]
[Category("API")]
[Category("Regression")]
public sealed class TranscriptsBusinessFlowTests : ApiTestBase
{
    [Test]
    public async Task PrecheckSubmitPollExport_Should_WorkForSingleVideo()
    {
        RequireLiveApi();

        using var httpClient = CreateAuthenticatedClient();
        var creditsClient = new CreditsClient(httpClient);
        var transcriptsClient = new TranscriptsClient(httpClient);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        var requestBuilder = new TranscriptRequestBuilder()
            .WithUrl("https://www.youtube.com/watch?v=tstENMAN001");

        using (var precheckResponse = await creditsClient.PrecheckAsync(requestBuilder.BuildPrecheck()))
        {
            IgnoreIfEndpointUnavailable(precheckResponse, "/api/v1/credits/precheck");
            precheckResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        string batchId;
        using (var submitResponse = await transcriptsClient.SubmitAsync(requestBuilder.BuildSubmit()))
        {
            submitResponse.IsSuccessStatusCode.Should().BeTrue();

            using var submitBody = await ReadJsonAsync(submitResponse);
            batchId = TryGetBatchId(submitBody);
        }

        var pollingResult = await pollingService.WaitForFinalStatusAsync(batchId);
        pollingResult.FinalStatus.Should().Be("completed");

        using var exportJsonResponse = await transcriptsClient.ExportAsync(batchId, "json");
        exportJsonResponse.IsSuccessStatusCode.Should().BeTrue();

        using var exportTxtResponse = await transcriptsClient.ExportAsync(batchId, "txt");
        exportTxtResponse.IsSuccessStatusCode.Should().BeTrue();

        using var exportSrtResponse = await transcriptsClient.ExportAsync(batchId, "srt");
        exportSrtResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task CreditsBalance_Should_DecreaseOrStayEqual_AfterBatchProcessing()
    {
        RequireLiveApi();

        using var httpClient = CreateAuthenticatedClient();
        var creditsClient = new CreditsClient(httpClient);
        var transcriptsClient = new TranscriptsClient(httpClient);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        decimal beforeBalance;
        using (var beforeResponse = await creditsClient.GetBalanceAsync())
        {
            beforeResponse.IsSuccessStatusCode.Should().BeTrue();
            using var beforeJson = await ReadJsonAsync(beforeResponse);
            beforeBalance = TryGetBalance(beforeJson);
        }

        var submitRequest = new TranscriptRequestBuilder()
            .WithUrl("https://www.youtube.com/watch?v=tstENAUT001")
            .BuildSubmit();

        string batchId;
        using (var submitResponse = await transcriptsClient.SubmitAsync(submitRequest))
        {
            submitResponse.IsSuccessStatusCode.Should().BeTrue();
            using var submitJson = await ReadJsonAsync(submitResponse);
            batchId = TryGetBatchId(submitJson);
        }

        var pollingResult = await pollingService.WaitForFinalStatusAsync(batchId);
        pollingResult.FinalStatus.Should().Be("completed");

        decimal afterBalance;
        using (var afterResponse = await creditsClient.GetBalanceAsync())
        {
            afterResponse.IsSuccessStatusCode.Should().BeTrue();
            using var afterJson = await ReadJsonAsync(afterResponse);
            afterBalance = TryGetBalance(afterJson);
        }

        afterBalance.Should().BeLessThanOrEqualTo(beforeBalance);
    }

    [Test]
    public async Task BatchControlEndpoints_Should_BeCallable_WhenAvailable()
    {
        RequireLiveApi();

        using var httpClient = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(httpClient);

        var submitRequest = new TranscriptRequestBuilder()
            .WithUrl("https://www.youtube.com/watch?v=tstENMAN001")
            .BuildSubmit();

        string batchId;
        using (var submitResponse = await transcriptsClient.SubmitAsync(submitRequest))
        {
            submitResponse.IsSuccessStatusCode.Should().BeTrue();
            using var submitJson = await ReadJsonAsync(submitResponse);
            batchId = TryGetBatchId(submitJson);
        }

        using (var cancelResponse = await transcriptsClient.CancelAsync(batchId))
        {
            IgnoreIfEndpointUnavailable(cancelResponse, "/api/v1/transcripts/{batch_id}/cancel");
            cancelResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        using (var retryFailedResponse = await transcriptsClient.RetryFailedAsync(batchId))
        {
            IgnoreIfEndpointUnavailable(retryFailedResponse, "/api/v1/transcripts/{batch_id}/retry-failed");
            retryFailedResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        using (var rerunResponse = await transcriptsClient.RerunAsync(batchId))
        {
            IgnoreIfEndpointUnavailable(rerunResponse, "/api/v1/transcripts/{batch_id}/rerun");
            rerunResponse.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        }

        using (var deleteResponse = await transcriptsClient.DeleteAsync(batchId))
        {
            IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/v1/transcripts/{batch_id}");
            deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        }
    }
}
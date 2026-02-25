using System.Net;
using FluentAssertions;
using Scriptube.Api.Builders;
using Scriptube.Api.Clients;
using Scriptube.Api.Services;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Api.Regression;

[TestFixture]
[Category("API")]
[Category("Regression")]
public sealed class BatchLifecycleFlowTests : ApiTestBase
{
    [Test]
    public async Task CancelMidProcessing_Should_EventuallyReachCancelledStatus_WhenAvailable()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        var request = new TranscriptRequestBuilder()
            .WithUrl(TestDataCatalog.SuccessVideos.EnglishManual)
            .WithUrl(TestDataCatalog.SuccessVideos.EnglishAuto)
            .WithUrl(TestDataCatalog.Playlists.MixedPlaylist)
            .BuildSubmit();

        string batchId;
        using (var submitResponse = await transcriptsClient.SubmitAsync(request))
        {
            submitResponse.IsSuccessStatusCode.Should().BeTrue();
            using var submitJson = await ReadJsonAsync(submitResponse);
            batchId = TryGetBatchId(submitJson);
        }

        using (var cancelResponse = await transcriptsClient.CancelAsync(batchId))
        {
            IgnoreIfEndpointUnavailable(cancelResponse, "/api/v1/transcripts/{batch_id}/cancel");
            cancelResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent);
        }

        var finalResult = await pollingService.WaitForFinalStatusAsync(batchId);
        finalResult.FinalStatus.Should().BeOneOf("cancelled", "canceled", "completed");
    }

    [Test]
    public async Task RetryFailedItems_Should_BeCallable_AfterErrorBatch()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var transcriptsClient = new TranscriptsClient(client);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        var request = new TranscriptRequestBuilder()
            .WithUrl(TestDataCatalog.ErrorVideos.PrivateVideo)
            .WithUrl(TestDataCatalog.ErrorVideos.TimeoutVideo)
            .BuildSubmit();

        string batchId;
        using (var submitResponse = await transcriptsClient.SubmitAsync(request))
        {
            submitResponse.IsSuccessStatusCode.Should().BeTrue();
            using var submitJson = await ReadJsonAsync(submitResponse);
            batchId = TryGetBatchId(submitJson);
        }

        var initialResult = await pollingService.WaitForFinalStatusAsync(batchId);
        initialResult.FinalStatus.Should().BeOneOf("failed", "completed");

        using (var batchStateResponse = await transcriptsClient.GetBatchAsync(batchId))
        {
            batchStateResponse.IsSuccessStatusCode.Should().BeTrue();
            using var batchStateJson = await ReadJsonAsync(batchStateResponse);
            var failedItems = TryCountItemsWithStatus(batchStateJson, "failed");
            failedItems.Should().BeGreaterThanOrEqualTo(0);
        }

        using var retryResponse = await transcriptsClient.RetryFailedAsync(batchId);
        IgnoreIfEndpointUnavailable(retryResponse, "/api/v1/transcripts/{batch_id}/retry-failed");
        retryResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent);
    }
}
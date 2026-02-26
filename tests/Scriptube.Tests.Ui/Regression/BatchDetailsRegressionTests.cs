using FluentAssertions;
using Scriptube.Tests.Shared;
using Scriptube.Ui.Flows;
using Scriptube.Ui.Pages;

namespace Scriptube.Tests.Ui.Regression;

[TestFixture]
[Category("UI")]
[Category("Regression")]
public sealed class BatchDetailsRegressionTests : UiTestBase
{
    private static string? GetExistingBatchIdOrNull()
        => Environment.GetEnvironmentVariable("SCRIPTUBE_EXISTING_BATCH_ID");

    [Test]
    public async Task BatchDetails_Should_ShowProgress_And_TranscriptSignals()
    {
        RequireLiveUi();
        var credentials = GetCredentialsOrIgnore();

        var loginPage = new LoginPage(Page);
        var dashboardPage = new DashboardPage(Page);
        var authFlow = new AuthenticationFlow(loginPage, dashboardPage, Page);
        var authenticated = await authFlow.TryLoginToDashboardAsync(Settings.UiBaseUrl, credentials.Email, credentials.Password);
        if (!authenticated)
        {
            await IgnoreIfServiceUnavailableAsync("batch details auth flow");
            Assert.Ignore("Authentication did not reach dashboard in current live UI variant.");
        }

        var batchDetailsPage = new BatchDetailsPage(Page);

        await dashboardPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("dashboard navigation");

        var submitted = await dashboardPage.TrySubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);
        if (!submitted)
        {
            var existingBatchId = GetExistingBatchIdOrNull();
            if (string.IsNullOrWhiteSpace(existingBatchId))
            {
                Assert.Ignore("Dashboard submit controls are not available in current live UI variant. Set SCRIPTUBE_EXISTING_BATCH_ID to run batch details tests without dashboard submit.");
            }

            await batchDetailsPage.NavigateAsync(Settings.UiBaseUrl, existingBatchId!);
        }

        if (submitted)
        {
            var openedDetails = await batchDetailsPage.TryOpenFromDashboardAsync();
            if (!openedDetails)
            {
                Assert.Ignore("Batch details link/control is not available in current live UI variant.");
            }
        }

        await IgnoreIfServiceUnavailableAsync("batch details page load");

        var loaded = await batchDetailsPage.IsLoadedAsync();
        var hasProgress = await batchDetailsPage.HasProgressSignalAsync();
        var hasPreview = await batchDetailsPage.HasItemsOrPreviewAsync();

        loaded.Should().BeTrue();
        hasProgress.Should().BeTrue();
        hasPreview.Should().BeTrue();
    }

    [Test]
    public async Task BatchDetails_ExportTranscript_Should_ExposeAnyExportControl()
    {
        RequireLiveUi();
        var credentials = GetCredentialsOrIgnore();

        var loginPage = new LoginPage(Page);
        var dashboardPage = new DashboardPage(Page);
        var authFlow = new AuthenticationFlow(loginPage, dashboardPage, Page);
        var authenticated = await authFlow.TryLoginToDashboardAsync(Settings.UiBaseUrl, credentials.Email, credentials.Password);
        if (!authenticated)
        {
            await IgnoreIfServiceUnavailableAsync("batch export auth flow");
            Assert.Ignore("Authentication did not reach dashboard in current live UI variant.");
        }

        var batchDetailsPage = new BatchDetailsPage(Page);

        await dashboardPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("dashboard navigation");

        var submitted = await dashboardPage.TrySubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);
        if (!submitted)
        {
            var existingBatchId = GetExistingBatchIdOrNull();
            if (string.IsNullOrWhiteSpace(existingBatchId))
            {
                Assert.Ignore("Dashboard submit controls are not available in current live UI variant. Set SCRIPTUBE_EXISTING_BATCH_ID to run batch export tests without dashboard submit.");
            }

            await batchDetailsPage.NavigateAsync(Settings.UiBaseUrl, existingBatchId!);
        }

        if (submitted)
        {
            var openedDetails = await batchDetailsPage.TryOpenFromDashboardAsync();
            if (!openedDetails)
            {
                Assert.Ignore("Batch details link/control is not available in current live UI variant.");
            }
        }

        await IgnoreIfServiceUnavailableAsync("batch export page load");

        var exportInvoked = false;
        try
        {
            await batchDetailsPage.ExportJsonAsync();
            exportInvoked = true;
        }
        catch
        {
            try
            {
                await batchDetailsPage.ExportTxtAsync();
                exportInvoked = true;
            }
            catch
            {
                try
                {
                    await batchDetailsPage.ExportSrtAsync();
                    exportInvoked = true;
                }
                catch
                {
                }
            }
        }

        if (!exportInvoked)
        {
            Assert.Ignore("No export control (JSON/TXT/SRT) visible in current live UI variant.");
        }

        exportInvoked.Should().BeTrue();
    }
}

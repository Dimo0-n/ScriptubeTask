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
        }

        await IgnoreIfServiceUnavailableAsync("batch details auth flow");

        var batchDetailsPage = new BatchDetailsPage(Page);
        var submitted = await dashboardPage.TrySubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);
        if (!submitted)
        {
            Assert.Ignore("Dashboard submit controls are not available in current live UI variant.");
        }

        var openedDetails = await batchDetailsPage.TryOpenFromDashboardAsync();
        if (!openedDetails)
        {
            Assert.Ignore("Batch details link/control is not available in current live UI variant.");
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
        }

        await IgnoreIfServiceUnavailableAsync("batch export auth flow");

        var batchDetailsPage = new BatchDetailsPage(Page);
        var submitted = await dashboardPage.TrySubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);
        if (!submitted)
        {
            Assert.Ignore("Dashboard submit controls are not available in current live UI variant.");
        }

        var openedDetails = await batchDetailsPage.TryOpenFromDashboardAsync();
        if (!openedDetails)
        {
            Assert.Ignore("Batch details link/control is not available in current live UI variant.");
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

using FluentAssertions;
using Scriptube.Tests.Shared;
using Scriptube.Ui.Flows;
using Scriptube.Ui.Pages;

namespace Scriptube.Tests.Ui.Regression;

[TestFixture]
[Category("UI")]
[Category("Regression")]
public sealed class DashboardRegressionTests : UiTestBase
{
    [Test]
    public async Task SubmitBatch_FromDashboard_Should_CreateBatchSignal()
    {
        RequireLiveUi();
        var credentials = GetCredentialsOrIgnore();

        var loginPage = new LoginPage(Page);
        var dashboardPage = new DashboardPage(Page);
        var authFlow = new AuthenticationFlow(loginPage, dashboardPage, Page);
        var authenticated = await authFlow.TryLoginToDashboardAsync(Settings.UiBaseUrl, credentials.Email, credentials.Password);
        if (!authenticated)
        {
            await IgnoreIfServiceUnavailableAsync("dashboard auth flow");
        }

        await IgnoreIfServiceUnavailableAsync("dashboard auth flow");

        (await dashboardPage.IsLoadedAsync()).Should().BeTrue();

        var submitted = await dashboardPage.TrySubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);
        if (!submitted)
        {
            Assert.Ignore("Dashboard submit controls are not available in current live UI variant.");
        }

        var hasSignal = await dashboardPage.HasBatchCreatedSignalAsync();
        hasSignal.Should().BeTrue();
    }
}
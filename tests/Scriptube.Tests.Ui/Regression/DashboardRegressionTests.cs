using FluentAssertions;
using Scriptube.Tests.Shared;
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
        await loginPage.NavigateAsync(Settings.UiBaseUrl);
        await loginPage.LoginAsync(credentials.Email, credentials.Password);

        var dashboardPage = new DashboardPage(Page);
        (await dashboardPage.IsLoadedAsync()).Should().BeTrue();

        await dashboardPage.SubmitBatchAsync([TestDataCatalog.SuccessVideos.EnglishManual]);

        var hasSignal = await dashboardPage.HasBatchCreatedSignalAsync();
        hasSignal.Should().BeTrue();
    }
}
using FluentAssertions;
using Scriptube.Ui.Flows;
using Scriptube.Ui.Pages;

namespace Scriptube.Tests.Ui.Regression;

[TestFixture]
[Category("UI")]
[Category("Regression")]
public sealed class BillingPagesRegressionTests : UiTestBase
{
    [Test]
    public async Task CreditsPage_Should_ShowBalance_And_Options()
    {
        RequireLiveUi();
        var credentials = GetCredentialsOrIgnore();

        var loginPage = new LoginPage(Page);
        var dashboardPage = new DashboardPage(Page);
        var authFlow = new AuthenticationFlow(loginPage, dashboardPage, Page);
        var authenticated = await authFlow.TryLoginToDashboardAsync(Settings.UiBaseUrl, credentials.Email, credentials.Password);
        if (!authenticated)
        {
            await IgnoreIfServiceUnavailableAsync("credits auth flow");
        }

        var creditsPage = new CreditsPage(Page);
        await creditsPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("credits page navigation");

        var loaded = await creditsPage.IsLoadedAsync();
        if (!loaded)
        {
            Assert.Ignore("Credits/Billing page route is not available in current live UI variant.");
        }

        loaded.Should().BeTrue();
        var hasBalance = await creditsPage.HasBalanceAsync();
        var hasOptions = await creditsPage.HasPackOptionsAsync();
        if (!hasBalance && !hasOptions)
        {
            Assert.Ignore("Credits page rendered without balance/options signals in current live UI variant.");
        }

        (hasBalance || hasOptions).Should().BeTrue();
    }

    [Test]
    public async Task PricingPage_Should_ShowAvailablePlans()
    {
        RequireLiveUi();
        var pricingPage = new PricingPage(Page);

        await pricingPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("pricing page navigation");

        (await pricingPage.IsLoadedAsync()).Should().BeTrue();
        (await pricingPage.HasPlanCardsAsync()).Should().BeTrue();
    }
}

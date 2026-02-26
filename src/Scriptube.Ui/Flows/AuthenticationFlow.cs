using Scriptube.Ui.Pages;

namespace Scriptube.Ui.Flows;

public sealed class AuthenticationFlow
{
    private readonly LoginPage _loginPage;
    private readonly DashboardPage _dashboardPage;
    private readonly Microsoft.Playwright.IPage _page;

    public AuthenticationFlow(LoginPage loginPage, DashboardPage dashboardPage, Microsoft.Playwright.IPage page)
    {
        _loginPage = loginPage;
        _dashboardPage = dashboardPage;
        _page = page;
    }

    public async Task LoginToDashboardAsync(string uiBaseUrl, string email, string password)
    {
        var loaded = await TryLoginToDashboardAsync(uiBaseUrl, email, password);
        if (!loaded)
        {
            throw new InvalidOperationException("Authentication flow did not reach dashboard.");
        }
    }

    public async Task<bool> TryLoginToDashboardAsync(string uiBaseUrl, string email, string password)
    {
        await _loginPage.NavigateAsync(uiBaseUrl);

        var alreadyLoaded = await _dashboardPage.IsLoadedAsync(8000);
        if (alreadyLoaded)
        {
            return true;
        }

        if (await _loginPage.IsLoginFormVisibleAsync())
        {
            await _loginPage.LoginAsync(email, password);
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        }

        return await _dashboardPage.IsLoadedAsync(20000);
    }
}

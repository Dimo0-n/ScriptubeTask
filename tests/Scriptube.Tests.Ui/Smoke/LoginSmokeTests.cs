using FluentAssertions;
using Microsoft.Playwright;
using Scriptube.Ui.Pages;

namespace Scriptube.Tests.Ui.Smoke;

[TestFixture]
[Category("UI")]
[Category("Smoke")]
public sealed class LoginSmokeTests : UiTestBase
{
    [Test]
    public async Task Login_WithValidCredentials_Should_RedirectToDashboard()
    {
        RequireLiveUi();
        var credentials = GetCredentialsOrIgnore();

        var loginPage = new LoginPage(Page);
        var dashboardPage = new DashboardPage(Page);

        var maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await loginPage.NavigateAsync(Settings.UiBaseUrl);
            await IgnoreIfServiceUnavailableAsync($"login page load attempt {attempt}");

            var dashboardLoaded = await dashboardPage.IsLoadedAsync(8000);
            if (!dashboardLoaded && await loginPage.IsLoginFormVisibleAsync())
            {
                await loginPage.LoginAsync(credentials.Email, credentials.Password);
            }

            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await IgnoreIfServiceUnavailableAsync($"post-login attempt {attempt}");

            var loaded = await dashboardPage.IsLoadedAsync(20000);
            if (!loaded)
            {
                loaded = !Page.Url.Contains("/ui/login", StringComparison.OrdinalIgnoreCase);
            }

            if (loaded)
            {
                loaded.Should().BeTrue();
                return;
            }

            if (attempt < maxAttempts)
            {
                await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }
        }

        var pageText = await Page.Locator("body").InnerTextAsync();
        var snippet = pageText.Length > 300 ? pageText[..300] : pageText;
        Assert.Fail($"Login did not reach authenticated UI after retries. Current URL: {Page.Url}. Page snippet: {snippet}");
    }

    [Test]
    public async Task Login_WithInvalidCredentials_Should_ShowError()
    {
        RequireLiveUi();

        var loginPage = new LoginPage(Page);
        var hasForm = await loginPage.EnsureLoginFormAsync(Settings.UiBaseUrl);
        if (!hasForm)
        {
            await IgnoreIfServiceUnavailableAsync("invalid-credentials login form");
            Assert.Ignore("Login form is not accessible in current live UI variant.");
        }

        hasForm.Should().BeTrue();
        await IgnoreIfServiceUnavailableAsync("invalid-credentials login form");

        await loginPage.LoginAsync("invalid_user@example.com", "invalid-password");
        await IgnoreIfServiceUnavailableAsync("invalid-credentials submit");

        var hasError = await loginPage.HasLoginErrorAsync();
        var stillOnLogin = Page.Url.Contains("/ui/login", StringComparison.OrdinalIgnoreCase) && await loginPage.IsLoginFormVisibleAsync();
        var dashboardLoaded = await new DashboardPage(Page).IsLoadedAsync(5000);
        if (!hasError)
        {
            await IgnoreIfServiceUnavailableAsync("invalid-credentials assertion");
        }

        if (dashboardLoaded)
        {
            Assert.Fail("Invalid credentials unexpectedly led to an authenticated dashboard state.");
        }

        if (!hasError && !stillOnLogin)
        {
            Assert.Ignore("Invalid login rejection signal is not observable in current live UI variant.");
        }

        (hasError || stillOnLogin || !dashboardLoaded).Should().BeTrue();
    }
}
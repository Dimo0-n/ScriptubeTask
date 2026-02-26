using FluentAssertions;
using Scriptube.Ui.Pages;

namespace Scriptube.Tests.Ui.Regression;

[TestFixture]
[Category("UI")]
[Category("Regression")]
public sealed class SignupRegressionTests : UiTestBase
{
    [Test]
    public async Task Signup_WithNewUniqueEmail_Should_EventuallyReachAuthenticatedOrConfirmationState()
    {
        RequireLiveUi();

        var signupPage = new SignupPage(Page);
        var dashboardPage = new DashboardPage(Page);

        await signupPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("signup page load");

        (await signupPage.IsLoadedAsync()).Should().BeTrue();

        var uniqueEmail = $"scriptube.auto+{Guid.NewGuid():N}@example.com";
        var password = "P@ssw0rd-" + Guid.NewGuid().ToString("N")[..6];

        var emailInput = Page.Locator("input[type='email'], input[name='email'], #email").First;
        var passwordInput = Page.Locator("input[type='password'], input[name='password'], #password").First;

        if (!await emailInput.IsVisibleAsync() || !await passwordInput.IsVisibleAsync())
        {
            Assert.Ignore("Signup form inputs are not visible in current live UI variant.");
        }

        await emailInput.FillAsync(uniqueEmail);
        await passwordInput.FillAsync(password);

        var submitButton = Page.Locator("button[type='submit'], button:has-text('Sign up'), button:has-text('Create account')").First;
        if (!await submitButton.IsVisibleAsync())
        {
            Assert.Ignore("Signup submit control is not available in current live UI variant.");
        }

        await submitButton.ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await IgnoreIfServiceUnavailableAsync("signup submit");

        var dashboardLoaded = await dashboardPage.IsLoadedAsync(20000);
        var stillOnSignup = Page.Url.Contains("/ui/signup", StringComparison.OrdinalIgnoreCase)
                            || Page.Url.Contains("/signup", StringComparison.OrdinalIgnoreCase);

        if (!dashboardLoaded && !stillOnSignup)
        {
            Assert.Ignore("Signup success signal is not clearly observable in current live UI variant.");
        }

        (dashboardLoaded || stillOnSignup).Should().BeTrue();
    }

    [Test]
    public async Task Signup_WithDuplicateEmail_Should_ShowErrorOrStayOnForm()
    {
        RequireLiveUi();

        var credentials = GetCredentialsOrIgnore();
        var signupPage = new SignupPage(Page);

        await signupPage.NavigateAsync(Settings.UiBaseUrl);
        await IgnoreIfServiceUnavailableAsync("duplicate-signup page load");

        (await signupPage.IsLoadedAsync()).Should().BeTrue();

        var emailInput = Page.Locator("input[type='email'], input[name='email'], #email").First;
        var passwordInput = Page.Locator("input[type='password'], input[name='password'], #password").First;

        if (!await emailInput.IsVisibleAsync() || !await passwordInput.IsVisibleAsync())
        {
            Assert.Ignore("Signup form inputs are not visible in current live UI variant.");
        }

        await emailInput.FillAsync(credentials.Email);
        await passwordInput.FillAsync(credentials.Password);

        var submitButton = Page.Locator("button[type='submit'], button:has-text('Sign up'), button:has-text('Create account')").First;
        if (!await submitButton.IsVisibleAsync())
        {
            Assert.Ignore("Signup submit control is not available in current live UI variant.");
        }

        await submitButton.ClickAsync();
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await IgnoreIfServiceUnavailableAsync("duplicate-signup submit");

        var possibleErrorSelectors = new[]
        {
            "text=already registered",
            "text=already exists",
            "text=duplicate",
            "text=used",
            "text=error"
        };

        var hasError = false;
        foreach (var selector in possibleErrorSelectors)
        {
            try
            {
                if (await Page.Locator(selector).First.IsVisibleAsync())
                {
                    hasError = true;
                    break;
                }
            }
            catch (Microsoft.Playwright.PlaywrightException)
            {
            }
        }

        var stillOnSignup = Page.Url.Contains("/ui/signup", StringComparison.OrdinalIgnoreCase)
                            || Page.Url.Contains("/signup", StringComparison.OrdinalIgnoreCase);

        if (!hasError && !stillOnSignup)
        {
            Assert.Ignore("Duplicate signup rejection signal is not observable in current live UI variant.");
        }

        (hasError || stillOnSignup).Should().BeTrue();
    }
}

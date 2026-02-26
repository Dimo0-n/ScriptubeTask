using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl, CancellationToken cancellationToken = default)
    {
        var loginUrl = $"{uiBaseUrl.TrimEnd('/')}/login";
        await _page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task<bool> EnsureLoginFormAsync(string uiBaseUrl)
    {
        await NavigateAsync(uiBaseUrl);
        if (await IsLoginFormVisibleAsync())
        {
            return true;
        }

        var logoutUrl = $"{uiBaseUrl.TrimEnd('/')}/logout";
        await _page.GotoAsync(logoutUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await NavigateAsync(uiBaseUrl);

        return await IsLoginFormVisibleAsync();
    }

    public async Task<bool> IsLoginFormVisibleAsync()
    {
        var selectors = new[] { "input[type='email']", "input[name='email']", "#email" };
        foreach (var selector in selectors)
        {
            if (await _page.Locator(selector).First.IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }

    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        await FillFirstAvailableAsync(["input[type='email']", "input[name='email']", "#email"], email);
        await FillFirstAvailableAsync(["input[type='password']", "input[name='password']", "#password"], password);

        var beforeUrl = _page.Url;
        await ClickFirstAvailableAsync([
            "button[type='submit']",
            "button:has-text('Login')",
            "button:has-text('Log in')",
            "button:has-text('Sign in')"
        ]);

        try
        {
            await _page.WaitForURLAsync("**/ui/**", new PageWaitForURLOptions { Timeout = 15000 });
        }
        catch (PlaywrightException)
        {
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        if (string.Equals(beforeUrl, _page.Url, StringComparison.OrdinalIgnoreCase))
        {
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        if (await IsLoginFormVisibleAsync())
        {
            var passwordInput = _page.Locator("input[type='password'], input[name='password'], #password").First;
            try
            {
                await passwordInput.PressAsync("Enter");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (PlaywrightException)
            {
            }
        }

        if (await IsLoginFormVisibleAsync())
        {
            var form = _page.Locator("form#login-form").First;
            try
            {
                await form.EvaluateAsync("form => form.submit()");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (PlaywrightException)
            {
            }
        }
    }

    public async Task<bool> HasLoginErrorAsync()
    {
        var possibleErrors = new[]
        {
            "text=invalid",
            "text=incorrect",
            "text=failed",
            "text=error"
        };

        foreach (var selector in possibleErrors)
        {
            if (await _page.Locator(selector).First.IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }

    private async Task FillFirstAvailableAsync(IReadOnlyCollection<string> selectors, string value)
    {
        var locator = await FindFirstVisibleLocatorAsync(selectors, timeoutMs: 12000);
        if (locator is not null)
        {
            await locator.FillAsync(value);
            return;
        }

        throw new InvalidOperationException($"None of the selectors became visible: {string.Join(", ", selectors)}");
    }

    private async Task ClickFirstAvailableAsync(IReadOnlyCollection<string> selectors)
    {
        var locator = await FindFirstVisibleLocatorAsync(selectors, timeoutMs: 12000);
        if (locator is not null)
        {
            await locator.ClickAsync();
            return;
        }

        throw new InvalidOperationException($"None of the selectors became visible: {string.Join(", ", selectors)}");
    }

    private async Task<ILocator?> FindFirstVisibleLocatorAsync(IReadOnlyCollection<string> selectors, int timeoutMs)
    {
        var start = DateTime.UtcNow;

        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            foreach (var selector in selectors)
            {
                var locator = _page.Locator(selector).First;
                try
                {
                    if (await locator.IsVisibleAsync())
                    {
                        return locator;
                    }
                }
                catch (PlaywrightException)
                {
                }
            }

            await Task.Delay(200);
        }

        return null;
    }
}
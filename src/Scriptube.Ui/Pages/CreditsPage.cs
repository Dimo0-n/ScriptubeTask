using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class CreditsPage
{
    private readonly IPage _page;

    public CreditsPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl)
    {
        var candidates = new[]
        {
            $"{uiBaseUrl.TrimEnd('/')}/credits",
            $"{uiBaseUrl.TrimEnd('/')}/billing",
            $"{uiBaseUrl.TrimEnd('/')}/plans"
        };

        foreach (var url in candidates)
        {
            await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            if (await IsLoadedAsync(4000))
            {
                return;
            }
        }
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("/credits", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/billing", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/plans", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var anchors = new[] { "text=Credits", "text=Balance", "text=credit", "text=Top up", "text=Buy" };
            foreach (var selector in anchors)
            {
                try
                {
                    if (await _page.Locator(selector).First.IsVisibleAsync())
                    {
                        return true;
                    }
                }
                catch (PlaywrightException)
                {
                }
            }

            await Task.Delay(250);
        }

        return false;
    }

    public async Task<bool> HasBalanceAsync()
    {
        var selectors = new[]
        {
            "text=Balance",
            "text=credits",
            "[data-testid='credits-balance']",
            "text=/\\d+\\s*credits/i"
        };

        foreach (var selector in selectors)
        {
            try
            {
                if (await _page.Locator(selector).First.IsVisibleAsync())
                {
                    return true;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        return false;
    }

    public async Task<bool> HasPackOptionsAsync()
    {
        var selectors = new[]
        {
            "text=Buy",
            "text=Top up",
            "text=package",
            "text=pack",
            "text=Pro",
            "text=credits"
        };

        foreach (var selector in selectors)
        {
            try
            {
                if (await _page.Locator(selector).First.IsVisibleAsync())
                {
                    return true;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        return false;
    }
}

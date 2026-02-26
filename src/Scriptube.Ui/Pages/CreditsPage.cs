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
            if (_page.Url.Contains("/ui/credits", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/ui/billing", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/ui/plans", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var anchors = new[]
            {
                "h1:has-text('Credits')",
                ".credit-summary",
                ".summary-card.balance",
                "text=Credits",
                "text=Balance",
                "text=credit",
                "text=Buy"
            };
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

    public async Task<bool> HasBalanceAsync(int timeoutMs = 8000)
    {
        var selectors = new[]
        {
            ".summary-card.balance .card-value",
            "text=Balance",
            "text=credits",
            "[data-testid='credits-balance']",
            "text=/\\d+\\s*credits/i"
        };

        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
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

            await Task.Delay(300);
        }

        return false;
    }

    public async Task<bool> HasPackOptionsAsync(int timeoutMs = 8000)
    {
        var selectors = new[]
        {
            ".credit-packs-grid",
            ".pack-card",
            "text=Buy More Credits",
            "text=Buy",
            "text=pack",
            "text=credits"
        };

        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
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

            await Task.Delay(300);
        }

        return false;
    }
}

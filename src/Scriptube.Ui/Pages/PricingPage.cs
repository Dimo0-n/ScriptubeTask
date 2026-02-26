using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class PricingPage
{
    private readonly IPage _page;

    public PricingPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl)
    {
        var pricingUrl = $"{uiBaseUrl.TrimEnd('/')}/pricing";
        await _page.GotoAsync(pricingUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("/pricing", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var anchors = new[] { "text=Pricing", "text=Plan", "text=Free", "text=Pro" };
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

    public async Task<bool> HasPlanCardsAsync()
    {
        var selectors = new[]
        {
            "text=Free",
            "text=Pro",
            "text=Enterprise",
            "text=month",
            "[data-testid='plan-card']"
        };

        var visibleCount = 0;
        foreach (var selector in selectors)
        {
            try
            {
                if (await _page.Locator(selector).First.IsVisibleAsync())
                {
                    visibleCount++;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        return visibleCount >= 2;
    }
}

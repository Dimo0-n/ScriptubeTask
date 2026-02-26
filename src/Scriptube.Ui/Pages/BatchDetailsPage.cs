using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class BatchDetailsPage
{
    private readonly IPage _page;

    public BatchDetailsPage(IPage page)
    {
        _page = page;
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("/batch", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/batches/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var anchors = new[]
            {
                "text=Batch details",
                "text=Status",
                "text=Transcript",
                "text=Export"
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

    public async Task<bool> HasProgressSignalAsync()
    {
        var selectors = new[] { "text=queued", "text=processing", "text=completed", "text=failed", "text=progress" };
        foreach (var selector in selectors)
        {
            if (await _page.Locator(selector).First.IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasItemsOrPreviewAsync()
    {
        var selectors = new[]
        {
            "text=Items",
            "text=Preview",
            "text=Transcript",
            "[data-testid='transcript-preview']",
            "pre"
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

    public async Task<bool> TryOpenFromDashboardAsync(int timeoutMs = 10000)
    {
        var selectors = new[]
        {
            "a[href*='/ui/batch/']",
            "a[href*='/ui/batches/']",
            "a:has-text('View')",
            "a:has-text('Details')",
            "button:has-text('View')",
            "button:has-text('Details')"
        };

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
                        await locator.ClickAsync();
                        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

    public async Task ExportJsonAsync()
    {
        await ClickFirstVisibleAsync(
            "a:has-text('JSON')",
            "button:has-text('JSON')",
            "a[href*='format=json']",
            "a[href*='/export?format=json']");
    }

    public async Task ExportTxtAsync()
    {
        await ClickFirstVisibleAsync(
            "a:has-text('TXT')",
            "button:has-text('TXT')",
            "a[href*='format=txt']",
            "a[href*='/export?format=txt']");
    }

    public async Task ExportSrtAsync()
    {
        await ClickFirstVisibleAsync(
            "a:has-text('SRT')",
            "button:has-text('SRT')",
            "a[href*='format=srt']",
            "a[href*='/export?format=srt']");
    }

    private async Task ClickFirstVisibleAsync(params string[] selectors)
    {
        foreach (var selector in selectors)
        {
            var locator = _page.Locator(selector).First;
            try
            {
                if (await locator.IsVisibleAsync())
                {
                    await locator.ClickAsync();
                    return;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        throw new InvalidOperationException($"Could not find any visible export control. Selectors: {string.Join(", ", selectors)}");
    }
}

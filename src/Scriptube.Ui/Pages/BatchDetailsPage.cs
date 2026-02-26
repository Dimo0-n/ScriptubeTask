using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class BatchDetailsPage
{
    private readonly IPage _page;

    public BatchDetailsPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl, string batchId)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            throw new ArgumentException("Batch id must be provided.", nameof(batchId));
        }

        var url = $"{uiBaseUrl.TrimEnd('/')}/batches/{batchId.Trim()}";
        await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("/ui/batches/", StringComparison.OrdinalIgnoreCase)
                || _page.Url.Contains("/ui/batch/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var anchors = new[]
            {
                "text=Batch #",
                "text=Back to dashboard",
                "text=Export TXT",
                "text=Export CSV",
                "text=Export JSONL"
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
        var selectors = new[]
        {
            "span.badge",
            "text=queued",
            "text=processing",
            "text=completed",
            "text=failed",
            "text=progress",
            "text=Total items",
            "text=Completed"
        };
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
            ".transcript-preview",
            "text=Copy Transcript",
            "text=Transcript",
            "text=Items",
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
            "tr[data-batch-id] a",
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
            "a:has-text('JSONL')",
            "button:has-text('JSONL')",
            "a[href*='fmt=jsonl']",
            "a[href*='/export?fmt=jsonl']");
    }

    public async Task ExportTxtAsync()
    {
        await ClickFirstVisibleAsync(
            "a:has-text('Export TXT')",
            "a:has-text('TXT')",
            "button:has-text('TXT')",
            "a[href*='fmt=txt']",
            "a[href*='/export?fmt=txt']");
    }

    public async Task ExportSrtAsync()
    {
        await ClickFirstVisibleAsync(
            "a:has-text('Export CSV')",
            "a:has-text('CSV')",
            "button:has-text('CSV')",
            "a[href*='fmt=csv']",
            "a[href*='/export?fmt=csv']");
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

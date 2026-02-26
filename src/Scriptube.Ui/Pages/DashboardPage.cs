using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class DashboardPage
{
    private readonly IPage _page;

    public DashboardPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl)
    {
        var dashboardUrl = $"{uiBaseUrl.TrimEnd('/')}/dashboard";
        await _page.GotoAsync(dashboardUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("/ui/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Use dashboard-specific anchors to avoid false positives on other /ui pages.
            var anchors = new[]
            {
                "#submit-form",
                "#urls",
                "#submit-batch-btn",
                "form[action='/ui/batches']",
                "h2:has-text('Submit YouTube URLs')"
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

    public async Task SubmitBatchAsync(IEnumerable<string> urls)
    {
        var submitted = await TrySubmitBatchAsync(urls);
        if (!submitted)
        {
            throw new InvalidOperationException("Could not find batch input textarea on dashboard.");
        }
    }

    public async Task<bool> TrySubmitBatchAsync(IEnumerable<string> urls)
    {
        // Wait for the SPA to fully render the form before probing for controls.
        try { await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 8000 }); } catch (PlaywrightException) { }

        var joinedUrls = string.Join(Environment.NewLine, urls);

        var textAreaSelectors = new[]
        {
            "#urls",
            "textarea#urls",
            "textarea[name='urls']",
            "textarea[placeholder*='YouTube']",
            "textarea[placeholder*='youtube']",
            "input[name='urls']",
            "input[placeholder*='YouTube']",
            "[data-testid='urls-input']",
            "textarea"
        };

        ILocator? textArea = null;
        foreach (var selector in textAreaSelectors)
        {
            var locator = _page.Locator(selector).First;
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 2500 });
                textArea = locator;
                break;
            }
            catch (PlaywrightException)
            {
            }
        }

        if (textArea is null)
        {
            return false;
        }

        await textArea.FillAsync(joinedUrls);

        var submitSelectors = new[]
        {
            "#submit-batch-btn",
            "button:has-text('Get Transcripts')",
            "button:has-text('Submit')",
            "button:has-text('Create Batch')",
            "button:has-text('Process')",
            "button[type='submit']"
        };

        foreach (var selector in submitSelectors)
        {
            var button = _page.Locator(selector).First;
            try
            {
                await button.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 2500 });
                if (await button.IsEnabledAsync())
                {
                    await button.ClickAsync();
                    return true;
                }
            }
            catch (PlaywrightException)
            {
            }
        }

        return false;
    }

    public async Task<bool> HasBatchCreatedSignalAsync()
    {
        if (_page.Url.Contains("/ui/batches/", StringComparison.OrdinalIgnoreCase)
            || _page.Url.Contains("/ui/batch/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var signals = new[]
        {
            "tr[data-batch-id]",
            "tr[data-batch-status]",
            "text=processing",
            "text=queued",
            "text=batch",
            "text=status"
        };

        foreach (var selector in signals)
        {
            if (await _page.Locator(selector).First.IsVisibleAsync())
            {
                return true;
            }
        }

        return false;
    }
}
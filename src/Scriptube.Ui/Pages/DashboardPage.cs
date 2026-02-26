using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class DashboardPage
{
    private readonly IPage _page;

    public DashboardPage(IPage page)
    {
        _page = page;
    }

    public async Task<bool> IsLoadedAsync(int timeoutMs = 15000)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < timeoutMs)
        {
            if (_page.Url.Contains("dashboard", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsAuthenticatedUrl(_page.Url))
            {
                return true;
            }

            var possibleLocators = new[]
            {
                "textarea",
                "text=Batch",
                "text=Dashboard",
                "text=Credits",
                "a[href='/ui/logout']",
                "text=Logout",
                "text=API Keys"
            };

            foreach (var selector in possibleLocators)
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

    private static bool IsAuthenticatedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!url.Contains("/ui", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !url.Contains("/ui/login", StringComparison.OrdinalIgnoreCase)
               && !url.Contains("/ui/signup", StringComparison.OrdinalIgnoreCase);
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
        var joinedUrls = string.Join(Environment.NewLine, urls);

        var textAreaSelectors = new[]
        {
            "textarea",
            "textarea[name='urls']",
            "textarea[placeholder*='YouTube']",
            "textarea[placeholder*='youtube']",
            "input[name='urls']",
            "input[placeholder*='YouTube']",
            "[data-testid='urls-input']"
        };

        ILocator? textArea = null;
        foreach (var selector in textAreaSelectors)
        {
            var locator = _page.Locator(selector).First;
            if (await locator.IsVisibleAsync())
            {
                textArea = locator;
                break;
            }
        }

        if (textArea is null)
        {
            return false;
        }

        await textArea.FillAsync(joinedUrls);

        var submitSelectors = new[]
        {
            "button:has-text('Submit')",
            "button:has-text('Create Batch')",
            "button:has-text('Process')",
            "button[type='submit']"
        };

        foreach (var selector in submitSelectors)
        {
            var button = _page.Locator(selector).First;
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
                return true;
            }
        }

        return false;
    }

    public async Task<bool> HasBatchCreatedSignalAsync()
    {
        var signals = new[]
        {
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
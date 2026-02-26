using Microsoft.Playwright;

namespace Scriptube.Ui.Pages;

public sealed class SignupPage
{
    private readonly IPage _page;

    public SignupPage(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync(string uiBaseUrl)
    {
        var signupUrl = $"{uiBaseUrl.TrimEnd('/')}/signup";
        await _page.GotoAsync(signupUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task<bool> IsLoadedAsync()
    {
        return await _page.Locator("text=Sign up").First.IsVisibleAsync()
               || await _page.Locator("text=Create account").First.IsVisibleAsync()
               || await _page.Locator("button[type='submit']").First.IsVisibleAsync();
    }
}
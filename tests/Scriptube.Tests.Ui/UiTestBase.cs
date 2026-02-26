using Microsoft.Playwright.NUnit;
using Scriptube.Core.Configuration;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Ui;

public abstract class UiTestBase : PageTest
{
    protected ScriptubeSettings Settings { get; private set; } = default!;

    [SetUp]
    public void UiSetUp()
    {
        DotEnvLoader.LoadFromWorkspaceRoot();
        Settings = ScriptubeSettingsLoader.Load();
    }

    [TearDown]
    public async Task UiTearDown()
    {
        var status = TestContext.CurrentContext.Result.Outcome.Status;
        if (status != NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            return;
        }

        var path = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "TestResults",
            "ui-screenshots",
            $"{TestContext.CurrentContext.Test.Name}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await Page.ScreenshotAsync(new() { Path = path, FullPage = true });
        TestContext.AddTestAttachment(path, "UI failure screenshot");
    }

    protected void RequireLiveUi()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_UI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Set RUN_LIVE_UI=true to run live UI tests.");
        }
    }

    protected (string Email, string Password) GetCredentialsOrIgnore()
    {
        var email = Environment.GetEnvironmentVariable("SCRIPTUBE_EMAIL");
        var password = Environment.GetEnvironmentVariable("SCRIPTUBE_PASSWORD");

        email = string.IsNullOrWhiteSpace(email) ? Settings.Credentials.Email : email;
        password = string.IsNullOrWhiteSpace(password) ? Settings.Credentials.Password : password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Ignore("Set SCRIPTUBE_EMAIL and SCRIPTUBE_PASSWORD to run authenticated UI flows.");
        }

        return (email!, password!);
    }

    protected async Task<bool> IsServiceUnavailableAsync()
    {
        try
        {
            var body = await Page.ContentAsync();
            return body.Contains("503 Service Temporarily Unavailable", StringComparison.OrdinalIgnoreCase)
                   || body.Contains("<title>503", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    protected async Task IgnoreIfServiceUnavailableAsync(string context)
    {
        if (await IsServiceUnavailableAsync())
        {
            Assert.Ignore($"Scriptube UI unavailable (503) during {context}. Skipping transient external failure.");
        }
    }
}
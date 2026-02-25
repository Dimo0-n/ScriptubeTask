using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Webhooks.Regression;

[TestFixture]
[Category("Webhook")]
[Category("Regression")]
public sealed class WebhookRegressionScaffoldTests : ScriptubeTestBase
{
    [Test]
    public void WebhookRegistration_IsPendingImplementation()
    {
        Assert.Ignore("Webhook flow implementation starts in next iteration.");
    }
}
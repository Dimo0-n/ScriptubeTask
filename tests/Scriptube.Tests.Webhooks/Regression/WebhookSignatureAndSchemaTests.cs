using FluentAssertions;
using Scriptube.Webhooks.Clients;
using Scriptube.Webhooks.Signature;

namespace Scriptube.Tests.Webhooks.Regression;

[TestFixture]
[Category("Webhook")]
[Category("Regression")]
public sealed class WebhookSignatureAndSchemaTests
{
    [Test]
    public void HmacSignatureVerifier_Should_ValidateKnownSignature()
    {
        const string payload = "{\"batch_id\":\"abc\",\"status\":\"completed\",\"items\":[]}";
        const string secret = "test-secret";

        var signature = HmacSignatureVerifier.ComputeSignature(payload, secret);

        var isValid = HmacSignatureVerifier.VerifySignature(payload, secret, signature);
        isValid.Should().BeTrue();
    }

    [Test]
    public void WebhookDeliveryVerifier_Should_ParseAndValidateBasicSchema()
    {
        const string payload = """
        {
          "batch_id": "test-batch-id",
          "status": "completed",
          "items": [
            { "url": "https://www.youtube.com/watch?v=tstENMAN001", "status": "completed" }
          ]
        }
        """;

        var parsed = WebhookDeliveryVerifier.ParseAndValidate(payload);

        parsed.BatchId.Should().Be("test-batch-id");
        parsed.Status.Should().Be("completed");
        parsed.Items.Should().HaveCount(1);
    }
}
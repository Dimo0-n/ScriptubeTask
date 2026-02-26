using FluentAssertions;
using Scriptube.Webhooks.Clients;
using Scriptube.Webhooks.Listeners;
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

    [Test]
    public void WebhookRegistrationClient_Should_ExtractWebhookId_FromResponse()
    {
        const string payload = """
        {
          "webhook_id": "wh_123"
        }
        """;

        var webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(payload);

        webhookId.Should().Be("wh_123");
    }

    [Test]
    public async Task WebhookTestListener_Should_CaptureIncomingDelivery()
    {
        await using var listener = WebhookTestListener.StartLocal(path: "/hook/");

        using var httpClient = new HttpClient();
        using var content = new StringContent("{\"status\":\"completed\"}", System.Text.Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync(listener.CallbackUrl, content);

        response.IsSuccessStatusCode.Should().BeTrue();

        var delivery = await listener.WaitForDeliveryAsync(TimeSpan.FromSeconds(3));

        delivery.Should().NotBeNull();
        delivery!.Method.Should().Be("POST");
        delivery.Path.Should().Be("/hook/");
        delivery.Body.Should().Contain("\"status\":\"completed\"");
    }
}

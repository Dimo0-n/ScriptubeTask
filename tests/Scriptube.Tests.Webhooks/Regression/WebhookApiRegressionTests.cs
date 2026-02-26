using System.Net;
using System.Text.Json;
using FluentAssertions;
using Scriptube.Api.Clients;
using Scriptube.Api.Contracts;

namespace Scriptube.Tests.Webhooks.Regression;

[TestFixture]
[Category("Webhook")]
[Category("Regression")]
public sealed class WebhookApiRegressionTests : WebhookTestBase
{
    [Test]
    public async Task RegisterWebhook_WithValidHttps_Should_ReturnCreated_WhenConfigured()
    {
        RequireLiveApi();
        var webhookUrl = RequireWebhookUrlOrIgnore();

        using var client = CreateAuthenticatedClient();
        var webhooksClient = new WebhooksClient(client);

        var request = new WebhookRegisterRequest
        {
            Url = webhookUrl,
            Events = ["batch.completed"]
        };

        using var response = await webhooksClient.RegisterAsync(request);
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Test]
    public async Task RegisterWebhook_WithLocalhost_Should_BeRejected()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var webhooksClient = new WebhooksClient(client);

        var request = new WebhookRegisterRequest
        {
            Url = "http://localhost/test",
            Events = ["batch.completed"]
        };

        using var response = await webhooksClient.RegisterAsync(request);
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AvailableEventsEndpoint_Should_ReturnSuccess()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var webhooksClient = new WebhooksClient(client);

        using var response = await webhooksClient.ListAvailableEventsAsync();
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/events/available");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task TriggerTestAndLogs_Should_Work_WhenWebhookIsRegistered()
    {
        RequireLiveApi();
        var webhookUrl = RequireWebhookUrlOrIgnore();

        using var client = CreateAuthenticatedClient();
        var webhooksClient = new WebhooksClient(client);

        var registerRequest = new WebhookRegisterRequest
        {
            Url = webhookUrl,
            Events = ["batch.completed"]
        };

        string webhookId;
        using (var registerResponse = await webhooksClient.RegisterAsync(registerRequest))
        {
            IgnoreIfEndpointUnavailable(registerResponse, "/api/webhooks/register");
            registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            var body = await registerResponse.Content.ReadAsStringAsync();
            webhookId = ExtractWebhookId(body);
        }

        using (var triggerResponse = await webhooksClient.TriggerTestAsync(webhookId))
        {
            IgnoreIfEndpointUnavailable(triggerResponse, "/api/webhooks/{webhook_id}/test");
            triggerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent);
        }

        using (var logsResponse = await webhooksClient.GetLogsAsync(webhookId))
        {
            IgnoreIfEndpointUnavailable(logsResponse, "/api/webhooks/{webhook_id}/logs");
            logsResponse.IsSuccessStatusCode.Should().BeTrue();
        }

        using (var retryResponse = await webhooksClient.RetryDeliveryAsync(webhookId))
        {
            IgnoreIfEndpointUnavailable(retryResponse, "/api/webhooks/{webhook_id}/retry");
            retryResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
        }

        using (var deleteResponse = await webhooksClient.DeleteAsync(webhookId))
        {
            IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/webhooks/{webhook_id}");
            deleteResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Accepted, HttpStatusCode.NotFound);
        }
    }

    private static string ExtractWebhookId(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (TryGetString(document.RootElement, "webhook_id", out var webhookId) ||
            TryGetString(document.RootElement, "id", out webhookId))
        {
            return webhookId!;
        }

        throw new AssertionException("Webhook register response does not include webhook id.");
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string? value)
    {
        value = null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }
}
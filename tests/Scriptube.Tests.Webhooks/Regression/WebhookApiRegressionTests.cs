using System.Net;
using FluentAssertions;
using Scriptube.Api.Clients;
using Scriptube.Webhooks.Clients;

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
        var registrationClient = new WebhookRegistrationClient(client);

        using var response = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"]);
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
    }

    [Test]
    public async Task RegisterWebhook_WithLocalhost_Should_BeRejected()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);

        using var response = await registrationClient.RegisterAsync("http://localhost/test", ["batch.completed"]);
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
        var registrationClient = new WebhookRegistrationClient(client);
        var webhooksClient = new WebhooksClient(client);

        string webhookId;
        using (var registerResponse = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"]))
        {
            IgnoreIfEndpointUnavailable(registerResponse, "/api/webhooks/register");
            registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            var body = await registerResponse.Content.ReadAsStringAsync();
            webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(body);
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
}

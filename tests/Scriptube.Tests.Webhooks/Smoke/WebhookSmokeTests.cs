using System.Net;
using System.Text.Json;
using FluentAssertions;
using Scriptube.Api.Clients;
using Scriptube.Webhooks.Clients;

namespace Scriptube.Tests.Webhooks.Smoke;

[TestFixture]
[Category("Webhook")]
[Category("Smoke")]
public sealed class WebhookSmokeTests : WebhookTestBase
{
    [Test]
    public async Task AvailableEventsEndpoint_Should_ReturnCoreEvents()
    {
        RequireLiveApi();

        using var client = CreateAuthenticatedClient();
        var webhooksClient = new WebhooksClient(client);

        using var response = await webhooksClient.ListAvailableEventsAsync();
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/events/available");
        response.IsSuccessStatusCode.Should().BeTrue();

        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);

        var events = json.RootElement.GetProperty("events")
            .EnumerateArray()
            .Where(static x => x.ValueKind == JsonValueKind.String)
            .Select(static x => x.GetString())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToArray();

        events.Should().Contain("batch.completed");
        events.Should().Contain("transcript.ready");
    }

    [Test]
    public async Task RegisterWebhook_Localhost_Should_BeBlocked()
    {
        RequireLiveApi();
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);

        using var response = await registrationClient.RegisterAsync("http://localhost/test", ["batch.completed"], webhookSecret);
        IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Forbidden);
    }
}

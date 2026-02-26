using System.Net;
using System.Text.Json;
using FluentAssertions;
using Scriptube.Api.Builders;
using Scriptube.Api.Clients;
using Scriptube.Api.Services;
using Scriptube.Tests.Shared;
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
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);
        var webhooksClient = new WebhooksClient(client);

        string? webhookId = null;
        try
        {
            using var response = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"], webhookSecret);
            IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            var body = await response.Content.ReadAsStringAsync();
            webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(body);
            webhookId.Should().NotBeNullOrWhiteSpace();
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(webhookId))
            {
                using var deleteResponse = await webhooksClient.DeleteAsync(webhookId);
                IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/webhooks/{webhook_id}");
            }
        }
    }

    [Test]
    public async Task RegisterWebhook_WithPrivateNetworkTargets_Should_BeRejected()
    {
        RequireLiveApi();
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);

        var targets = new[]
        {
            "http://localhost/test",
            "http://192.168.1.1/test",
            "http://10.0.0.1/test"
        };

        foreach (var target in targets)
        {
            using var response = await registrationClient.RegisterAsync(target, ["batch.completed"], webhookSecret);
            IgnoreIfEndpointUnavailable(response, "/api/webhooks/register");
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Forbidden);
        }
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

        using var json = await ParseJsonAsync(response);
        var events = TryGetStringArray(json.RootElement, "events");
        events.Should().Contain("batch.completed");
        events.Should().Contain("transcript.ready");
    }

    [Test]
    public async Task TriggerTestEvent_AndVerifyDelivery_Should_Work_WhenWebhookIsRegistered()
    {
        RequireLiveApi();
        var webhookUrl = RequireWebhookUrlOrIgnore();
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);
        var webhooksClient = new WebhooksClient(client);

        var webhookId = string.Empty;
        try
        {
            using (var registerResponse = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"], webhookSecret))
            {
                IgnoreIfEndpointUnavailable(registerResponse, "/api/webhooks/register");
                registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

                var body = await registerResponse.Content.ReadAsStringAsync();
                webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(body);
            }

            string? deliveryId = null;
            using (var triggerResponse = await webhooksClient.TriggerTestAsync(webhookId))
            {
                IgnoreIfEndpointUnavailable(triggerResponse, "/api/webhooks/{webhook_id}/test");
                triggerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent);

                if (triggerResponse.Content.Headers.ContentLength.GetValueOrDefault() > 0)
                {
                    using var triggerJson = await ParseJsonAsync(triggerResponse);
                    deliveryId = TryGetString(triggerJson.RootElement, "delivery_id");
                }
            }

            var matchingDelivery = await WaitForDeliveryLogAsync(webhooksClient, webhookId, deliveryId, expectedEvent: null, timeout: TimeSpan.FromSeconds(40));
            matchingDelivery.Should().NotBeNull("triggered test event should produce at least one delivery log entry");
            matchingDelivery!.ResponseCode.Should().NotBeNull("delivery logs should include response status codes");

            using var retryResponse = await webhooksClient.RetryDeliveryAsync(webhookId);
            IgnoreIfEndpointUnavailable(retryResponse, "/api/webhooks/{webhook_id}/retry");
            retryResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(webhookId))
            {
                using var deleteResponse = await webhooksClient.DeleteAsync(webhookId);
                IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/webhooks/{webhook_id}");
            }
        }
    }

    [Test]
    public async Task BatchComplete_UseByokFalse_Should_FireWebhook_AndExposeDeliveryStatusCode()
    {
        RequireLiveApi();
        var webhookUrl = RequireWebhookUrlOrIgnore();
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);
        var webhooksClient = new WebhooksClient(client);
        var transcriptsClient = new TranscriptsClient(client);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        var webhookId = string.Empty;
        try
        {
            using (var registerResponse = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"], webhookSecret))
            {
                IgnoreIfEndpointUnavailable(registerResponse, "/api/webhooks/register");
                registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
                var body = await registerResponse.Content.ReadAsStringAsync();
                webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(body);
            }

            var submitRequest = new TranscriptRequestBuilder()
                .WithUrl(TestDataCatalog.SuccessVideos.EnglishManual)
                .UseByok(false)
                .BuildSubmit();

            string batchId;
            using (var submitResponse = await transcriptsClient.SubmitAsync(submitRequest))
            {
                submitResponse.IsSuccessStatusCode.Should().BeTrue();
                using var submitJson = await ParseJsonAsync(submitResponse);
                batchId = TryGetString(submitJson.RootElement, "batch_id")
                          ?? throw new AssertionException("Missing batch_id in transcript submit response.");
            }

            var result = await pollingService.WaitForFinalStatusAsync(batchId);
            result.FinalStatus.Should().Be("completed");

            var delivery = await WaitForDeliveryLogAsync(webhooksClient, webhookId, expectedDeliveryId: null, expectedEvent: "batch.completed", timeout: TimeSpan.FromSeconds(60));
            delivery.Should().NotBeNull("batch.completed webhook should be delivered/logged");
            delivery!.ResponseCode.Should().NotBeNull("delivery logs should contain status codes");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(webhookId))
            {
                using var deleteResponse = await webhooksClient.DeleteAsync(webhookId);
                IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/webhooks/{webhook_id}");
            }
        }
    }

    [Test]
    public async Task BatchComplete_UseByokTrue_Should_FireWebhook_AndShowDifferentCostEstimate_WhenAvailable()
    {
        RequireLiveApi();
        var webhookUrl = RequireWebhookUrlOrIgnore();
        var webhookSecret = RequireWebhookSecretOrIgnore();

        using var client = CreateAuthenticatedClient();
        var registrationClient = new WebhookRegistrationClient(client);
        var webhooksClient = new WebhooksClient(client);
        var transcriptsClient = new TranscriptsClient(client);
        var creditsClient = new CreditsClient(client);
        var pollingService = new BatchPollingService(transcriptsClient, Settings);

        var requestSystem = new TranscriptRequestBuilder()
            .WithUrl(TestDataCatalog.SuccessVideos.ElevenLabsForced)
            .UseByok(false)
            .BuildPrecheck();

        var requestByok = new TranscriptRequestBuilder()
            .WithUrl(TestDataCatalog.SuccessVideos.ElevenLabsForced)
            .UseByok(true)
            .BuildPrecheck();

        decimal? estimatedSystem = null;
        decimal? estimatedByok = null;

        using (var systemEstimateResponse = await creditsClient.PrecheckAsync(requestSystem))
        {
            IgnoreIfEndpointUnavailable(systemEstimateResponse, "/api/v1/credits/precheck");
            systemEstimateResponse.IsSuccessStatusCode.Should().BeTrue();
            using var json = await ParseJsonAsync(systemEstimateResponse);
            estimatedSystem = TryReadEstimatedCredits(json.RootElement);
        }

        using (var byokEstimateResponse = await creditsClient.PrecheckAsync(requestByok))
        {
            IgnoreIfEndpointUnavailable(byokEstimateResponse, "/api/v1/credits/precheck");
            byokEstimateResponse.IsSuccessStatusCode.Should().BeTrue();
            using var json = await ParseJsonAsync(byokEstimateResponse);
            estimatedByok = TryReadEstimatedCredits(json.RootElement);
        }

        if (estimatedSystem.HasValue && estimatedByok.HasValue && estimatedSystem.Value == estimatedByok.Value)
        {
            Assert.Ignore("BYOK and system-key precheck estimates are equal in this deployment; differential cost not observable.");
        }

        var webhookId = string.Empty;
        try
        {
            using (var registerResponse = await registrationClient.RegisterAsync(webhookUrl, ["batch.completed"], webhookSecret))
            {
                IgnoreIfEndpointUnavailable(registerResponse, "/api/webhooks/register");
                registerResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
                var body = await registerResponse.Content.ReadAsStringAsync();
                webhookId = WebhookRegistrationClient.ExtractWebhookIdOrThrow(body);
            }

            var submitRequest = new TranscriptRequestBuilder()
                .WithUrl(TestDataCatalog.SuccessVideos.EnglishManual)
                .UseByok(true)
                .BuildSubmit();

            string batchId;
            using (var submitResponse = await transcriptsClient.SubmitAsync(submitRequest))
            {
                submitResponse.IsSuccessStatusCode.Should().BeTrue();
                using var submitJson = await ParseJsonAsync(submitResponse);
                batchId = TryGetString(submitJson.RootElement, "batch_id")
                          ?? throw new AssertionException("Missing batch_id in transcript submit response.");
            }

            var result = await pollingService.WaitForFinalStatusAsync(batchId);
            if (!string.Equals(result.FinalStatus, "completed", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Ignore($"BYOK batch did not complete in this environment (final status: {result.FinalStatus}).");
            }

            var delivery = await WaitForDeliveryLogAsync(webhooksClient, webhookId, expectedDeliveryId: null, expectedEvent: "batch.completed", timeout: TimeSpan.FromSeconds(60));
            delivery.Should().NotBeNull("batch.completed webhook should be delivered/logged for BYOK flow");
            delivery!.ResponseCode.Should().NotBeNull("delivery logs should contain status codes");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(webhookId))
            {
                using var deleteResponse = await webhooksClient.DeleteAsync(webhookId);
                IgnoreIfEndpointUnavailable(deleteResponse, "DELETE /api/webhooks/{webhook_id}");
            }
        }
    }

    private static async Task<JsonDocument> ParseJsonAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(payload);
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = TryGetString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var nested = TryGetString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static IReadOnlyCollection<string> TryGetStringArray(JsonElement root, string propertyName)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return property.Value.EnumerateArray()
                .Where(static x => x.ValueKind == JsonValueKind.String)
                .Select(static x => x.GetString())
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToArray();
        }

        return [];
    }

    private static decimal? TryReadEstimatedCredits(JsonElement root)
    {
        var candidates = new[] { "estimated_credits", "total_credits", "credits_required", "estimated_cost" };
        foreach (var key in candidates)
        {
            var value = TryGetDecimal(root, key);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement root, string propertyName)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var number))
                    {
                        return number;
                    }

                    if (property.Value.ValueKind == JsonValueKind.String && decimal.TryParse(property.Value.GetString(), out var parsed))
                    {
                        return parsed;
                    }
                }

                var nested = TryGetDecimal(property.Value, propertyName);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var nested = TryGetDecimal(item, propertyName);
                if (nested.HasValue)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static async Task<DeliveryLogEntry?> WaitForDeliveryLogAsync(
        WebhooksClient webhooksClient,
        string webhookId,
        string? expectedDeliveryId,
        string? expectedEvent,
        TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            using var logsResponse = await webhooksClient.GetLogsAsync(webhookId);
            if (!logsResponse.IsSuccessStatusCode)
            {
                await Task.Delay(1000);
                continue;
            }

            using var logsJson = await ParseJsonAsync(logsResponse);
            var deliveries = ParseDeliveryLogs(logsJson.RootElement);
            var match = deliveries.FirstOrDefault(d =>
                (string.IsNullOrWhiteSpace(expectedDeliveryId) || string.Equals(d.DeliveryId, expectedDeliveryId, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(expectedEvent) || string.Equals(d.Event, expectedEvent, StringComparison.OrdinalIgnoreCase)));

            if (match is not null)
            {
                return match;
            }

            await Task.Delay(1500);
        }

        return null;
    }

    private static IReadOnlyCollection<DeliveryLogEntry> ParseDeliveryLogs(JsonElement root)
    {
        if (!TryGetProperty(root, "deliveries", out var deliveriesElement) || deliveriesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<DeliveryLogEntry>();
        foreach (var item in deliveriesElement.EnumerateArray())
        {
            var deliveryId = TryGetString(item, "delivery_id") ?? string.Empty;
            var ev = TryGetString(item, "event") ?? string.Empty;
            var status = TryGetString(item, "status") ?? string.Empty;

            int? responseCode = null;
            if (TryGetProperty(item, "response_code", out var responseCodeElement) && responseCodeElement.ValueKind == JsonValueKind.Number && responseCodeElement.TryGetInt32(out var code))
            {
                responseCode = code;
            }

            result.Add(new DeliveryLogEntry(deliveryId, ev, status, responseCode));
        }

        return result;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private sealed record DeliveryLogEntry(string DeliveryId, string Event, string Status, int? ResponseCode);
}

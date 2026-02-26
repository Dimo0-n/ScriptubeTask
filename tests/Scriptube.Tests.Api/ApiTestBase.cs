using System.Text.Json;
using FluentAssertions;
using Scriptube.Api.Services;
using Scriptube.Core.Configuration;
using Scriptube.Core.Http;
using Scriptube.Tests.Shared;

namespace Scriptube.Tests.Api;

public abstract class ApiTestBase : ScriptubeTestBase
{
    private HttpLogCollector _logCollector = default!;

    [SetUp]
    public void ApiSetUp()
    {
        _logCollector = new HttpLogCollector();
    }

    [TearDown]
    public void ApiTearDown()
    {
        if (_logCollector.HasEntries)
        {
            var testName = TestContext.CurrentContext.Test.Name;
            var fileName = $"{testName}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.http.log.txt";
            HttpLogAttachmentHelper.AttachToCurrentTest(fileName, _logCollector.GetCombinedLog());
        }
    }

    protected void RequireLiveApi()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_LIVE_SCRIPTUBE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Set RUN_LIVE_SCRIPTUBE=true to run live Scriptube API tests.");
        }
    }

    protected string RequireApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("SCRIPTUBE_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore("Set SCRIPTUBE_API_KEY to run authenticated API tests.");
        }

        return apiKey;
    }

    protected HttpClient CreateAuthenticatedClient()
    {
        var apiKey = RequireApiKey();
        return ApiContext.CreateClient(Settings, new ApiAuthenticationOptions { ApiKey = apiKey }, _logCollector);
    }

    protected HttpClient CreateClientWithoutApiKey()
    {
        var client = ApiContext.CreateClient(Settings, new ApiAuthenticationOptions { ApiKey = null }, _logCollector);
        client.DefaultRequestHeaders.Remove("X-API-Key");
        return client;
    }

    protected HttpClient CreateClientWithInvalidApiKey()
    {
        return ApiContext.CreateClient(Settings, new ApiAuthenticationOptions { ApiKey = "invalid_api_key_value" }, _logCollector);
    }

    protected static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonDocument.Parse(payload);
    }

    protected static string TryGetBatchId(JsonDocument document)
    {
        var batchId = FindStringProperty(document.RootElement, "batch_id")
                      ?? FindStringProperty(document.RootElement, "id");

        batchId.Should().NotBeNullOrWhiteSpace("submit response should include a batch id");
        return batchId!;
    }

    protected static decimal TryGetBalance(JsonDocument document)
    {
        if (TryGetDecimalProperty(document.RootElement, "credits_balance", out var creditsBalance))
        {
            return creditsBalance;
        }

        if (TryGetDecimalProperty(document.RootElement, "balance", out var balance))
        {
            return balance;
        }

        if (document.RootElement.TryGetProperty("credits", out var creditsElement) &&
            TryGetDecimalProperty(creditsElement, "balance", out var nestedBalance))
        {
            return nestedBalance;
        }

        throw new AssertionException("Balance field was not found in credits response payload.");
    }

    protected static string TryGetStatus(JsonDocument document)
    {
        var status = FindStringProperty(document.RootElement, "status");
        status.Should().NotBeNullOrWhiteSpace("payload should contain a status field");
        return status!;
    }

    protected static int TryCountItemsWithStatus(JsonDocument document, string expectedStatus)
    {
        if (!TryFindProperty(document.RootElement, "items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var count = 0;
        foreach (var item in itemsElement.EnumerateArray())
        {
            if (TryFindProperty(item, "status", out var statusElement) &&
                statusElement.ValueKind == JsonValueKind.String &&
                string.Equals(statusElement.GetString(), expectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    protected static string RequireForeignBatchIdOrIgnore()
    {
        var foreignBatchId = Environment.GetEnvironmentVariable("SCRIPTUBE_FOREIGN_BATCH_ID");
        if (string.IsNullOrWhiteSpace(foreignBatchId))
        {
            Assert.Ignore("Set SCRIPTUBE_FOREIGN_BATCH_ID to validate 'other user batch => 404' negative scenario.");
        }

        return foreignBatchId;
    }

    protected static void IgnoreIfEndpointUnavailable(HttpResponseMessage response, string endpoint)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.NotFound or System.Net.HttpStatusCode.MethodNotAllowed)
        {
            Assert.Ignore($"Endpoint '{endpoint}' is not available in this Scriptube deployment version.");
        }
    }

    protected static void AttachPollingArtifacts(BatchPollingResult result)
    {
        var timeline = string.Join(Environment.NewLine, result.StatusTimeline.Select((status, index) => $"{index + 1}. {status}"));
        AllureAttachmentHelper.AttachText($"polling-timeline-{result.BatchId}", timeline);
        AllureAttachmentHelper.AttachText($"polling-final-payload-{result.BatchId}", result.FinalPayloadJson);
    }

    private static string? FindStringProperty(JsonElement root, string propertyName)
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

                var nested = FindStringProperty(property.Value, propertyName);
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
                var nested = FindStringProperty(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static bool TryFindProperty(JsonElement root, string propertyName, out JsonElement value)
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

                if (TryFindProperty(property.Value, propertyName, out value))
                {
                    return true;
                }
            }
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (TryFindProperty(item, propertyName, out value))
                {
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetDecimalProperty(JsonElement element, string propertyName, out decimal value)
    {
        value = default;
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var numeric))
        {
            value = numeric;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

}
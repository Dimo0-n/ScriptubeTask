using System.Text.Json;
using Scriptube.Webhooks.Assertions;
using Scriptube.Webhooks.Models;
using Scriptube.Webhooks.Signature;

namespace Scriptube.Webhooks.Clients;

public static class WebhookDeliveryVerifier
{
    public static WebhookPayload ParseAndValidate(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var batchId = TryGetString(root, "batch_id") ?? TryGetString(root, "batchId") ?? string.Empty;
        var status = TryGetString(root, "status") ?? string.Empty;
        var items = ParseItems(root);

        var payload = new WebhookPayload
        {
            BatchId = batchId,
            Status = status,
            Items = items,
            RawJson = payloadJson
        };

        WebhookPayloadAssertions.EnsureBasicSchema(payload);
        return payload;
    }

    public static bool VerifyHmacSignature(string payloadJson, string secret, string? signatureHeader)
    {
        return HmacSignatureVerifier.VerifySignature(payloadJson, secret, signatureHeader);
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static IReadOnlyCollection<WebhookPayloadItem> ParseItems(JsonElement root)
    {
        if (!TryGetProperty(root, "items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<WebhookPayloadItem>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            items.Add(new WebhookPayloadItem
            {
                Url = TryGetString(item, "url"),
                Status = TryGetString(item, "status")
            });
        }

        return items;
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
}
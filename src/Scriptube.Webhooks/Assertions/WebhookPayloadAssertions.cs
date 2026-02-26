using Scriptube.Webhooks.Models;

namespace Scriptube.Webhooks.Assertions;

public static class WebhookPayloadAssertions
{
    public static void EnsureBasicSchema(WebhookPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.BatchId))
        {
            throw new InvalidOperationException("Webhook payload is missing batch_id.");
        }

        if (string.IsNullOrWhiteSpace(payload.Status))
        {
            throw new InvalidOperationException("Webhook payload is missing status.");
        }

        if (payload.Items is null)
        {
            throw new InvalidOperationException("Webhook payload items collection is null.");
        }
    }
}
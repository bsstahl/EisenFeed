using EisenFeed.Core.Models;
using System.Text.Json;

namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed class FeedIdItemIdMessageMapper
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private const string SchemaVersion = "1.0";
    private const string EventType = "feed-item-ingested";

    public Task<IReadOnlyCollection<FeedKafkaMessage>> MapMessagesAsync(
        IEnumerable<FeedItem> items,
        Guid runId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<FeedKafkaMessage> messages = items
            .Select(item => new FeedKafkaMessage(
                $"{item.FeedId}:{item.ItemId}",
                JsonSerializer.Serialize(new
                {
                    schemaVersion = SchemaVersion,
                    eventType = EventType,
                    runId,
                    occurredAt,
                    feedId = item.FeedId,
                    itemId = item.ItemId,
                    publishedAt = item.PublishedAt,
                    title = item.Title,
                    content = item.Content
                }, _jsonSerializerOptions)))
            .ToArray();

        return Task.FromResult(messages);
    }
}
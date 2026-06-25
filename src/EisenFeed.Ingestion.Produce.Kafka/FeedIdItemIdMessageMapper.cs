using EisenFeed.Core.Models;
using System.Text.Json;

namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed class FeedIdItemIdMessageMapper
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyCollection<FeedKafkaMessage>> MapMessagesAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<FeedKafkaMessage> messages = items
            .Select(item => new FeedKafkaMessage(
                $"{item.FeedId}:{item.ItemId}",
                JsonSerializer.Serialize(new
                {
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
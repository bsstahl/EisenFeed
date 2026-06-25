using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed class FeedIdItemIdMessageMapper
{
    public Task<IReadOnlyCollection<FeedKafkaMessage>> MapMessagesAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
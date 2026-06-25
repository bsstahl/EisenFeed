using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed class FeedRepository : IWriteFeedItems
{
    public Task<ProduceDeliveryResult> PublishAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
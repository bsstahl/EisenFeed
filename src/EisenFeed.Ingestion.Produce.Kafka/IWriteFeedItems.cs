using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Produce.Kafka;

public interface IWriteFeedItems
{
    Task<ProduceDeliveryResult> PublishAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default);
}
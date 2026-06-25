using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface IWriteFeedItems
{
    Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default);
}
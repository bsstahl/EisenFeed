using EisenFeed.Core.Models;

namespace EisenFeed.Core.DataPersistence;

public interface IFeedItemStore
{
    Task SaveAsync(FeedItem item, FeedScore score, CancellationToken cancellationToken = default);
}

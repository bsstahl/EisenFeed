using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface ICollectFeedItems
{
    Task<IReadOnlyCollection<FeedItem>> IngestAsync(CancellationToken cancellationToken = default);
}
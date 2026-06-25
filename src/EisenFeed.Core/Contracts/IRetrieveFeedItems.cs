using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface IRetrieveFeedItems
{
    Task<IReadOnlyCollection<FeedItem>> RetrieveAsync(CancellationToken cancellationToken = default);
}
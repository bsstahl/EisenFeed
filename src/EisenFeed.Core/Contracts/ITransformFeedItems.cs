using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface ITransformFeedItems
{
    Task<IReadOnlyCollection<FeedItem>> TransformAsync(IReadOnlyCollection<FeedItem> items, CancellationToken cancellationToken = default);
}
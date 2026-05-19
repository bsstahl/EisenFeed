using EisenFeed.Core.Models;

namespace EisenFeed.Core.ContentAging;

public interface IContentAgingService
{
    FeedScore ApplyAging(FeedItem item, FeedScore currentScore, DateTimeOffset asOf);
}

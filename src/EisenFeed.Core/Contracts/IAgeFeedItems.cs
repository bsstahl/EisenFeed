using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface IAgeFeedItems
{
    FeedScore ApplyAging(FeedItem item, FeedScore currentScore, DateTimeOffset asOf);
}
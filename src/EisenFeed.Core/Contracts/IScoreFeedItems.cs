using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface IScoreFeedItems
{
    FeedScore Score(FeedItem item, DateTimeOffset asOf);
}
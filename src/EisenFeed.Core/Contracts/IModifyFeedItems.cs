using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

public interface IModifyFeedItems
{
    FeedScore Apply(FeedItem item, FeedScore currentScore);
}
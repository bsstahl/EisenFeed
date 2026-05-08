using EisenFeed.Core.Models;

namespace EisenFeed.Core.FeedModifiers;

public interface IFeedModifier
{
    FeedScore Apply(FeedItem item, FeedScore currentScore);
}

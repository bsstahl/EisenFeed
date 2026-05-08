using EisenFeed.Core.Models;

namespace EisenFeed.Core.Scoring;

public interface IScoringService
{
    FeedScore Score(FeedItem item, DateTimeOffset asOf);
}

using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Transform.Parser;

public interface IFeedParserStrategy
{
    Task<IReadOnlyCollection<FeedItem>> ParseAsync(string feedId, string payload, CancellationToken cancellationToken = default);
}

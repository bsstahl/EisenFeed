using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Transform.Parser;

public sealed class RssXmlParserStrategy : IFeedParserStrategy
{
    public Task<IReadOnlyCollection<FeedItem>> ParseAsync(string feedId, string payload, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

namespace EisenFeed.Ingestion.Transform.Parser;

public sealed class FeedParserStrategySelector
{
    private readonly IFeedParserStrategy _rssXmlParserStrategy = new RssXmlParserStrategy();

    public IFeedParserStrategy Select(string contentType)
    {
        if (string.Equals(contentType, "application/rss+xml", StringComparison.OrdinalIgnoreCase))
        {
            return _rssXmlParserStrategy;
        }

        throw new NotSupportedException($"Unsupported content type '{contentType}'.");
    }
}

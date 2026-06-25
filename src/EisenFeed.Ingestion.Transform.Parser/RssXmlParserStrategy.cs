using EisenFeed.Core.Models;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace EisenFeed.Ingestion.Transform.Parser;

public sealed class RssXmlParserStrategy : IFeedParserStrategy
{
    public Task<IReadOnlyCollection<FeedItem>> ParseAsync(string feedId, string payload, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(feedId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        try
        {
            XDocument document = XDocument.Parse(payload, LoadOptions.PreserveWhitespace);

            IReadOnlyCollection<FeedItem> items = document
                .Descendants("item")
                .Select(item => CreateFeedItem(feedId, item))
                .ToArray();

            return Task.FromResult(items);
        }
        catch (XmlException ex)
        {
            throw new FormatException("The RSS payload is malformed.", ex);
        }
    }

    private static FeedItem CreateFeedItem(string feedId, XElement item)
    {
        string itemId =
            item.Element("guid")?.Value.Trim()
            ?? item.Element("link")?.Value.Trim()
            ?? $"{item.Element("title")?.Value.Trim()}:{item.Element("pubDate")?.Value.Trim()}";

        string title = item.Element("title")?.Value.Trim() ?? string.Empty;
        string content = item.Element("description")?.Value.Trim() ?? string.Empty;
        string publishedValue = item.Element("pubDate")?.Value.Trim() ?? string.Empty;

        DateTimeOffset publishedAt = DateTimeOffset.Parse(
            publishedValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces);

        return new FeedItem(feedId, itemId, publishedAt, title, content);
    }
}

using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace EisenFeed.Ingestion.Consume.Rss;

public sealed class FeedRepository : IRetrieveFeedItems
{
    private readonly HttpClient _httpClient;
    private readonly Uri _feedUrl;
    private readonly DateTimeOffset? _lastProcessedAt;

    public FeedRepository(HttpClient httpClient, Uri feedUrl, DateTimeOffset? lastProcessedAt = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _feedUrl = feedUrl ?? throw new ArgumentNullException(nameof(feedUrl));
        _lastProcessedAt = lastProcessedAt;
    }

    public async Task<IReadOnlyCollection<FeedItem>> RetrieveAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, _feedUrl);
        if (_lastProcessedAt.HasValue)
        {
            request.Headers.IfModifiedSince = _lastProcessedAt;
        }

        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            XDocument document = XDocument.Parse(payload, LoadOptions.PreserveWhitespace);

            IReadOnlyCollection<FeedItem> items = document
                .Descendants("item")
                .Select(item => CreateFeedItem(FeedId.From(_feedUrl.Host), item))
                .ToArray();

            return items;
        }
        catch (XmlException ex)
        {
            throw new FormatException("The feed payload is malformed.", ex);
        }
    }

    private static FeedItem CreateFeedItem(FeedId feedId, XElement item)
    {
        FeedItemId itemId = FeedItemId.From(
            item.Element("guid")?.Value.Trim()
            ?? item.Element("link")?.Value.Trim()
            ?? $"{item.Element("title")?.Value.Trim()}:{item.Element("pubDate")?.Value.Trim()}");

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
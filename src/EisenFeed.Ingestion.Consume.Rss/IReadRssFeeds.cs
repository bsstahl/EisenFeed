namespace EisenFeed.Ingestion.Consume.Rss;

public interface IReadRssFeeds
{
    Task<string> FetchAsync(Uri feedUrl, CancellationToken cancellationToken = default);
}
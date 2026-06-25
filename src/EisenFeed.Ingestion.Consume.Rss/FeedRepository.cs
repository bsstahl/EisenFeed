namespace EisenFeed.Ingestion.Consume.Rss;

public sealed class FeedRepository : IReadRssFeeds
{
    public Task<string> FetchAsync(Uri feedUrl, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
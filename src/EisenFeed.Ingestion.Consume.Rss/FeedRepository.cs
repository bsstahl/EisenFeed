namespace EisenFeed.Ingestion.Consume.Rss;

public sealed class FeedRepository : IReadRssFeeds
{
    private readonly HttpClient _httpClient;

    public FeedRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<string> FetchAsync(Uri feedUrl, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
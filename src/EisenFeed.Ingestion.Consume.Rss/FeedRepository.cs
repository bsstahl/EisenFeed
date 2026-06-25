namespace EisenFeed.Ingestion.Consume.Rss;

public sealed class FeedRepository : IReadRssFeeds
{
    private readonly HttpClient _httpClient;

    public FeedRepository(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> FetchAsync(Uri feedUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(feedUrl);

        using var request = new HttpRequestMessage(HttpMethod.Get, feedUrl);
        using HttpResponseMessage response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
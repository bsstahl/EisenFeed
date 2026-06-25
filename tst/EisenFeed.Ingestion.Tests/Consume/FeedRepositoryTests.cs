using EisenFeed.Ingestion.Consume.Rss;

namespace EisenFeed.Ingestion.Tests.Consume;

public sealed class FeedRepositoryTests
{
    [Fact]
    public async Task FetchAsync_WhenFeedIsReachable_ReturnsRawRssPayload()
    {
        var target = CreateTestTarget();

        string payload = await target.FetchAsync(new Uri("https://example.com/feed.xml"), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(payload));
        Assert.Contains("<rss", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadRssFeeds CreateTestTarget()
    {
        return new FeedRepository();
    }
}
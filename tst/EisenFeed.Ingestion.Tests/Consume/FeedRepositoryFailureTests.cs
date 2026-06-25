using EisenFeed.Ingestion.Consume.Rss;

namespace EisenFeed.Ingestion.Tests.Consume;

public sealed class FeedRepositoryFailureTests
{
    [Fact]
    public async Task FetchAsync_WhenFeedIsUnavailable_ThrowsHttpRequestException()
    {
        var repository = CreateSut();

        await Assert.ThrowsAsync<HttpRequestException>(
            () => repository.FetchAsync(new Uri("https://127.0.0.1:1/unreachable.xml"), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var repository = CreateSut();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.FetchAsync(new Uri("https://example.com/feed.xml"), cts.Token));
    }

    private static IReadRssFeeds CreateSut()
    {
        return new FeedRepository();
    }
}
using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Consume.Rss;
using System.Net;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Consume.Rss.Tests;

[Trait("TestType", "Unit")]
[Trait("Phase", "Consume")]
[Trait("Component", "FeedRepository")]
public sealed class FeedRepository_RetrieveAsync_Should
{
    private readonly ITestOutputHelper _output;

    public FeedRepository_RetrieveAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReturnCanonicalFeedItemsWhenTheFeedIsReachable()
    {
        try
        {
            const string expectedPayload = "<rss><channel><item><guid>item-1</guid><title>sample</title><pubDate>Wed, 24 Jun 2026 00:00:00 GMT</pubDate><description>desc</description></item></channel></rss>";
            var handler = new DelegateHttpMessageHandler((request, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent(expectedPayload)
                };
                return Task.FromResult(response);
            });

            var target = CreateTestTarget(handler);
            var feedUrl = new Uri("https://example.com/feed.xml");

            _output.WriteLine("Input feedUrl: {0}", feedUrl);
            _output.WriteLine("Input expectedPayload: {0}", expectedPayload);

            IReadOnlyCollection<FeedItem> items = await target.RetrieveAsync(CancellationToken.None);

            _output.WriteLine("Output item count: {0}", items.Count);

            FeedItem item = Assert.Single(items);
            Assert.Equal(FeedId.From("example.com"), item.FeedId);
            Assert.Equal(FeedItemId.From("item-1"), item.ItemId);
            Assert.Equal("sample", item.Title);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnCanonicalFeedItemsWhenTheFeedIsReachable");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ReturnAllCanonicalFeedItemsWhenTheFeedContainsMultipleItems()
    {
        try
        {
            const string payload = "<rss><channel><item><guid>item-1</guid><title>first</title><pubDate>Wed, 24 Jun 2026 00:00:00 GMT</pubDate><description>desc-1</description></item><item><guid>item-2</guid><title>second</title><pubDate>Wed, 24 Jun 2026 01:00:00 GMT</pubDate><description>desc-2</description></item></channel></rss>";
            var handler = new DelegateHttpMessageHandler((_, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload)
                };
                return Task.FromResult(response);
            });

            var target = CreateTestTarget(handler);

            _output.WriteLine("Input payload length: {0}", payload.Length);

            IReadOnlyCollection<FeedItem> items = await target.RetrieveAsync(CancellationToken.None);

            _output.WriteLine("Output item count: {0}", items.Count);

            Assert.Equal(2, items.Count);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnAllCanonicalFeedItemsWhenTheFeedContainsMultipleItems");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ThrowFormatExceptionWhenTheFeedPayloadIsMalformed()
    {
        try
        {
            const string payload = "<rss><channel><item><guid>broken</guid><title>Broken item</title>";
            var handler = new DelegateHttpMessageHandler((_, _) =>
            {
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent(payload)
                };
                return Task.FromResult(response);
            });

            var target = CreateTestTarget(handler);

            _output.WriteLine("Input payload length: {0}", payload.Length);

            FormatException ex = await Assert.ThrowsAsync<FormatException>(
                () => target.RetrieveAsync(CancellationToken.None));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowFormatExceptionWhenTheFeedPayloadIsMalformed");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ThrowHttpRequestExceptionWhenTheFeedIsUnavailable()
    {
        try
        {
            var handler = new DelegateHttpMessageHandler((_, _) =>
                throw new HttpRequestException("Simulated network failure"));

            var target = CreateTestTarget(handler);
            var feedUrl = new Uri("https://127.0.0.1:1/unreachable.xml");

            _output.WriteLine("Input feedUrl: {0}", feedUrl);

            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => target.RetrieveAsync(CancellationToken.None));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowHttpRequestExceptionWhenTheFeedIsUnavailable");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ThrowOperationCanceledExceptionWhenCancellationIsRequested()
    {
        try
        {
            var handler = new DelegateHttpMessageHandler((_, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                HttpResponseMessage response = new(HttpStatusCode.OK)
                {
                    Content = new StringContent("<rss></rss>")
                };
                return Task.FromResult(response);
            });

            var target = CreateTestTarget(handler);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var feedUrl = new Uri("https://example.com/feed.xml");

            _output.WriteLine("Input feedUrl: {0}", feedUrl);
            _output.WriteLine("Input cancellation requested: {0}", cts.IsCancellationRequested);

            OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => target.RetrieveAsync(cts.Token));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowOperationCanceledExceptionWhenCancellationIsRequested");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static IRetrieveFeedItems CreateTestTarget(HttpMessageHandler handler)
    {
        Uri feedUrl = new("https://example.com/feed.xml");
        var httpClient = new HttpClient(handler);
        return new FeedRepository(httpClient, feedUrl);
    }

    private sealed class DelegateHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;

        public DelegateHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }
}

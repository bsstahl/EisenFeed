using EisenFeed.Ingestion.Consume.Rss;
using System.Net;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Tests.Consume;

[Trait("TestType", "Unit")]
[Trait("Phase", "Consume")]
[Trait("Component", "FeedRepository")]
public sealed class FeedRepository_FetchAsync_Should
{
    private readonly ITestOutputHelper _output;

    public FeedRepository_FetchAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReturnTheRawRssPayloadWhenTheFeedIsReachable()
    {
        try
        {
            const string expectedPayload = "<rss><channel><title>sample</title></channel></rss>";
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

            string payload = await target.FetchAsync(feedUrl, CancellationToken.None);

            _output.WriteLine("Output payload length: {0}", payload?.Length ?? 0);
            _output.WriteLine("Output payload preview: {0}", payload?[..Math.Min(payload.Length, 200)] ?? "null");

            Assert.False(string.IsNullOrWhiteSpace(payload));
            Assert.Contains("<rss", payload, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnTheRawRssPayloadWhenTheFeedIsReachable");
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
                () => target.FetchAsync(feedUrl, CancellationToken.None));

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
                () => target.FetchAsync(feedUrl, cts.Token));

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

    private static IReadRssFeeds CreateTestTarget(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new FeedRepository(httpClient);
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
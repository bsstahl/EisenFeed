using EisenFeed.Ingestion.Transform.Parser;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Tests.Transform;

[Trait("TestType", "Unit")]
[Trait("Phase", "Transform")]
[Trait("Component", "RssXmlParserStrategy")]
public sealed class RssXmlParserStrategy_ParseAsync_Should
{
    private readonly ITestOutputHelper _output;

    public RssXmlParserStrategy_ParseAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReturnCanonicalFeedItemsWhenXmlIsValid()
    {
        try
        {
            var target = CreateTestTarget();
            var xmlPath = Path.Combine("TestData", "Rss", "valid-feed.xml");
            const string feedId = "sample-feed";
            string xml = await File.ReadAllTextAsync(xmlPath);

            _output.WriteLine("Input xmlPath: {0}", xmlPath);
            _output.WriteLine("Input feedId: {0}", feedId);
            _output.WriteLine("Input xml length: {0}", xml.Length);

            IReadOnlyCollection<EisenFeed.Core.Models.FeedItem> items = await target.ParseAsync(
                feedId,
                xml,
                CancellationToken.None);

            _output.WriteLine("Output item count: {0}", items.Count);
            foreach (var item in items)
            {
                _output.WriteLine(
                    "Output item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                    item.FeedId,
                    item.ItemId,
                    item.PublishedAt,
                    item.Title,
                    item.Content?.Length ?? 0);
            }

            Assert.Equal(2, items.Count);
            Assert.All(items, item =>
            {
                Assert.Equal("sample-feed", item.FeedId);
                Assert.False(string.IsNullOrWhiteSpace(item.ItemId));
                Assert.False(string.IsNullOrWhiteSpace(item.Title));
            });
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnCanonicalFeedItemsWhenXmlIsValid");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ThrowFormatExceptionWhenXmlIsMalformed()
    {
        try
        {
            var target = CreateTestTarget();
            var xmlPath = Path.Combine("TestData", "Rss", "malformed-feed.xml");
            const string feedId = "sample-feed";
            string xml = await File.ReadAllTextAsync(xmlPath);

            _output.WriteLine("Input xmlPath: {0}", xmlPath);
            _output.WriteLine("Input feedId: {0}", feedId);
            _output.WriteLine("Input xml length: {0}", xml.Length);

            FormatException ex = await Assert.ThrowsAsync<FormatException>(() => target.ParseAsync(
                feedId,
                xml,
                CancellationToken.None));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowFormatExceptionWhenXmlIsMalformed");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static RssXmlParserStrategy CreateTestTarget()
    {
        return new RssXmlParserStrategy();
    }
}

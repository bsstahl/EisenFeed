using EisenFeed.Ingestion.Transform.Parser;

namespace EisenFeed.Ingestion.Tests.Transform;

public sealed class RssXmlParserStrategyTests
{
    [Fact]
    public async Task ParseAsync_WhenXmlIsValid_ReturnsCanonicalFeedItems()
    {
        var strategy = CreateSut();
        string xml = await File.ReadAllTextAsync(Path.Combine("TestData", "Rss", "valid-feed.xml"));

        IReadOnlyCollection<EisenFeed.Core.Models.FeedItem> items = await strategy.ParseAsync(
            "sample-feed",
            xml,
            CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.All(items, item =>
        {
            Assert.Equal("sample-feed", item.FeedId);
            Assert.False(string.IsNullOrWhiteSpace(item.ItemId));
            Assert.False(string.IsNullOrWhiteSpace(item.Title));
        });
    }

    private static RssXmlParserStrategy CreateSut()
    {
        return new RssXmlParserStrategy();
    }
}

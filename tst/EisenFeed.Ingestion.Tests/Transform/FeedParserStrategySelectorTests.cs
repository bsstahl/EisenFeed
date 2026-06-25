using EisenFeed.Ingestion.Transform.Parser;

namespace EisenFeed.Ingestion.Tests.Transform;

public sealed class FeedParserStrategySelectorTests
{
    [Fact]
    public void Select_WhenRssContentType_ReturnsRssXmlParserStrategy()
    {
        var target = CreateTestTarget();

        IFeedParserStrategy strategy = target.Select("application/rss+xml");

        Assert.IsType<RssXmlParserStrategy>(strategy);
    }

    [Fact]
    public void Select_WhenUnknownContentType_ThrowsNotSupportedException()
    {
        var target = CreateTestTarget();

        Assert.Throws<NotSupportedException>(() => target.Select("application/unknown"));
    }

    private static FeedParserStrategySelector CreateTestTarget()
    {
        return new FeedParserStrategySelector();
    }
}

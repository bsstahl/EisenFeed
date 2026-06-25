using EisenFeed.Ingestion.Transform.Parser;

namespace EisenFeed.Ingestion.Tests.Transform;

public sealed class FeedParserStrategySelectorTests
{
    [Fact]
    public void Select_WhenRssContentType_ReturnsRssXmlParserStrategy()
    {
        var selector = CreateSut();

        IFeedParserStrategy strategy = selector.Select("application/rss+xml");

        Assert.IsType<RssXmlParserStrategy>(strategy);
    }

    [Fact]
    public void Select_WhenUnknownContentType_ThrowsNotSupportedException()
    {
        var selector = CreateSut();

        Assert.Throws<NotSupportedException>(() => selector.Select("application/unknown"));
    }

    private static FeedParserStrategySelector CreateSut()
    {
        return new FeedParserStrategySelector();
    }
}

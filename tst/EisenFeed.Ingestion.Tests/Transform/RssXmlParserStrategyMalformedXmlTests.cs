using EisenFeed.Ingestion.Transform.Parser;

namespace EisenFeed.Ingestion.Tests.Transform;

public sealed class RssXmlParserStrategyMalformedXmlTests
{
    [Fact]
    public async Task ParseAsync_WhenXmlIsMalformed_ThrowsFormatException()
    {
        var strategy = CreateSut();
        string xml = await File.ReadAllTextAsync(Path.Combine("TestData", "Rss", "malformed-feed.xml"));

        await Assert.ThrowsAsync<FormatException>(() => strategy.ParseAsync(
            "sample-feed",
            xml,
            CancellationToken.None));
    }

    private static RssXmlParserStrategy CreateSut()
    {
        return new RssXmlParserStrategy();
    }
}

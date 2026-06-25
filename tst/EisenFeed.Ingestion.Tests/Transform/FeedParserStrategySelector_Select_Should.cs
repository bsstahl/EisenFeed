using EisenFeed.Ingestion.Transform.Parser;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Tests.Transform;

[Trait("TestType", "Unit")]
[Trait("Phase", "Transform")]
[Trait("Component", "FeedParserStrategySelector")]
public sealed class FeedParserStrategySelector_Select_Should
{
    private readonly ITestOutputHelper _output;

    public FeedParserStrategySelector_Select_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReturnRssXmlParserStrategyWhenContentTypeIsRss()
    {
        try
        {
            var target = CreateTestTarget();
            const string contentType = "application/rss+xml";

            _output.WriteLine("Input contentType: {0}", contentType);

            IFeedParserStrategy strategy = target.Select(contentType);

            _output.WriteLine("Output strategy type: {0}", strategy.GetType().FullName ?? "unknown");

            Assert.IsType<RssXmlParserStrategy>(strategy);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnRssXmlParserStrategyWhenContentTypeIsRss");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public void ThrowNotSupportedExceptionWhenContentTypeIsUnknown()
    {
        try
        {
            var target = CreateTestTarget();
            const string contentType = "application/unknown";

            _output.WriteLine("Input contentType: {0}", contentType);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => target.Select(contentType));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowNotSupportedExceptionWhenContentTypeIsUnknown");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static FeedParserStrategySelector CreateTestTarget()
    {
        return new FeedParserStrategySelector();
    }
}

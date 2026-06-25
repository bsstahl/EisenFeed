using EisenFeed.Core.Contracts;
using EisenFeed.Ingestion.Transform.Rules;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Transform.Rules.Tests;

[Trait("TestType", "Unit")]
[Trait("Phase", "Transform")]
[Trait("Component", "FeedTransformStrategySelector")]
public sealed class FeedTransformStrategySelector_Select_Should
{
    private readonly ITestOutputHelper _output;

    public FeedTransformStrategySelector_Select_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReturnFeedItemTransformerWhenProfileIsDefault()
    {
        try
        {
            var target = CreateTestTarget();
            const string profile = "default";

            _output.WriteLine("Input profile: {0}", profile);

            ITransformFeedItems strategy = target.Select(profile);

            _output.WriteLine("Output strategy type: {0}", strategy.GetType().FullName ?? "unknown");

            Assert.IsType<FeedItemTransformer>(strategy);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnFeedItemTransformerWhenProfileIsDefault");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public void ThrowNotSupportedExceptionWhenProfileIsUnknown()
    {
        try
        {
            var target = CreateTestTarget();
            const string profile = "unknown";

            _output.WriteLine("Input profile: {0}", profile);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => target.Select(profile));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowNotSupportedExceptionWhenProfileIsUnknown");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static FeedTransformStrategySelector CreateTestTarget()
    {
        return new FeedTransformStrategySelector();
    }
}
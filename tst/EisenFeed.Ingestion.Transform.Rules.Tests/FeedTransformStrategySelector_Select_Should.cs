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

    [Fact]
    public void ThrowArgumentExceptionWhenProfileIsNull()
    {
        try
        {
            var target = CreateTestTarget();
            string? profile = null;

            _output.WriteLine("Input profile: null");

            ArgumentException ex = Assert.Throws<ArgumentException>(() => target.Select(profile!));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
            _output.WriteLine("Captured exception param name: {0}", ex.ParamName);

            Assert.Equal("profile", ex.ParamName);
            Assert.Contains("null", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowArgumentExceptionWhenProfileIsNull");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public void ThrowArgumentExceptionWhenProfileIsEmpty()
    {
        try
        {
            var target = CreateTestTarget();
            const string profile = "";

            _output.WriteLine("Input profile: empty string");

            ArgumentException ex = Assert.Throws<ArgumentException>(() => target.Select(profile));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
            _output.WriteLine("Captured exception param name: {0}", ex.ParamName);

            Assert.Equal("profile", ex.ParamName);
            Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowArgumentExceptionWhenProfileIsEmpty");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public void ThrowArgumentExceptionWhenProfileIsWhitespace()
    {
        try
        {
            var target = CreateTestTarget();
            const string profile = "   ";

            _output.WriteLine("Input profile: whitespace");

            ArgumentException ex = Assert.Throws<ArgumentException>(() => target.Select(profile));

            _output.WriteLine("Captured exception type: {0}", ex.GetType().FullName ?? "unknown");
            _output.WriteLine("Captured exception message: {0}", ex.Message);
            _output.WriteLine("Captured exception param name: {0}", ex.ParamName);

            Assert.Equal("profile", ex.ParamName);
            Assert.Contains("whitespace", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ThrowArgumentExceptionWhenProfileIsWhitespace");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static FeedTransformStrategySelector CreateTestTarget()
    {
        return new FeedTransformStrategySelector();
    }
}
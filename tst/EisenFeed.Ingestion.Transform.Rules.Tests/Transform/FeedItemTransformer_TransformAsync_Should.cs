using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Transform.Rules;
using EisenFeed.Ingestion.Transform.Rules.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Transform.Rules.Tests.Transform;

[Trait("TestType", "Unit")]
[Trait("Phase", "Transform")]
[Trait("Component", "FeedItemTransformer")]
public sealed class FeedItemTransformer_TransformAsync_Should
{
    private readonly ITestOutputHelper _output;

    public FeedItemTransformer_TransformAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReturnInputItemsWhenNoTransformRulesApply()
    {
        try
        {
            var target = CreateTestTarget();
            FeedItem inputItem = CanonicalFeedItemFactory.Create(
                feedId: FeedId.From("feed-a"),
                itemId: FeedItemId.From("item-42"),
                title: "title",
                content: "body");
            IReadOnlyCollection<FeedItem> items = new[] { inputItem };

            _output.WriteLine("Input item count: {0}", items.Count);
            _output.WriteLine("Input item: feedId={0}, itemId={1}, title={2}", inputItem.FeedId, inputItem.ItemId, inputItem.Title);

            IReadOnlyCollection<FeedItem> result = await target.TransformAsync(items, CancellationToken.None);

            _output.WriteLine("Output item count: {0}", result.Count);

            FeedItem outputItem = Assert.Single(result);
            Assert.Equal(inputItem, outputItem);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnInputItemsWhenNoTransformRulesApply");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ReturnEmptyCollectionWhenNoItemsAreProvided()
    {
        try
        {
            var target = CreateTestTarget();
            IReadOnlyCollection<FeedItem> items = Array.Empty<FeedItem>();

            _output.WriteLine("Input item count: {0}", items.Count);

            IReadOnlyCollection<FeedItem> result = await target.TransformAsync(items, CancellationToken.None);

            _output.WriteLine("Output item count: {0}", result.Count);

            Assert.Empty(result);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnEmptyCollectionWhenNoItemsAreProvided");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ApplyRulesInOrderWhenRulesAreConfigured()
    {
        try
        {
            IReadOnlyCollection<ITransformFeedItemRule> rules =
            [
                new AppendTitleSuffixRule("-normalized"),
                new AppendTitleSuffixRule("-enriched")
            ];
            var target = CreateTestTarget(rules);
            FeedItem inputItem = CanonicalFeedItemFactory.Create(title: "title");

            _output.WriteLine("Input title: {0}", inputItem.Title);
            _output.WriteLine("Input rule count: {0}", rules.Count);

            IReadOnlyCollection<FeedItem> result = await target.TransformAsync([inputItem], CancellationToken.None);

            FeedItem outputItem = Assert.Single(result);
            _output.WriteLine("Output title: {0}", outputItem.Title);

            Assert.Equal("title-normalized-enriched", outputItem.Title);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ApplyRulesInOrderWhenRulesAreConfigured");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static ITransformFeedItems CreateTestTarget()
    {
        return new FeedItemTransformer();
    }

    private static ITransformFeedItems CreateTestTarget(IEnumerable<ITransformFeedItemRule> rules)
    {
        return new FeedItemTransformer(rules);
    }

    private sealed class AppendTitleSuffixRule : ITransformFeedItemRule
    {
        private readonly string _suffix;

        public AppendTitleSuffixRule(string suffix)
        {
            _suffix = suffix;
        }

        public Task<FeedItem> ApplyAsync(FeedItem item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(item);
            return Task.FromResult(item with { Title = $"{item.Title}{_suffix}" });
        }
    }
}
using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Transform.Rules;

public sealed class FeedItemTransformer : ITransformFeedItems
{
    private readonly IReadOnlyCollection<ITransformFeedItemRule> _rules;

    public FeedItemTransformer(IEnumerable<ITransformFeedItemRule>? rules = null)
    {
        _rules = rules?.ToArray() ?? Array.Empty<ITransformFeedItemRule>();
    }

    public async Task<IReadOnlyCollection<FeedItem>> TransformAsync(IReadOnlyCollection<FeedItem> items, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        List<FeedItem> transformedItems = new(items.Count);

        foreach (FeedItem item in items)
        {
            FeedItem transformedItem = item;

            foreach (ITransformFeedItemRule rule in _rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                transformedItem = await rule.ApplyAsync(transformedItem, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("A transform rule returned a null feed item.");
            }

            transformedItems.Add(transformedItem);
        }

        return transformedItems;
    }
}
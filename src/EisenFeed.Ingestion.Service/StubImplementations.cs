using System.Diagnostics.CodeAnalysis;
using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;

#pragma warning disable CA1515 // "types can be made internal" - these are accessed by integration tests

namespace EisenFeed.Ingestion.Service;

/// <summary>Stub that returns a few sample items for testing.</summary>
public sealed class StubRetrieveFeedItems : IRetrieveFeedItems
{
    /// <summary>Test override: if set, returns these items instead of the default 2.</summary>
    public static IReadOnlyCollection<FeedItem>? TestOverrideItems { get; set; }

    public Task<IReadOnlyCollection<FeedItem>> RetrieveAsync(CancellationToken cancellationToken = default)
    {
        if (TestOverrideItems != null)
            return Task.FromResult(TestOverrideItems);

        var items = new[]
        {
            new FeedItem(
                FeedId.From("test-feed"),
                FeedItemId.From("item-1"),
                DateTimeOffset.UtcNow,
                "Test Item 1",
                "Content 1"),
            new FeedItem(
                FeedId.From("test-feed"),
                FeedItemId.From("item-2"),
                DateTimeOffset.UtcNow,
                "Test Item 2",
                "Content 2"),
        };
        return Task.FromResult((IReadOnlyCollection<FeedItem>)items);
    }
}

/// <summary>Stub that passes items through unchanged.</summary>
public sealed class PassThroughTransformFeedItems : ITransformFeedItems
{
    public Task<IReadOnlyCollection<FeedItem>> TransformAsync(IReadOnlyCollection<FeedItem> items, CancellationToken cancellationToken = default)
        => Task.FromResult(items);
}

/// <summary>Stub that pretends to publish all items successfully.</summary>
public sealed class StubWriteFeedItems : IWriteFeedItems
{
    /// <summary>Test override: if set, these item IDs will fail to publish (return in FailedCount).</summary>
    public static IReadOnlyCollection<FeedItemId>? TestFailItemIds { get; set; }

    public Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, Guid runId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        var list = items.ToList();
        
        if (TestFailItemIds != null && TestFailItemIds.Count > 0)
        {
            int successCount = list.Count(i => !TestFailItemIds.Contains(i.ItemId));
            int failCount = list.Count(i => TestFailItemIds.Contains(i.ItemId));
            return Task.FromResult(new DeliveryResult(list.Count, successCount, failCount));
        }

        return Task.FromResult(new DeliveryResult(list.Count, list.Count, 0));
    }
}

/// <summary>Stub that never marks anything as tracked (always returns false for in-memory testing).</summary>
public sealed class StubTrackFeedItemIngestion : ITrackFeedItemIngestion
{
    private readonly HashSet<(FeedId, FeedItemId)> _tracked = [];

    public Task<bool> IsTrackedAsync(FeedId feedId, FeedItemId itemId, CancellationToken cancellationToken = default)
        => Task.FromResult(_tracked.Contains((feedId, itemId)));

    public Task RecordAsync(FeedItemIngestion ingestion, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingestion);
        _tracked.Add((ingestion.FeedId, ingestion.ItemId));
        return Task.CompletedTask;
    }
}

#pragma warning restore CA1515

using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Orchestration.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Orchestration.Tests;

[Trait("TestType", "Unit")]
[Trait("Phase", "Orchestration")]
[Trait("Component", "IngestionOrchestrator")]
public sealed class IngestionOrchestrator_RunOnceAsync_Should
{
    private readonly ITestOutputHelper _output;

    public IngestionOrchestrator_RunOnceAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    // T014 — idempotency: items already in the store are skipped
    [Fact]
    public async Task SkipItemsAlreadyPresentInIngestionStoreWhenRunning()
    {
        try
        {
            var item1 = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-a"), itemId: FeedItemId.From("item-1"));
            var item2 = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-a"), itemId: FeedItemId.From("item-2"));

            var publishedItems = new List<FeedItem>();
            var fakeStore = new FakeFeedItemIngestionStore(alreadyTracked: [item1.ItemId]);
            var target = CreateTestTarget(
                items: [item1, item2],
                publishCapture: publishedItems,
                store: fakeStore);

            _output.WriteLine("Input items: item-1 (tracked), item-2 (new)");

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Published item count: {0}", publishedItems.Count);

            FeedItem published = Assert.Single(publishedItems);
            Assert.Equal(FeedItemId.From("item-2"), published.ItemId);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in SkipItemsAlreadyPresentInIngestionStoreWhenRunning");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T014 — idempotency: only new items are published when some are already ingested
    [Fact]
    public async Task PublishOnlyNewItemsWhenSomeItemsAreAlreadyIngested()
    {
        try
        {
            var itemOld = CanonicalFeedItemFactory.Create(itemId: FeedItemId.From("item-old"));
            var itemNew = CanonicalFeedItemFactory.Create(itemId: FeedItemId.From("item-new"));

            var publishedItems = new List<FeedItem>();
            var fakeStore = new FakeFeedItemIngestionStore(alreadyTracked: [itemOld.ItemId]);
            var target = CreateTestTarget(
                items: [itemOld, itemNew],
                publishCapture: publishedItems,
                store: fakeStore);

            _output.WriteLine("Input: 2 items, 1 already tracked");

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Published item count: {0}", publishedItems.Count);

            Assert.Equal(1, publishedItems.Count);
            Assert.Equal(FeedItemId.From("item-new"), publishedItems[0].ItemId);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in PublishOnlyNewItemsWhenSomeItemsAreAlreadyIngested");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T015 — continue-on-failure: remaining items are still published when one item fails
    [Fact]
    public async Task ContinuePublishingRemainingItemsWhenOneItemPublishFails()
    {
        try
        {
            var itemFail = CanonicalFeedItemFactory.Create(itemId: FeedItemId.From("item-fail"));
            var itemOk = CanonicalFeedItemFactory.Create(itemId: FeedItemId.From("item-ok"));

            var publishedItems = new List<FeedItem>();
            var fakeProduce = new FailingWriteFeedItems(publishedItems, failItemId: itemFail.ItemId);
            var fakeStore = new FakeFeedItemIngestionStore(alreadyTracked: []);
            var target = CreateTestTarget(
                items: [itemFail, itemOk],
                fakeProduce: fakeProduce,
                store: fakeStore);

            _output.WriteLine("Input: item-fail (publish will fail), item-ok");

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Published item count: {0}", publishedItems.Count);

            FeedItem published = Assert.Single(publishedItems);
            Assert.Equal(FeedItemId.From("item-ok"), published.ItemId);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ContinuePublishingRemainingItemsWhenOneItemPublishFails");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T015 — continue-on-failure: failed items are recorded in the ingestion store
    [Fact]
    public async Task RecordFailureForItemThatCouldNotBePublished()
    {
        try
        {
            var item = CanonicalFeedItemFactory.Create(itemId: FeedItemId.From("item-fail"));

            var savedIngestions = new List<FeedItemIngestion>();
            var fakeProduce = new AlwaysFailingWriteFeedItems();
            var fakeStore = new CapturingFeedItemIngestionStore(savedIngestions);
            var target = CreateTestTarget(
                items: [item],
                fakeProduce: fakeProduce,
                store: fakeStore);

            _output.WriteLine("Input item: {0}", item.ItemId);

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Saved ingestion count: {0}", savedIngestions.Count);

            FeedItemIngestion saved = Assert.Single(savedIngestions);
            Assert.Equal(item.FeedId, saved.FeedId);
            Assert.Equal(item.ItemId, saved.ItemId);
            Assert.Equal(FeedItemIngestionStatus.Failed, saved.Status);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in RecordFailureForItemThatCouldNotBePublished");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T016 — at-least-once retry: items not yet tracked are re-delivered on a subsequent run
    [Fact]
    public async Task RedeliverItemsNotYetTrackedAsIngestedWhenRunningAgain()
    {
        try
        {
            var item = CanonicalFeedItemFactory.Create();

            var publishedItems = new List<FeedItem>();
            // Empty store: simulates a re-run where a prior run was interrupted before tracking completed
            var fakeStore = new FakeFeedItemIngestionStore(alreadyTracked: []);
            var target = CreateTestTarget(
                items: [item],
                publishCapture: publishedItems,
                store: fakeStore);

            _output.WriteLine("Input item: {0}", item.ItemId);
            _output.WriteLine("Store state: empty (prior run was interrupted)");

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Published item count: {0}", publishedItems.Count);

            Assert.Single(publishedItems);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in RedeliverItemsNotYetTrackedAsIngestedWhenRunningAgain");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T017 — duplicate minimization: items already tracked are not republished on a subsequent run
    [Fact]
    public async Task NotRepublishItemsAlreadyTrackedAsIngestedWhenRunningAgain()
    {
        try
        {
            var item = CanonicalFeedItemFactory.Create();

            var publishedItems = new List<FeedItem>();
            // Item is already in the store from the prior run
            var fakeStore = new FakeFeedItemIngestionStore(alreadyTracked: [item.ItemId]);
            var target = CreateTestTarget(
                items: [item],
                publishCapture: publishedItems,
                store: fakeStore);

            _output.WriteLine("Input item: {0}", item.ItemId);
            _output.WriteLine("Store state: item already tracked (prior run completed)");

            await target.RunOnceAsync(CancellationToken.None);

            _output.WriteLine("Published item count: {0}", publishedItems.Count);

            Assert.Empty(publishedItems);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in NotRepublishItemsAlreadyTrackedAsIngestedWhenRunningAgain");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // — factory helpers —

    private static IngestionOrchestrator CreateTestTarget(
        IReadOnlyCollection<FeedItem> items,
        List<FeedItem>? publishCapture = null,
        IWriteFeedItems? fakeProduce = null,
        ITrackFeedItemIngestion? store = null)
    {
        return new IngestionOrchestrator(
            retrieve: new FakeRetrieveFeedItems(items),
            transform: new PassThroughTransformFeedItems(),
            produce: fakeProduce ?? new CapturingWriteFeedItems(publishCapture ?? []),
            ingestionStore: store ?? new FakeFeedItemIngestionStore([]));
    }

    // — test fakes —

    private sealed class FakeRetrieveFeedItems : IRetrieveFeedItems
    {
        private readonly IReadOnlyCollection<FeedItem> _items;

        public FakeRetrieveFeedItems(IReadOnlyCollection<FeedItem> items) => _items = items;

        public Task<IReadOnlyCollection<FeedItem>> RetrieveAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items);
    }

    private sealed class PassThroughTransformFeedItems : ITransformFeedItems
    {
        public Task<IReadOnlyCollection<FeedItem>> TransformAsync(IReadOnlyCollection<FeedItem> items, CancellationToken cancellationToken = default)
            => Task.FromResult(items);
    }

    private sealed class CapturingWriteFeedItems : IWriteFeedItems
    {
        private readonly List<FeedItem> _captured;

        public CapturingWriteFeedItems(List<FeedItem> captured) => _captured = captured;

        public Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, Guid runId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
        {
            var list = items.ToArray();
            _captured.AddRange(list);
            return Task.FromResult(new DeliveryResult(list.Length, list.Length, 0));
        }
    }

    private sealed class FailingWriteFeedItems : IWriteFeedItems
    {
        private readonly List<FeedItem> _captured;
        private readonly FeedItemId _failItemId;

        public FailingWriteFeedItems(List<FeedItem> captured, FeedItemId failItemId)
        {
            _captured = captured;
            _failItemId = failItemId;
        }

        public Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, Guid runId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
        {
            var list = items.ToArray();
            if (list.Any(i => i.ItemId == _failItemId))
                return Task.FromResult(new DeliveryResult(list.Length, 0, list.Length));
            _captured.AddRange(list);
            return Task.FromResult(new DeliveryResult(list.Length, list.Length, 0));
        }
    }

    private sealed class AlwaysFailingWriteFeedItems : IWriteFeedItems
    {
        public Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, Guid runId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
        {
            int count = items.Count();
            return Task.FromResult(new DeliveryResult(count, 0, count));
        }
    }

    private sealed class FakeFeedItemIngestionStore : ITrackFeedItemIngestion
    {
        private readonly HashSet<FeedItemId> _tracked;

        public FakeFeedItemIngestionStore(IEnumerable<FeedItemId> alreadyTracked)
            => _tracked = alreadyTracked.ToHashSet();

        public Task<bool> IsTrackedAsync(FeedId feedId, FeedItemId itemId, CancellationToken cancellationToken = default)
            => Task.FromResult(_tracked.Contains(itemId));

        public Task RecordAsync(FeedItemIngestion ingestion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CapturingFeedItemIngestionStore : ITrackFeedItemIngestion
    {
        private readonly List<FeedItemIngestion> _captured;

        public CapturingFeedItemIngestionStore(List<FeedItemIngestion> captured) => _captured = captured;

        public Task<bool> IsTrackedAsync(FeedId feedId, FeedItemId itemId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task RecordAsync(FeedItemIngestion ingestion, CancellationToken cancellationToken = default)
        {
            _captured.Add(ingestion);
            return Task.CompletedTask;
        }
    }
}

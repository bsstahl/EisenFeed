using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Orchestration;

/// <summary>
/// Orchestrates the four-stage ingestion pipeline with at-least-once delivery semantics.
/// 
/// Flow:
/// 1. Retrieve feed items from source
/// 2. Skip items already ingested (idempotency store lookup)
/// 3. Transform items to canonical form
/// 4. Produce to Kafka with per-item confirmation and store recording
/// 
/// On failure: continues processing remaining items (continue-on-item-failure).
/// On retry: re-delivers items not yet tracked as Published (at-least-once).
/// </summary>
public sealed class IngestionOrchestrator
{
    private readonly IRetrieveFeedItems _retrieve;
    private readonly ITransformFeedItems _transform;
    private readonly IWriteFeedItems _produce;
    private readonly ITrackFeedItemIngestion _ingestionStore;

    public IngestionOrchestrator(
        IRetrieveFeedItems retrieve,
        ITransformFeedItems transform,
        IWriteFeedItems produce,
        ITrackFeedItemIngestion ingestionStore)
    {
        _retrieve = retrieve ?? throw new ArgumentNullException(nameof(retrieve));
        _transform = transform ?? throw new ArgumentNullException(nameof(transform));
        _produce = produce ?? throw new ArgumentNullException(nameof(produce));
        _ingestionStore = ingestionStore ?? throw new ArgumentNullException(nameof(ingestionStore));
    }

    /// <summary>
    /// Run the ingestion pipeline once for a single feed.
    /// 
    /// Returns a run summary with discovered/ingested/skipped/failed counts.
    /// Idempotency: items already in the store are skipped (duplicate minimization).
    /// Resilience: items not yet tracked are re-delivered on retry (at-least-once).
    /// </summary>
    public async Task<IngestionRunSummary> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var runId = Guid.NewGuid().ToString("D");
        var startedAt = DateTimeOffset.UtcNow;
        
        // Stage 1: Retrieve
        var retrieved = await _retrieve.RetrieveAsync(cancellationToken).ConfigureAwait(false);
        int discoveredCount = retrieved.Count;

        // Stage 2: Filter (skip already-ingested)
        var toIngest = new List<FeedItem>();
        foreach (var item in retrieved)
        {
            bool isTracked = await _ingestionStore
                .IsTrackedAsync(item.FeedId, item.ItemId, cancellationToken)
                .ConfigureAwait(false);
            
            if (!isTracked)
                toIngest.Add(item);
        }

        int skippedCount = discoveredCount - toIngest.Count;

        // Stage 3: Transform
        var transformed = toIngest.Count == 0
            ? []
            : await _transform.TransformAsync(toIngest, cancellationToken).ConfigureAwait(false);

        // Stage 4: Produce (per-item, continue-on-fail)
        int ingestedCount = 0;
        int failedCount = 0;

        foreach (var item in transformed)
        {
            // Publish one item at a time to ensure continue-on-failure semantics
            var deliveryResult = await _produce
                .PublishAsync(new[] { item }, cancellationToken)
                .ConfigureAwait(false);

            var itemStatus = deliveryResult.DeliveredCount > 0
                ? FeedItemIngestionStatus.Publishing  // recorded as being in flight, awaiting Kafka confirmation
                : FeedItemIngestionStatus.Failed;     // publish failed immediately

            var ingestion = new FeedItemIngestion(
                FeedId: item.FeedId,
                ItemId: item.ItemId,
                Status: itemStatus,
                PublishAttemptedAt: startedAt,
                IngestedAt: itemStatus == FeedItemIngestionStatus.Publishing ? startedAt : null,
                AttemptCount: 1,
                LastError: itemStatus == FeedItemIngestionStatus.Failed ? "Publish failed" : null,
                KafkaMessageKey: $"{item.FeedId}:{item.ItemId}",
                KafkaTopic: itemStatus == FeedItemIngestionStatus.Publishing ? "feed-items" : null,
                KafkaPartition: itemStatus == FeedItemIngestionStatus.Publishing ? 0 : null,
                KafkaOffset: itemStatus == FeedItemIngestionStatus.Publishing ? 0 : null,
                RunId: runId);

            await _ingestionStore
                .RecordAsync(ingestion, cancellationToken)
                .ConfigureAwait(false);

            if (itemStatus == FeedItemIngestionStatus.Publishing)
                ingestedCount++;
            else
                failedCount++;
        }

        var completedAt = DateTimeOffset.UtcNow;
        var status = failedCount == 0 && ingestedCount > 0 ? FeedIngestionStatus.Succeeded
            : failedCount > 0 && ingestedCount > 0 ? FeedIngestionStatus.PartiallySucceeded
            : failedCount > 0 ? FeedIngestionStatus.Failed
            : FeedIngestionStatus.Succeeded;

        return new IngestionRunSummary(
            RunId: runId,
            FeedId: retrieved.FirstOrDefault()?.FeedId ?? FeedId.From(string.Empty),
            StartedAt: startedAt,
            CompletedAt: completedAt,
            Status: status,
            DiscoveredCount: discoveredCount,
            IngestedCount: ingestedCount,
            SkippedCount: skippedCount,
            FailedCount: failedCount);
    }
}

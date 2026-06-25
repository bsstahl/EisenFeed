using EisenFeed.Core.Models;

namespace EisenFeed.Core.Contracts;

/// <summary>
/// Persistent idempotency store for tracking which feed items have already been ingested.
/// </summary>
public interface ITrackFeedItemIngestion
{
    /// <summary>
    /// Check if a feed item has already been tracked (successfully ingested or failed).
    /// </summary>
    Task<bool> IsTrackedAsync(FeedId feedId, FeedItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a feed item ingestion attempt (Publishing, Published, or Failed status).
    /// </summary>
    Task RecordAsync(FeedItemIngestion ingestion, CancellationToken cancellationToken = default);
}

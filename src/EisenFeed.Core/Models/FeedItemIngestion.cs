namespace EisenFeed.Core.Models;

/// <summary>
/// Persistent idempotency record proving a feed item has been ingested to Kafka.
/// Used for duplicate minimization and at-least-once delivery semantics.
/// </summary>
public sealed record FeedItemIngestion(
    FeedId FeedId,
    FeedItemId ItemId,
    FeedItemIngestionStatus Status,
    DateTimeOffset PublishAttemptedAt,
    DateTimeOffset? IngestedAt,
    int AttemptCount,
    string? LastError,
    string KafkaMessageKey,
    string? KafkaTopic,
    int? KafkaPartition,
    long? KafkaOffset,
    string RunId);

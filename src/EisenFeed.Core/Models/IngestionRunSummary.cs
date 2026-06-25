namespace EisenFeed.Core.Models;

public sealed record IngestionRunSummary(
    string RunId,
    FeedId FeedId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    FeedIngestionStatus Status,
    int DiscoveredCount,
    int IngestedCount,
    int SkippedCount,
    int FailedCount);

namespace EisenFeed.Core.Models;

public sealed record IngestionRunSummary(
    int DiscoveredCount,
    int IngestedCount,
    int SkippedCount,
    int FailedCount);

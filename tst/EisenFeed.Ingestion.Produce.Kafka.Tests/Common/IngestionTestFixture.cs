namespace EisenFeed.Ingestion.Produce.Kafka.Tests.Common;

internal static class IngestionTestFixture
{
    public const string FeedId = "sample-feed";
    public static readonly DateTimeOffset ReferenceTime = new(2026, 6, 24, 0, 0, 0, TimeSpan.Zero);
}

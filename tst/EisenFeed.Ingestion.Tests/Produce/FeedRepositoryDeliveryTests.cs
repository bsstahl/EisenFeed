using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Tests.Common;

namespace EisenFeed.Ingestion.Tests.Produce;

public sealed class FeedRepositoryDeliveryTests
{
    [Fact]
    public async Task PublishAsync_WhenKafkaAckReceived_ReturnsSuccessfulDeliveryResult()
    {
        var target = CreateTestTarget();
        var item = CanonicalFeedItemFactory.Create();

        ProduceDeliveryResult result = await target.PublishAsync(new[] { item }, CancellationToken.None);

        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(1, result.DeliveredCount);
        Assert.Equal(0, result.FailedCount);
    }

    [Fact]
    public async Task PublishAsync_WhenKafkaPublishFails_ReturnsFailureAndPreservesAttemptCount()
    {
        var target = CreateTestTarget();
        var item = CanonicalFeedItemFactory.Create();

        ProduceDeliveryResult result = await target.PublishAsync(new[] { item }, CancellationToken.None);

        Assert.Equal(1, result.AttemptedCount);
        Assert.Equal(0, result.DeliveredCount);
        Assert.Equal(1, result.FailedCount);
    }

    private static IWriteFeedItems CreateTestTarget()
    {
        return new FeedRepository();
    }
}
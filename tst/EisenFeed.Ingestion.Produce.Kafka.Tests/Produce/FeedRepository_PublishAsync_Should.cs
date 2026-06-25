using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Produce.Kafka.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Produce.Kafka.Tests.Produce;

[Trait("TestType", "Unit")]
[Trait("Phase", "Produce")]
[Trait("Component", "FeedRepository")]
public sealed class FeedRepository_PublishAsync_Should
{
    private readonly ITestOutputHelper _output;

    public FeedRepository_PublishAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReturnSuccessfulDeliveryResultWhenKafkaAckIsReceived()
    {
        try
        {
            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult(new KafkaProduceAck("feed-items-ingested", 0, 1));
                }
            };

            var target = CreateTestTarget(fakeProducer);
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);
            _output.WriteLine("Input fakeProducer mode: success");

            DeliveryResult result = await target.PublishAsync(new[] { item }, CancellationToken.None);

            _output.WriteLine(
                "Output result: attempted={0}, delivered={1}, failed={2}",
                result.AttemptedCount,
                result.DeliveredCount,
                result.FailedCount);

            Assert.Equal(1, result.AttemptedCount);
            Assert.Equal(1, result.DeliveredCount);
            Assert.Equal(0, result.FailedCount);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnSuccessfulDeliveryResultWhenKafkaAckIsReceived");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ReturnFailureAndPreserveAttemptCountWhenKafkaPublishFails()
    {
        try
        {
            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, _) => throw new InvalidOperationException("Simulated kafka publish failure")
            };

            var target = CreateTestTarget(fakeProducer);
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);
            _output.WriteLine("Input fakeProducer mode: failure");

            DeliveryResult result = await target.PublishAsync(new[] { item }, CancellationToken.None);

            _output.WriteLine(
                "Output result: attempted={0}, delivered={1}, failed={2}",
                result.AttemptedCount,
                result.DeliveredCount,
                result.FailedCount);

            Assert.Equal(1, result.AttemptedCount);
            Assert.Equal(0, result.DeliveredCount);
            Assert.Equal(1, result.FailedCount);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ReturnFailureAndPreserveAttemptCountWhenKafkaPublishFails");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static IWriteFeedItems CreateTestTarget(IKafkaFeedProducer kafkaProducer)
    {
        return new FeedRepository(kafkaProducer, new FeedIdItemIdMessageMapper());
    }

    private sealed class FakeKafkaFeedProducer : IKafkaFeedProducer
    {
        public required Func<string, string, CancellationToken, Task<KafkaProduceAck>> ProduceAsyncHandler { get; init; }

        public Task<KafkaProduceAck> ProduceAsync(string key, string payload, CancellationToken cancellationToken = default)
        {
            return ProduceAsyncHandler(key, payload, cancellationToken);
        }
    }
}

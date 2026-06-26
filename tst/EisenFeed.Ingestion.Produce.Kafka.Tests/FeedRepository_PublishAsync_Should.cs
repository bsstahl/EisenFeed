using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Produce.Kafka.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Produce.Kafka.Tests;

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
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);
            _output.WriteLine("Input fakeProducer mode: success");

            DeliveryResult result = await target.PublishAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);
            _output.WriteLine("Input fakeProducer mode: failure");

            DeliveryResult result = await target.PublishAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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

    [Fact]
    public async Task TreatTimeoutExceptionAsItemFailureAndContinueProcessing()
    {
        try
        {
            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, _) => throw new TimeoutException("Kafka broker timeout")
            };

            var target = CreateTestTarget(fakeProducer);
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}",
                item.FeedId,
                item.ItemId);
            _output.WriteLine("Input fakeProducer mode: timeout exception");

            DeliveryResult result = await target.PublishAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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
            _output.WriteLine("Test failure in TreatTimeoutExceptionAsItemFailureAndContinueProcessing");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task TreatBrokerExceptionAsItemFailureAndContinueProcessing()
    {
        try
        {
            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, _) => throw new Exception("Kafka broker error: connection refused")
            };

            var target = CreateTestTarget(fakeProducer);
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}",
                item.FeedId,
                item.ItemId);
            _output.WriteLine("Input fakeProducer mode: broker error");

            DeliveryResult result = await target.PublishAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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
            _output.WriteLine("Test failure in TreatBrokerExceptionAsItemFailureAndContinueProcessing");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task ContinueProcessingMultipleItemsWhenIndividualItemsFail()
    {
        try
        {
            int callCount = 0;

            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, _) =>
                {
                    callCount++;
                    // Fail on 2nd and 4th items
                    if (callCount == 2 || callCount == 4)
                    {
                        throw new TimeoutException("Simulated timeout on item " + callCount);
                    }
                    return Task.FromResult(new KafkaProduceAck("feed-items-ingested", 0, 1));
                }
            };

            var target = CreateTestTarget(fakeProducer);
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var items = new[]
            {
                CanonicalFeedItemFactory.Create(),
                CanonicalFeedItemFactory.Create(),
                CanonicalFeedItemFactory.Create(),
                CanonicalFeedItemFactory.Create()
            };

            _output.WriteLine("Input: 4 items (will fail on 2nd and 4th)");

            DeliveryResult result = await target.PublishAsync(items, runId, occurredAt, CancellationToken.None);

            _output.WriteLine(
                "Output result: attempted={0}, delivered={1}, failed={2}",
                result.AttemptedCount,
                result.DeliveredCount,
                result.FailedCount);

            Assert.Equal(4, result.AttemptedCount);
            Assert.Equal(2, result.DeliveredCount);
            Assert.Equal(2, result.FailedCount);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in ContinueProcessingMultipleItemsWhenIndividualItemsFail");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task PropagateOperationCanceledExceptionWithoutTreatingAsItemFailure()
    {
        try
        {
            var fakeProducer = new FakeKafkaFeedProducer
            {
                ProduceAsyncHandler = (_, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new OperationCanceledException("External cancellation");
                }
            };

            var target = CreateTestTarget(fakeProducer);
            var item = CanonicalFeedItemFactory.Create();

            _output.WriteLine("Input item: feedId={0}, itemId={1}", item.FeedId, item.ItemId);
            _output.WriteLine("Input fakeProducer mode: operation canceled");

            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;

            OperationCanceledException? caughtException = null;
            try
            {
                await target.PublishAsync(new[] { item }, runId, occurredAt, CancellationToken.None);
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            _output.WriteLine("Caught OperationCanceledException as expected");

            Assert.NotNull(caughtException);
            Assert.IsType<OperationCanceledException>(caughtException);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in PropagateOperationCanceledExceptionWithoutTreatingAsItemFailure");
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

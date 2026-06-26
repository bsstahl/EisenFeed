using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed class FeedRepository : IWriteFeedItems
{
    private readonly IKafkaFeedProducer _kafkaProducer;
    private readonly FeedIdItemIdMessageMapper _messageMapper;

    public FeedRepository(IKafkaFeedProducer kafkaProducer, FeedIdItemIdMessageMapper messageMapper)
    {
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _messageMapper = messageMapper ?? throw new ArgumentNullException(nameof(messageMapper));
    }

    public async Task<DeliveryResult> PublishAsync(IEnumerable<FeedItem> items, Guid runId, DateTimeOffset occurredAt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<FeedKafkaMessage> messages = await _messageMapper
            .MapMessagesAsync(items, runId, occurredAt, cancellationToken)
            .ConfigureAwait(false);

        int attemptedCount = 0;
        int deliveredCount = 0;
        int failedCount = 0;

        foreach (FeedKafkaMessage message in messages)
        {
            attemptedCount++;

            try
            {
                await _kafkaProducer
                    .ProduceAsync(message.Key, message.Payload, cancellationToken)
                    .ConfigureAwait(false);

                deliveredCount++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Intentionally catch all exceptions (except cancellation) to implement "continue on item failure" pattern
            catch (Exception)
            {
                failedCount++;
            }
#pragma warning restore CA1031
        }

        return new DeliveryResult(attemptedCount, deliveredCount, failedCount);
    }
}
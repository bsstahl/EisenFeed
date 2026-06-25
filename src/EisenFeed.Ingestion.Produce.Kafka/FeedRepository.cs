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

    public Task<ProduceDeliveryResult> PublishAsync(IEnumerable<FeedItem> items, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
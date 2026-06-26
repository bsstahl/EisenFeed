namespace EisenFeed.Ingestion.Produce.Kafka;

public interface IKafkaFeedProducer
{
    Task<KafkaProduceAck> ProduceAsync(string key, string payload, CancellationToken cancellationToken = default);
}

public sealed record KafkaProduceAck(string Topic, int Partition, long Offset);
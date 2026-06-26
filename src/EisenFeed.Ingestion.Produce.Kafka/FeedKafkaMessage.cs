namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed record FeedKafkaMessage(string Key, string Payload);
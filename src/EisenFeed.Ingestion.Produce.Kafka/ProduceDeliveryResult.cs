namespace EisenFeed.Ingestion.Produce.Kafka;

public sealed record ProduceDeliveryResult(int AttemptedCount, int DeliveredCount, int FailedCount);

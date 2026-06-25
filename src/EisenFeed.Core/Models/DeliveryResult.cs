namespace EisenFeed.Core.Models;

public sealed record DeliveryResult(int AttemptedCount, int DeliveredCount, int FailedCount);
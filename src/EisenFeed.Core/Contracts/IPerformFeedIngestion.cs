namespace EisenFeed.Core.Contracts;

public interface IPerformFeedIngestion
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}
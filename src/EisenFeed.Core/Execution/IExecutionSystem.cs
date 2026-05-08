namespace EisenFeed.Core.Execution;

public interface IExecutionSystem
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}

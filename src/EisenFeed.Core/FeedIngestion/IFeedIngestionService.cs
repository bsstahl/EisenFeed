using EisenFeed.Core.Models;

namespace EisenFeed.Core.FeedIngestion;

public interface IFeedIngestionService
{
    Task<IReadOnlyCollection<FeedItem>> IngestAsync(CancellationToken cancellationToken = default);
}

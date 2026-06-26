using EisenFeed.Core.Models;

namespace EisenFeed.Ingestion.Transform.Rules;

public interface ITransformFeedItemRule
{
    Task<FeedItem> ApplyAsync(FeedItem item, CancellationToken cancellationToken = default);
}
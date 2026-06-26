using EisenFeed.Core.Models;
using TestHelperExtensions;

namespace EisenFeed.Ingestion.Transform.Rules.Tests.Common;

internal static class CanonicalFeedItemFactory
{
    internal static FeedItem Create(
        FeedId? feedId = null,
        FeedItemId? itemId = null,
        string? title = null,
        string? content = null,
        DateTimeOffset? publishedAt = null)
    {
        feedId ??= FeedId.From(string.Empty.GetRandom());
        itemId ??= FeedItemId.From(string.Empty.GetRandom());
        title ??= string.Empty.GetRandom();
        content ??= string.Empty.GetRandom();

        return new FeedItem(
            feedId,
            itemId,
            publishedAt ?? IngestionTestFixture.ReferenceTime,
            title,
            content);
    }
}

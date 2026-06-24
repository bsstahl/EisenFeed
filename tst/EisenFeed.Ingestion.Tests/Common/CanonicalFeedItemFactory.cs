using EisenFeed.Core.Models;
using TestHelperExtensions;

namespace EisenFeed.Ingestion.Tests.Common;

internal static class CanonicalFeedItemFactory
{
    internal static FeedItem Create(
        string? feedId = null,
        string? itemId = null,
        string? title = null,
        string? content = null,
        DateTimeOffset? publishedAt = null)
    {
        feedId ??= string.Empty.GetRandom();
        itemId ??= string.Empty.GetRandom();
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

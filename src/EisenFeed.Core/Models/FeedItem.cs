namespace EisenFeed.Core.Models;

public sealed record FeedItem(
    FeedId FeedId,
    FeedItemId ItemId,
    DateTimeOffset PublishedAt,
    string Title,
    string Content);

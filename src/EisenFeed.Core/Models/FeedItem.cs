namespace EisenFeed.Core.Models;

public sealed record FeedItem(
    string FeedId,
    string ItemId,
    DateTimeOffset PublishedAt,
    string Title,
    string Content);

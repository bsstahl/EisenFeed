using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Tests.Common;

namespace EisenFeed.Ingestion.Tests.Produce;

public sealed class FeedIdItemIdMessageMapperTests
{
    [Fact]
    public async Task MapMessagesAsync_WhenItemsProvided_MapsKeyAsFeedIdColonItemId()
    {
        var mapper = CreateSut();
        var item = CanonicalFeedItemFactory.Create(feedId: "feed-a", itemId: "item-42");

        IReadOnlyCollection<FeedKafkaMessage> messages = await mapper.MapMessagesAsync(new[] { item }, CancellationToken.None);

        FeedKafkaMessage message = Assert.Single(messages);
        Assert.Equal("feed-a:item-42", message.Key);
    }

    [Fact]
    public async Task MapMessagesAsync_WhenItemsProvided_MapsPayloadFieldsFromCanonicalFeedItem()
    {
        var mapper = CreateSut();
        var item = CanonicalFeedItemFactory.Create(feedId: "feed-b", itemId: "item-99", title: "title", content: "body");

        IReadOnlyCollection<FeedKafkaMessage> messages = await mapper.MapMessagesAsync(new[] { item }, CancellationToken.None);

        FeedKafkaMessage message = Assert.Single(messages);
        Assert.Contains("\"feedId\":\"feed-b\"", message.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"itemId\":\"item-99\"", message.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"title\":\"title\"", message.Payload, StringComparison.OrdinalIgnoreCase);
    }

    private static FeedIdItemIdMessageMapper CreateSut()
    {
        return new FeedIdItemIdMessageMapper();
    }
}
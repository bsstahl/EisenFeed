using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Produce.Kafka.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Produce.Kafka.Tests.Produce;

[Trait("TestType", "Unit")]
[Trait("Phase", "Produce")]
[Trait("Component", "FeedIdItemIdMessageMapper")]
public sealed class FeedIdItemIdMessageMapper_MapMessagesAsync_Should
{
    private readonly ITestOutputHelper _output;

    public FeedIdItemIdMessageMapper_MapMessagesAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MapTheKeyAsFeedIdColonItemIdWhenItemsAreProvided()
    {
        try
        {
            var target = CreateTestTarget();
            var item = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-a"), itemId: FeedItemId.From("item-42"));

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);

            IReadOnlyCollection<FeedKafkaMessage> messages = await target.MapMessagesAsync(new[] { item }, CancellationToken.None);

            _output.WriteLine("Output message count: {0}", messages.Count);

            FeedKafkaMessage message = Assert.Single(messages);

            _output.WriteLine("Output message: key={0}, payloadLength={1}", message.Key, message.Payload?.Length ?? 0);

            Assert.Equal("feed-a:item-42", message.Key);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in MapTheKeyAsFeedIdColonItemIdWhenItemsAreProvided");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    [Fact]
    public async Task MapPayloadFieldsFromCanonicalFeedItemWhenItemsAreProvided()
    {
        try
        {
            var target = CreateTestTarget();
            var item = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-b"), itemId: FeedItemId.From("item-99"), title: "title", content: "body");

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, content={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content);

            IReadOnlyCollection<FeedKafkaMessage> messages = await target.MapMessagesAsync(new[] { item }, CancellationToken.None);

            _output.WriteLine("Output message count: {0}", messages.Count);

            FeedKafkaMessage message = Assert.Single(messages);

            _output.WriteLine("Output message: key={0}, payload={1}", message.Key, message.Payload);

            Assert.Contains("\"feedId\":\"feed-b\"", message.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"itemId\":\"item-99\"", message.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"title\":\"title\"", message.Payload, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in MapPayloadFieldsFromCanonicalFeedItemWhenItemsAreProvided");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static FeedIdItemIdMessageMapper CreateTestTarget()
    {
        return new FeedIdItemIdMessageMapper();
    }
}

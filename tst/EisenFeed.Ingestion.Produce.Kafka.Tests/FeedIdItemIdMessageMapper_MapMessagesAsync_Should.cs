using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Produce.Kafka;
using EisenFeed.Ingestion.Produce.Kafka.Tests.Common;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Produce.Kafka.Tests;

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
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-a"), itemId: FeedItemId.From("item-42"));

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, contentLength={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content?.Length ?? 0);
            _output.WriteLine("Input runId: {0}", runId);
            _output.WriteLine("Input occurredAt: {0:O}", occurredAt);

            IReadOnlyCollection<FeedKafkaMessage> messages = await target.MapMessagesAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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
            var runId = Guid.NewGuid();
            var occurredAt = DateTimeOffset.UtcNow;
            var item = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-b"), itemId: FeedItemId.From("item-99"), title: "title", content: "body");

            _output.WriteLine(
                "Input item: feedId={0}, itemId={1}, publishedAt={2:O}, title={3}, content={4}",
                item.FeedId,
                item.ItemId,
                item.PublishedAt,
                item.Title,
                item.Content);
            _output.WriteLine("Input runId: {0}", runId);
            _output.WriteLine("Input occurredAt: {0:O}", occurredAt);

            IReadOnlyCollection<FeedKafkaMessage> messages = await target.MapMessagesAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

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

    [Fact]
    public async Task IncludeRequiredContractFieldsInPayload()
    {
        try
        {
            var target = CreateTestTarget();
            var runId = new Guid("3f8fef3f-8ee9-4e6b-9c11-2cded18f47fa");
            var occurredAt = DateTimeOffset.Parse("2026-06-24T12:00:00Z");
            var item = CanonicalFeedItemFactory.Create(feedId: FeedId.From("feed-x"), itemId: FeedItemId.From("item-1"));

            _output.WriteLine("Input runId: {0}", runId);
            _output.WriteLine("Input occurredAt: {0:O}", occurredAt);

            IReadOnlyCollection<FeedKafkaMessage> messages = await target.MapMessagesAsync(new[] { item }, runId, occurredAt, CancellationToken.None);

            FeedKafkaMessage message = Assert.Single(messages);

            _output.WriteLine("Output payload: {0}", message.Payload);

            // Verify required contract fields
            Assert.Contains("\"schemaVersion\":\"1.0\"", message.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"eventType\":\"feed-item-ingested\"", message.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains($"\"runId\":\"{runId:D}\"", message.Payload, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"occurredAt\":\"2026-06-24T12:00:00+00:00\"", message.Payload, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in IncludeRequiredContractFieldsInPayload");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    private static FeedIdItemIdMessageMapper CreateTestTarget()
    {
        return new FeedIdItemIdMessageMapper();
    }
}

using Aspire.Hosting;
using Aspire.Hosting.Testing;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Service;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace EisenFeed.Ingestion.Service.Tests;

[Trait("TestType", "Integration")]
[Trait("Phase", "All")]
[Trait("Component", "IngestionPipeline")]
public sealed class IngestionPipeline_RunOnceAsync_Should : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private DistributedApplication? _app;
    private HttpClient? _ingestionClient;

    public IngestionPipeline_RunOnceAsync_Should(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.EisenFeed_AppHost>();

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // "ingestion-service" is not yet registered in the AppHost — this will
        // throw at runtime until T041 wires the service in Phase 7 (RED).
        _ingestionClient = _app.CreateHttpClient("ingestion-service");
    }

    public async Task DisposeAsync()
    {
        _ingestionClient?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    // T051 — US2 integration: full pipeline run publishes items on first execution
    [Fact]
    public async Task PublishAllDiscoveredItemsToKafkaWhenRunningForTheFirstTime()
    {
        try
        {
            _output.WriteLine("Triggering first ingestion run via POST /ingest/run");

            HttpResponseMessage response = await _ingestionClient!.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);

            response.EnsureSuccessStatusCode();

            // IngestionRunSummary does not exist yet — compile RED until Phase 7/9.
            IngestionRunSummary? summary = await response.Content
                .ReadFromJsonAsync<IngestionRunSummary>(CancellationToken.None);

            _output.WriteLine(
                "Run summary: discovered={0}, ingested={1}, skipped={2}, failed={3}",
                summary?.DiscoveredCount, summary?.IngestedCount,
                summary?.SkippedCount, summary?.FailedCount);

            Assert.NotNull(summary);
            Assert.True(summary.DiscoveredCount > 0);
            Assert.Equal(summary.DiscoveredCount, summary.IngestedCount);
            Assert.Equal(0, summary.SkippedCount);
            Assert.Equal(0, summary.FailedCount);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in PublishAllDiscoveredItemsToKafkaWhenRunningForTheFirstTime");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // T051 — US2 integration: re-run skips all items already present in the idempotency store
    [Fact]
    public async Task SkipAllItemsWhenReRunWithNoNewItemsInFeed()
    {
        try
        {
            _output.WriteLine("Triggering first ingestion run");

            await _ingestionClient!.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);

            _output.WriteLine("Triggering second ingestion run (re-run)");

            HttpResponseMessage response = await _ingestionClient.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);

            response.EnsureSuccessStatusCode();

            IngestionRunSummary? summary = await response.Content
                .ReadFromJsonAsync<IngestionRunSummary>(CancellationToken.None);

            _output.WriteLine(
                "Re-run summary: discovered={0}, ingested={1}, skipped={2}, failed={3}",
                summary?.DiscoveredCount, summary?.IngestedCount,
                summary?.SkippedCount, summary?.FailedCount);

            Assert.NotNull(summary);
            Assert.Equal(0, summary.IngestedCount);
            Assert.True(summary.SkippedCount > 0);
            Assert.Equal(0, summary.FailedCount);
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in SkipAllItemsWhenReRunWithNoNewItemsInFeed");
            _output.WriteLine(ex.ToString());
            throw;
        }
    }

    // Mixed scenario: handles successes, failures, and duplicates in subsequent runs
    [Fact]
    public async Task HandleMixedSuccessesFailuresAndDuplicatesAcrossRuns()
    {
        try
        {
            _output.WriteLine("=== FIRST RUN: mixed success and failure ===");
            
            // First run: 3 items where item-2 will fail
            var run1Items = new[]
            {
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-1"), DateTimeOffset.UtcNow, "Item 1", "Content 1"),
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-2"), DateTimeOffset.UtcNow, "Item 2", "Content 2"),
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-3"), DateTimeOffset.UtcNow, "Item 3", "Content 3"),
            };
            
            // Configure stubs via test endpoint
            await _ingestionClient!.PostAsJsonAsync(
                "/test/configure-stubs",
                new { OverrideItems = run1Items, FailItemIds = new[] { FeedItemId.From("item-2") } });

            HttpResponseMessage response1 = await _ingestionClient!.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);
            response1.EnsureSuccessStatusCode();

            IngestionRunSummary? summary1 = await response1.Content
                .ReadFromJsonAsync<IngestionRunSummary>(CancellationToken.None);

            _output.WriteLine(
                "Run 1: discovered={0}, ingested={1}, skipped={2}, failed={3}",
                summary1?.DiscoveredCount, summary1?.IngestedCount,
                summary1?.SkippedCount, summary1?.FailedCount);

            Assert.NotNull(summary1);
            Assert.Equal(3, summary1.DiscoveredCount);
            Assert.Equal(2, summary1.IngestedCount);      // items 1, 3
            Assert.Equal(0, summary1.SkippedCount);
            Assert.Equal(1, summary1.FailedCount);        // item 2

            _output.WriteLine("=== SECOND RUN: duplicates, new items, new failures ===");

            // Second run: 4 items (1 duplicate, 1 duplicate-fail, 2 new where 5 will fail)
            var run2Items = new[]
            {
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-1"), DateTimeOffset.UtcNow, "Item 1", "Content 1"),  // duplicate success
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-2"), DateTimeOffset.UtcNow, "Item 2", "Content 2"),  // duplicate failure
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-4"), DateTimeOffset.UtcNow, "Item 4", "Content 4"),  // new success
                new FeedItem(FeedId.From("test-feed"), FeedItemId.From("item-5"), DateTimeOffset.UtcNow, "Item 5", "Content 5"),  // new failure
            };
            
            // Configure stubs for run 2
            await _ingestionClient.PostAsJsonAsync(
                "/test/configure-stubs",
                new { OverrideItems = run2Items, FailItemIds = new[] { FeedItemId.From("item-5") } });

            HttpResponseMessage response2 = await _ingestionClient!.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);
            response2.EnsureSuccessStatusCode();

            IngestionRunSummary? summary2 = await response2.Content
                .ReadFromJsonAsync<IngestionRunSummary>(CancellationToken.None);

            _output.WriteLine(
                "Run 2: discovered={0}, ingested={1}, skipped={2}, failed={3}",
                summary2?.DiscoveredCount, summary2?.IngestedCount,
                summary2?.SkippedCount, summary2?.FailedCount);

            Assert.NotNull(summary2);
            Assert.Equal(4, summary2.DiscoveredCount);
            Assert.Equal(1, summary2.IngestedCount);      // only item 4 (new)
            Assert.Equal(2, summary2.SkippedCount);       // items 1, 2 (duplicates)
            Assert.Equal(1, summary2.FailedCount);        // item 5 (new failure)

            _output.WriteLine("=== THIRD RUN: verify all duplicates are skipped ===");

            // Third run: same 4 items again (all should be skipped, nothing ingested, nothing failed)
            var run3Items = run2Items;  // same 4 items
            
            // Configure stubs for run 3
            await _ingestionClient.PostAsJsonAsync(
                "/test/configure-stubs",
                new { OverrideItems = run3Items, FailItemIds = new[] { FeedItemId.From("item-5") } });

            HttpResponseMessage response3 = await _ingestionClient!.PostAsync(
                "/ingest/run", content: null, CancellationToken.None);
            response3.EnsureSuccessStatusCode();

            IngestionRunSummary? summary3 = await response3.Content
                .ReadFromJsonAsync<IngestionRunSummary>(CancellationToken.None);

            _output.WriteLine(
                "Run 3: discovered={0}, ingested={1}, skipped={2}, failed={3}",
                summary3?.DiscoveredCount, summary3?.IngestedCount,
                summary3?.SkippedCount, summary3?.FailedCount);

            Assert.NotNull(summary3);
            Assert.Equal(4, summary3.DiscoveredCount);
            Assert.Equal(0, summary3.IngestedCount);      // nothing new
            Assert.Equal(4, summary3.SkippedCount);       // all duplicates
            Assert.Equal(0, summary3.FailedCount);        // none processed so none fail
        }
        catch (Exception ex)
        {
            _output.WriteLine("Test failure in HandleMixedSuccessesFailuresAndDuplicatesAcrossRuns");
            _output.WriteLine(ex.ToString());
            throw;
        }
        finally
        {
            // Clean up test overrides
            await _ingestionClient!.PostAsJsonAsync(
                "/test/configure-stubs",
                new { OverrideItems = (IReadOnlyCollection<FeedItem>?)null, FailItemIds = (IReadOnlyCollection<FeedItemId>?)null });
        }
    }
}

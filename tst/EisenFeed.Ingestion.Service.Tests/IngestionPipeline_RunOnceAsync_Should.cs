using Aspire.Hosting;
using Aspire.Hosting.Testing;
using EisenFeed.Core.Models;
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
}

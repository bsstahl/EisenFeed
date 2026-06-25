using EisenFeed.Core.Models;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapPost("/ingest/run", RunIngestion);

app.Run();

// Stub: returns incomplete/minimal IngestionRunSummary (fails tests for functional reasons)
async Task<IngestionRunSummary> RunIngestion(CancellationToken cancellationToken)
{
    // TODO: Wire real orchestration logic in Phase 7 (T041)
    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
    return new IngestionRunSummary(
        DiscoveredCount: 0,
        IngestedCount: 0,
        SkippedCount: 0,
        FailedCount: 0);
}

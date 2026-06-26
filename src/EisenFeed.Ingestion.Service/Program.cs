using EisenFeed.Core.Contracts;
using EisenFeed.Core.Models;
using EisenFeed.Ingestion.Orchestration;
using EisenFeed.Ingestion.Service;

var builder = WebApplication.CreateBuilder(args);

// Wire up the orchestration pipeline with stub implementations for now
builder.Services.AddScoped<IRetrieveFeedItems, StubRetrieveFeedItems>();
builder.Services.AddScoped<ITransformFeedItems, PassThroughTransformFeedItems>();
builder.Services.AddScoped<IWriteFeedItems, StubWriteFeedItems>();
builder.Services.AddSingleton<ITrackFeedItemIngestion, StubTrackFeedItemIngestion>();  // Singleton for in-memory tracking across requests
builder.Services.AddScoped<IngestionOrchestrator>();

var app = builder.Build();

app.MapPost("/ingest/run", RunIngestion);

// Test-only endpoint for configuring stubs
if (app.Environment.IsDevelopment())
{
    app.MapPost("/test/configure-stubs", ConfigureStubs);
}

app.Run();

async Task<IngestionRunSummary> RunIngestion(IngestionOrchestrator orchestrator, CancellationToken cancellationToken)
{
    return await orchestrator.RunOnceAsync(cancellationToken).ConfigureAwait(false);
}

async Task ConfigureStubs(HttpContext context)
{
    var config = await context.Request.ReadFromJsonAsync<TestStubConfiguration>().ConfigureAwait(false);
    
    if (config?.OverrideItems != null)
    {
        StubRetrieveFeedItems.TestOverrideItems = config.OverrideItems;
    }
    
    if (config?.FailItemIds != null)
    {
        StubWriteFeedItems.TestFailItemIds = config.FailItemIds;
    }
    
    context.Response.StatusCode = 200;
    await context.Response.WriteAsJsonAsync(new { message = "Stubs configured" }).ConfigureAwait(false);
}

#pragma warning disable CA1812 // Type is instantiated via deserialization
sealed record TestStubConfiguration(IReadOnlyCollection<FeedItem>? OverrideItems, IReadOnlyCollection<FeedItemId>? FailItemIds);
#pragma warning restore CA1812

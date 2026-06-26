var builder = DistributedApplication.CreateBuilder(args);

var ingestionService = builder
    .AddProject<Projects.EisenFeed_Ingestion_Service>("ingestion-service")
    .WithHttpEndpoint(port: 5001);

builder.Build().Run();

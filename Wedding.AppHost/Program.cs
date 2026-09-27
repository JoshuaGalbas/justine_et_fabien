using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject("wedding-api", "../api/WeddingApi/WeddingApi.Functions/WeddingApi.Functions.csproj");
builder.AddNpmApp("wedding-web", "..", "dev:web");
builder.AddNpmApp("wedding-admin", "..", "dev:admin");

builder.Build().Run();

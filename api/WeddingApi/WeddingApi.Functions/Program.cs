using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddLogging();
builder.Services.AddSingleton<WeddingApi.Functions.Services.IHouseholdRepository, WeddingApi.Functions.Services.HouseholdRepository>();

builder.Build().Run();

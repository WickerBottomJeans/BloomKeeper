using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using BloomKeeper.PlayFabFunctions.Services;
using BloomKeeper.PlayFabFunctions.Services.PlayerStateStorage;
using BloomKeeper.PlayFabFunctions.Services.ShopGrants;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Services.AddSingleton<PlayFabFunctionContextReader>();
builder.Services.AddSingleton<PlayFabInventoryService>();
builder.Services.AddSingleton<PlayFabLivesConfigService>();
builder.Services.AddSingleton<PlayFabEntityFileClient>();
builder.Services.AddSingleton<LivesFileStore>();
builder.Services.AddSingleton<LivesService>();
builder.Services.AddSingleton<ShopPurchaseStore>();
builder.Services.AddSingleton<IShopGrantHandler, InventoryItemShopGrantHandler>();
builder.Services.AddSingleton<IShopGrantHandler, UnlimitedLivesShopGrantHandler>();
builder.Services.AddSingleton<ShopGrantDispatcher>();

// Start the Azure Functions host.
builder.Build().Run();

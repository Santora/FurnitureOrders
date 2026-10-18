using FurnitureOrders.Client;
using FurnitureOrders.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("Chybí konfigurace ApiBaseUrl.");

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddMudServices();
builder.Services.AddScoped<CustomerApiClient>();
builder.Services.AddScoped<MaterialApiClient>();
builder.Services.AddScoped<OperationApiClient>();
builder.Services.AddScoped<ProductApiClient>();
builder.Services.AddScoped<ProductRoutingApiClient>();

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("cs-CZ");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("cs-CZ");

await builder.Build().RunAsync();

using FurnitureOrders.Api.Data;
using Microsoft.EntityFrameworkCore;
using FurnitureOrders.Api.Endpoints;
using FurnitureOrders.Api.Services;
using FurnitureOrders.Api.Ai;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IOperationService, OperationService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductRoutingService, ProductRoutingService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
builder.Services.AddHttpClient<AnthropicOrderTextExtractor>(c => c.Timeout = Timeout.InfiniteTimeSpan);
builder.Services.AddSingleton<FakeOrderTextExtractor>();
builder.Services.AddScoped<IOrderTextExtractor>(sp =>
    sp.GetRequiredService<IOptions<AiOptions>>().Value.Provider.ToLowerInvariant() switch
    {
        "anthropic" => sp.GetRequiredService<AnthropicOrderTextExtractor>(),
        "fake" => sp.GetRequiredService<FakeOrderTextExtractor>(),
        _ => new NoAiOrderTextExtractor()
    });
builder.Services.AddScoped<IOrderDraftService, OrderDraftService>();


var app = builder.Build();

await app.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapCustomerEndpoints();
app.MapMaterialEndpoints();
app.MapOperationEndpoints();
app.MapProductEndpoints();
app.MapProductRoutingEndpoints();
app.MapOrderEndpoints();
app.MapOrderDraftEndpoints();

app.Run();

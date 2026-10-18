using FurnitureOrders.Api.Data;
using Microsoft.EntityFrameworkCore;
using FurnitureOrders.Api.Endpoints;
using FurnitureOrders.Api.Services;

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



var app = builder.Build();

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

app.Run();

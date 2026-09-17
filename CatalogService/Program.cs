using CatalogService.Data;
using CatalogService.Dtos;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API Explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// All Catalog endpoints are public; no JWT security scheme needed in Swagger.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Catalog Service API", Version = "v1" });
});

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString == "InMemory")
{
    builder.Services.AddDbContext<CatalogServiceContext>(o =>
        o.UseInMemoryDatabase("CatalogServiceDb"));
}
else
{
    builder.Services.AddDbContext<CatalogServiceContext>(o =>
        o.UseNpgsql(connectionString));
}

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();

// Same VALIDATION_ERROR envelope as UserService (see its Program.cs for the full
// rationale) — kept consistent across services so every 400 body has the same shape.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage));

        var body = new ApiErrorResponse("VALIDATION_ERROR",
            string.IsNullOrWhiteSpace(message) ? "Validation failed" : message);

        return new BadRequestObjectResult(body);
    };
});

// ── Build & Middleware Pipeline ───────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    await DataSeeder.SeedAsync(dbContext);
}

app.MapControllers();

app.Run();

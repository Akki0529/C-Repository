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
var connectionString = builder.Configuration.GetConnectionString("CatalogDb");
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

// In production, nginx routes /catalog/... to this process but does not strip the
// prefix (no trailing slash on proxy_pass). UsePathBase tells ASP.NET its mount
// point so routing, Swagger, and generated links all use the correct base path.
if (!app.Environment.IsDevelopment())
    app.UsePathBase("/catalog");

// Swagger enabled in all environments (milestone-5 acceptance criterion).
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    await DataSeeder.SeedAsync(dbContext);
}
else if (connectionString != "InMemory")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    await dbContext.Database.MigrateAsync();
    // Seeder is idempotent (returns immediately if books already exist) so this is
    // safe across restarts. Needed in production because the dev-only seed branch
    // above doesn't run, leaving the catalog empty on first deploy.
    await DataSeeder.SeedAsync(dbContext);
}

app.MapControllers();

app.MapGet("/health", async (CatalogServiceContext db, IConfiguration config) =>
{
    var cs = config.GetConnectionString("CatalogDb");
    if (string.IsNullOrEmpty(cs) || cs == "InMemory")
        return Results.Ok(new { service = "CatalogService", database = "InMemory", status = "UP" });
    try
    {
        var migrations = await db.Database.GetAppliedMigrationsAsync();
        return Results.Ok(new { service = "CatalogService", database = "catalogservicedb", migrations = migrations.Count(), status = "UP" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { service = "CatalogService", status = "DOWN", error = ex.Message }, statusCode: 503);
    }
});

app.Run();

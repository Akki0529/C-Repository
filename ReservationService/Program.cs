using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReservationService.Data;
using ReservationService.Dtos;
using ReservationService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API Explorer ────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ReturnRequest.Condition binds from the wire's "GOOD"/"FAIR"/"POOR"/"DAMAGED".
        // JsonStringEnumConverter matches enum names case-insensitively by default, and
        // none of BookCondition's names contain underscores, so no custom naming logic
        // is needed on top of this.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        // Several response DTOs are shared across two contract shapes that differ only
        // in which optional fields are present (e.g. WaitlistEntryResponse is used for
        // both the "join" response, which has no bookAuthor, and the "list" response,
        // which does; ReturnResponse's dueDate is only in the contract's late-return
        // example). Omitting null properties entirely — rather than serializing them as
        // null — makes both cases match api-contracts.md exactly without needing a
        // separate DTO per shape.
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// JWT Authorize button added in Phase 2 once we verify the Swashbuckle 10.x API.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Reservation Service API", Version = "v1" });
});

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("ReservationDb");
if (connectionString == "InMemory")
{
    builder.Services.AddDbContext<ReservationServiceContext>(o =>
        o.UseInMemoryDatabase("ReservationServiceDb"));
}
else
{
    builder.Services.AddDbContext<ReservationServiceContext>(o =>
        o.UseNpgsql(connectionString));
}

// ── JWT Authentication ─────────────────────────────────────────────────────────
// ReservationService validates tokens locally using the shared secret.
// It does NOT call UserService on every request to validate the token — that would
// create a performance bottleneck and a hard dependency. The shared secret is what
// makes stateless JWT validation work across services.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be set in appsettings.json");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Same rationale as UserService: without this, a missing/invalid token produces
    // an empty 401 body instead of the {error, message, timestamp} envelope
    // api-contracts.md's "Common Error Responses" section requires everywhere.
    // (No OnForbidden override here — the LIBRARIAN-only checks on checkout/return
    // are done manually inside the controller, not via [Authorize(Roles=)], so they
    // never trigger this pipeline's forbidden handling; see ReservationsController.)
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var body = new ApiErrorResponse("UNAUTHORIZED", "Authentication required");
            await context.Response.WriteAsync(JsonSerializer.Serialize(body,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    };
});

builder.Services.AddAuthorization();

// ── HTTP Clients: UserService and CatalogService ──────────────────────────────
// ReservationService is the orchestrator: it calls both other services.
//
//   UserService  → validate user exists + check active reservation count before creating
//   CatalogService → verify availability + decrement/increment available copies
//
// Named clients let the DI container manage HttpClient lifetime properly (avoiding
// socket exhaustion from creating new instances per request).
var serviceUrls = builder.Configuration.GetSection("ServiceUrls");

builder.Services.AddHttpClient("UserService", client =>
{
    var url = serviceUrls["UserService"]
        ?? throw new InvalidOperationException("ServiceUrls:UserService must be configured");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient("CatalogService", client =>
{
    var url = serviceUrls["CatalogService"]
        ?? throw new InvalidOperationException("ServiceUrls:CatalogService must be configured");
    client.BaseAddress = new Uri(url);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();

// Same VALIDATION_ERROR envelope as the other two services.
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

// ── Application services ──────────────────────────────────────────────────────
// Scoped: each depends (directly or via ReservationServiceContext) on per-request
// state, so a shared singleton instance would be unsafe across concurrent requests.
builder.Services.AddScoped<IUserServiceClient, UserServiceClient>();
builder.Services.AddScoped<ICatalogServiceClient, CatalogServiceClient>();
builder.Services.AddScoped<IWaitlistCascadeService, WaitlistCascadeService>();

// ── Route Ordering — Important ────────────────────────────────────────────────
// ASP.NET Core attribute routing resolves ambiguity between literal segments and
// route parameters by giving literal segments higher precedence. For example:
//
//   [HttpGet("history")]   → matches /api/reservations/history  (literal wins)
//   [HttpGet("waitlist")]  → matches /api/reservations/waitlist (literal wins)
//   [HttpGet("{id}/checkout")] → only matches when the first segment is NOT a literal route
//
// This means the routes in ReservationController work without manual ordering,
// BUT only if "history" and "waitlist" are registered as [HttpGet("...")] attributes
// on the controller — NOT as catch-all parameters like [HttpGet("{anything}")].
// The Controller file in Phase 4 will have comments pointing back here.
// ─────────────────────────────────────────────────────────────────────────────

// ── Background Service: Waitlist Expiry Job ───────────────────────────────────
// Runs on the interval configured by "WaitlistExpiryIntervalSeconds" (appsettings.json).
// A BackgroundService is a singleton hosted service; it creates its own DI scope per
// tick internally (see WaitlistExpiryJob) to safely use the scoped DbContext/services above.
builder.Services.AddHostedService<WaitlistExpiryJob>();

// ── Build & Middleware Pipeline ───────────────────────────────────────────────
var app = builder.Build();

// Same UsePathBase rationale as CatalogService — nginx mounts this process at
// /reservations/ without stripping the prefix.
if (!app.Environment.IsDevelopment())
    app.UsePathBase("/reservations");

// Swagger enabled in all environments (milestone-5 acceptance criterion).
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
    await DataSeeder.SeedAsync(dbContext);
}
else if (connectionString != "InMemory")
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
    await dbContext.Database.MigrateAsync();
}

// Order matters: Authentication extracts the identity; Authorization checks it.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", async (ReservationServiceContext db, IConfiguration config) =>
{
    var cs = config.GetConnectionString("ReservationDb");
    if (string.IsNullOrEmpty(cs) || cs == "InMemory")
        return Results.Ok(new { service = "ReservationService", database = "InMemory", status = "UP" });
    try
    {
        var migrations = await db.Database.GetAppliedMigrationsAsync();
        return Results.Ok(new { service = "ReservationService", database = "reservationservicedb", migrations = migrations.Count(), status = "UP" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { service = "ReservationService", status = "DOWN", error = ex.Message }, statusCode: 503);
    }
});

app.Run();

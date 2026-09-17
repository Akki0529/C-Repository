using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Data;
using UserService.Dtos;
using UserService.Services;
using UserService.Validators;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API Explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger setup — JWT "Authorize" button is deferred: Swashbuckle 10.x moved to
// Microsoft.OpenApi 2.x, which restructured the OpenApiSecurityScheme API surface,
// and getting the button working correctly needs its own verification pass rather
// than guessing at a type name that would silently misconfigure the security scheme.
// Every endpoint is still fully testable via curl/Postman with a manually pasted
// "Authorization: Bearer {token}" header in the meantime.
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "User Service API", Version = "v1" });
});

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ── FluentValidation validators ───────────────────────────────────────────────
// Registered explicitly (rather than assembly-scanning) so it's obvious at a glance
// which validator backs which request DTO.
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

// ── Database ──────────────────────────────────────────────────────────────────
// Sentinel value "InMemory" switches between development (in-process, no install needed)
// and production (real PostgreSQL). Switch by changing the connection string in config
// or via ASPNETCORE_ConnectionStrings__DefaultConnection env var.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString == "InMemory")
{
    builder.Services.AddDbContext<UserServiceContext>(o =>
        o.UseInMemoryDatabase("UserServiceDb"));
}
else
{
    builder.Services.AddDbContext<UserServiceContext>(o =>
        o.UseNpgsql(connectionString));
}

// ── JWT Authentication ────────────────────────────────────────────────────────
// UserService issues tokens; ReservationService validates them independently using
// the same secret. This avoids a network call to UserService on every protected request.
// Both services read "Jwt:Secret" from their own appsettings so the value must match.
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
        // ClockSkew = Zero: tokens expire at exactly the declared time, not a few minutes later.
        // The default 5-minute tolerance is a legacy safety net we don't need with short-lived tokens.
        ClockSkew = TimeSpan.Zero
    };

    // By default, a missing/invalid token produces an empty 401/403 body with just a
    // WWW-Authenticate header. api-contracts.md's "Common Error Responses" section
    // requires the {error, message, timestamp} JSON envelope on every endpoint, so we
    // intercept both outcomes here and write that shape ourselves.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Skip the default challenge logic (which would write its own WWW-Authenticate-only response).
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var body = new ApiErrorResponse("UNAUTHORIZED", "Authentication required");
            await context.Response.WriteAsync(JsonSerializer.Serialize(body,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var body = new ApiErrorResponse("FORBIDDEN", "You do not have permission to access this resource");
            await context.Response.WriteAsync(JsonSerializer.Serialize(body,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    };
});

builder.Services.AddAuthorization();

// ── HTTP Client: Reservation Service ─────────────────────────────────────────
// UserService only calls ReservationService from the profile endpoint, to fetch
// the user's activeReservations and borrowingHistory counts.
builder.Services.AddHttpClient("ReservationService", client =>
{
    var baseUrl = builder.Configuration["ServiceUrls:ReservationService"]
        ?? throw new InvalidOperationException("ServiceUrls:ReservationService must be configured");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── FluentValidation ──────────────────────────────────────────────────────────
// Auto-validation runs validators registered in DI before the action method executes.
// On failure, ASP.NET Core's default is a ValidationProblemDetails body — a different
// shape from the {error, message, timestamp} envelope api-contracts.md requires for
// VALIDATION_ERROR, so InvalidModelStateResponseFactory below rewrites it to match.
builder.Services.AddFluentValidationAutoValidation();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // Concatenate every field error into one readable message; the contract's
        // error envelope has a single "message" string, not a per-field error list.
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

    // Run the seeder after the app is built but before it starts accepting requests.
    // CreateScope is needed because DbContext is scoped (not singleton).
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
    await DataSeeder.SeedAsync(dbContext);
}

// Authentication must appear before Authorization in the pipeline; order matters here.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

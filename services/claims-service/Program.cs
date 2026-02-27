// =============================================================================
// ClaimsService - Application Entry Point
// =============================================================================
// This file configures and starts the ASP.NET Core Web API application.
// It sets up dependency injection, middleware pipeline, and routing.
//
// Architecture: Controller -> Service -> Repository
// Port: http://localhost:5115 (configured in Properties/launchSettings.json)
// =============================================================================

using ClaimsService.Data;
using ClaimsService.Repositories;
using ClaimsService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// Service Registration (Dependency Injection Container)
// =============================================================================
// Services registered here are available throughout the application via
// constructor injection. ASP.NET Core automatically resolves dependencies.
//
// Lifetime options:
//   - Singleton: One instance for application lifetime
//   - Scoped: One instance per HTTP request
//   - Transient: New instance every time it's requested
// =============================================================================

// AddControllers: Registers MVC controllers for handling HTTP requests.
// Controllers are discovered automatically from the Controllers/ directory.
builder.Services.AddControllers();

// AddOpenApi: Enables OpenAPI/Swagger documentation generation.
// Available at /openapi/v1.json in development environment.
builder.Services.AddOpenApi();

// RegisterDatabase context with PostgreSQL connection string
// Connection string is read from appsettings.json
// DefaultConnection property contains database configuration
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// -----------------------------------------------------------------------------
// Application Services Registration
// -----------------------------------------------------------------------------
// Register custom services with their interfaces for dependency injection.
// Using Scoped lifetime: one instance per HTTP request (recommended for services
// that depend on request-specific data or DbContext).
// -----------------------------------------------------------------------------
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();

// =============================================================================
// CORS (Cross-Origin Resource Sharing) Configuration
// =============================================================================
// CORS allows the frontend (running on a different port/domain) to call this API.
// Allowed origins are configured in appsettings.json under "Cors:AllowedOrigins".
// Default fallback: http://localhost:3000 (Next.js default port)
// =============================================================================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                     ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()      // GET, POST, PUT, DELETE, etc.
              .AllowAnyHeader()      // Content-Type, Authorization, etc.
              .AllowCredentials();   // Cookies, Authorization headers
    });
});

// Build the configured application
var app = builder.Build();

// =============================================================================
// Apply Database Migrations
// =============================================================================
// Automatically apply any pending EF Core migrations at application startup.
// This ensures the database schema is always up-to-date with the current code.
// Uses scoped service to get ClaimsDbContext instance.
// =============================================================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error applying database migrations");
        // Continue startup even if migrations fail - in production, you might want to fail
    }
}

// =============================================================================
// HTTP Request Pipeline (Middleware)
// =============================================================================
// Middleware processes HTTP requests in order. Each middleware can:
// - Handle the request and return a response
// - Pass the request to the next middleware
// - Modify the request/response
// Order matters: CORS must be before routing, Auth before endpoints, etc.
// =============================================================================

// OpenAPI documentation - only enabled in Development environment
// Access at: http://localhost:5115/openapi/v1.json
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS middleware - must be called before MapControllers
app.UseCors("AllowFrontend");

// Map controller routes - connects HTTP requests to controller actions
// Routes are defined by [Route] and [Http*] attributes on controllers
app.MapControllers();

// Start the application and listen for incoming HTTP requests
app.Run();

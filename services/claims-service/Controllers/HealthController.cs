// =============================================================================
// HealthController - Service Health Check Endpoint
// =============================================================================
// Provides a simple HTTP endpoint to verify the service is running and responsive.
// Used by: Load balancers, container orchestrators (Kubernetes, Docker), 
//          monitoring systems (Prometheus, Datadog), and deployment pipelines.
//
// Endpoint: GET /health
// Response: 200 OK with JSON payload containing service status and timestamp
// =============================================================================

using Microsoft.AspNetCore.Mvc;

namespace ClaimsService.Controllers;

/// <summary>
/// Health check controller for service monitoring and orchestration.
/// Returns current service status, name, and UTC timestamp.
/// </summary>
[ApiController]
[Route("[controller]")]  // Route: /health (controller name without "Controller" suffix)
public class HealthController : ControllerBase
{
    // ILogger<T> - ASP.NET Core's built-in logging abstraction.
    // Injected automatically by the DI container (registered by default).
    // Logs are written to configured outputs (console, file, Application Insights, etc.)
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// ASP.NET Core automatically provides the ILogger instance when creating this controller.
    /// </summary>
    /// <param name="logger">Logger instance for recording health check events</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// GET /health - Returns service health status.
    /// </summary>
    /// <returns>
    /// 200 OK with JSON body:
    /// {
    ///   "status": "healthy",
    ///   "service": "claims-service",
    ///   "timestamp": "2026-02-19T00:00:00.000Z"
    /// }
    /// </returns>
    /// <remarks>
    /// This endpoint should return quickly (under 100ms) and not perform
    /// expensive operations. For deep health checks (database, dependencies),
    /// use a separate /health/ready endpoint.
    /// </remarks>
    [HttpGet]  // HTTP GET method
    public IActionResult Get()
    {
        // Log at Information level - visible in Development, can be filtered in Production
        _logger.LogInformation("Health check requested at {Timestamp}", DateTime.UtcNow);
        
        // Return anonymous object - automatically serialized to JSON
        // IActionResult allows returning different status codes (Ok, BadRequest, etc.)
        return Ok(new
        {
            status = "healthy",
            service = "claims-service",
            timestamp = DateTime.UtcNow
        });
    }
}

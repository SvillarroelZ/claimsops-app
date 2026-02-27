// =============================================================================
// ClaimsController - Claims API Endpoints
// =============================================================================
// REST API controller for managing insurance claims.
// Follows RESTful conventions for resource operations.
//
// Base Route: /api/claims
//
// Endpoints:
//   POST   /api/claims      - Create a new claim
//   GET    /api/claims      - List all claims
//   GET    /api/claims/{id} - Get a specific claim by ID
//
// Response Codes:
//   200 OK         - Successful GET request
//   201 Created    - Successful POST request
//   400 BadRequest - Validation errors in request body
//   404 NotFound   - Claim with specified ID not found
//
// Dependencies:
//   - IClaimService: Business logic layer
//   - ILogger: Logging
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using ClaimsService.DTOs;
using ClaimsService.Services;

namespace ClaimsService.Controllers;

/// <summary>
/// API controller for claim operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]  // Route: /api/claims
public class ClaimsController : ControllerBase
{
    // Purpose: Handle HTTP requests for claim operations.
    // Input: HTTP request body/route data mapped to DTOs and IDs.
    // Output: HTTP responses (201/200/404/400) with JSON payloads.
    // Why this exists: Keep transport concerns in controller and delegate business logic to service layer.
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimsController> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="claimService">Service for claim business logic</param>
    /// <param name="logger">Logger for controller operations</param>
    public ClaimsController(IClaimService claimService, ILogger<ClaimsController> logger)
    {
        _claimService = claimService;
        _logger = logger;
    }

    // =========================================================================
    // POST /api/claims - Create a new claim
    // =========================================================================
    /// <summary>
    /// Creates a new insurance claim.
    /// </summary>
    /// <param name="request">Claim creation data</param>
    /// <returns>The created claim with generated ID and timestamp</returns>
    /// <response code="201">Claim created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClaimResponse>> CreateClaim([FromBody] CreateClaimRequest request)
    {
        // Purpose: Create a new claim from API input.
        // Input: CreateClaimRequest with memberId, amount, and currency.
        // Output: 201 Created + ClaimResponse + Location header.
        // Why this exists: Provide a REST endpoint for claim creation while keeping flow explicit and testable.
        // Model validation is handled automatically by ASP.NET Core
        // If validation fails, a 400 BadRequest is returned with validation errors

        _logger.LogInformation("POST /api/claims - Creating claim for member: {MemberId}", request.MemberId);

        var claim = await _claimService.CreateClaimAsync(request);

        // Return 201 Created with Location header pointing to the new resource
        return CreatedAtAction(
            nameof(GetClaimById),           // Action name for Location header
            new { id = claim.Id },          // Route values
            claim                           // Response body
        );
    }

    // =========================================================================
    // GET /api/claims - List all claims
    // =========================================================================
    /// <summary>
    /// Retrieves all insurance claims.
    /// </summary>
    /// <returns>List of all claims</returns>
    /// <response code="200">Returns the list of claims</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ClaimResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ClaimResponse>>> GetAllClaims()
    {
        _logger.LogInformation("GET /api/claims - Fetching all claims");

        var claims = await _claimService.GetAllClaimsAsync();
        return Ok(claims);
    }

    // =========================================================================
    // GET /api/claims/{id} - Get a specific claim
    // =========================================================================
    /// <summary>
    /// Retrieves a specific claim by its unique identifier.
    /// </summary>
    /// <param name="id">The claim's unique identifier (GUID)</param>
    /// <returns>The claim if found</returns>
    /// <response code="200">Returns the claim</response>
    /// <response code="404">Claim not found</response>
    [HttpGet("{id:guid}")]  // Route constraint: id must be a valid GUID
    [ProducesResponseType(typeof(ClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimResponse>> GetClaimById(Guid id)
    {
        _logger.LogInformation("GET /api/claims/{ClaimId} - Fetching claim", id);

        var claim = await _claimService.GetClaimByIdAsync(id);

        if (claim == null)
        {
            _logger.LogWarning("Claim not found: {ClaimId}", id);
            return NotFound(new { message = "Claim not found", claimId = id });
        }

        return Ok(claim);
    }
}

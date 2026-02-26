// =============================================================================
// ClaimService - Business Logic Implementation
// =============================================================================
// Implements claim business operations including validation, entity mapping,
// and orchestration of repository calls.
//
// Responsibilities:
//   - Map CreateClaimRequest DTO to Claim entity
//   - Map Claim entity to ClaimResponse DTO
//   - Set default values (Id, Status, CreatedAt)
//   - Log business operations
//   - (Future) Call audit-service to record events
//
// Dependencies:
//   - IClaimRepository: Data access
//   - ILogger: Logging
// =============================================================================

using ClaimsService.DTOs;
using ClaimsService.Models;
using ClaimsService.Repositories;

namespace ClaimsService.Services;

/// <summary>
/// Service implementation for claim business operations.
/// </summary>
public class ClaimService : IClaimService
{
    private readonly IClaimRepository _repository;
    private readonly ILogger<ClaimService> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="repository">Repository for data access</param>
    /// <param name="logger">Logger for service operations</param>
    public ClaimService(IClaimRepository repository, ILogger<ClaimService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all claims and maps them to response DTOs.
    /// </summary>
    /// <returns>Collection of claim responses</returns>
    public async Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync()
    {
        _logger.LogInformation("Fetching all claims");
        
        var claims = await _repository.GetAllAsync();
        return claims.Select(MapToResponse);
    }

    /// <summary>
    /// Retrieves a single claim by ID.
    /// </summary>
    /// <param name="id">Claim identifier</param>
    /// <returns>Claim response if found, null otherwise</returns>
    public async Task<ClaimResponse?> GetClaimByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching claim: {ClaimId}", id);
        
        var claim = await _repository.GetByIdAsync(id);
        return claim != null ? MapToResponse(claim) : null;
    }

    /// <summary>
    /// Creates a new claim from the request DTO.
    /// Sets default values for Id, Status, and CreatedAt.
    /// </summary>
    /// <param name="request">Claim creation request</param>
    /// <returns>Created claim as response DTO</returns>
    public async Task<ClaimResponse> CreateClaimAsync(CreateClaimRequest request)
    {
        _logger.LogInformation("Creating claim for member: {MemberId}, amount: {Amount} {Currency}", 
            request.MemberId, request.Amount, request.Currency);

        // Map DTO to entity and set default values
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = ClaimStatus.Draft,  // New claims start as Draft
            CreatedAt = DateTime.UtcNow
        };

        // Persist the claim
        var created = await _repository.CreateAsync(claim);
        
        _logger.LogInformation("Claim created successfully: {ClaimId}", created.Id);

        // TODO: Phase 7 - Call audit-service to record claim creation event

        return MapToResponse(created);
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Maps a Claim entity to a ClaimResponse DTO.
    /// Centralizes the mapping logic for consistent responses.
    /// </summary>
    /// <param name="claim">Domain entity to map</param>
    /// <returns>Response DTO</returns>
    private static ClaimResponse MapToResponse(Claim claim)
    {
        return new ClaimResponse
        {
            Id = claim.Id,
            MemberId = claim.MemberId,
            Status = claim.Status.ToString(),  // Enum to string for API readability
            Amount = claim.Amount,
            Currency = claim.Currency,
            CreatedAt = claim.CreatedAt
        };
    }
}

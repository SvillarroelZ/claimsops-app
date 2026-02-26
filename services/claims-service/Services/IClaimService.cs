// =============================================================================
// IClaimService - Business Logic Interface
// =============================================================================
// Defines the contract for claim business operations.
// This interface abstracts business logic from the controller layer.
//
// Pattern: Service Layer Pattern
// Purpose: Encapsulate business rules, validation, and orchestration
//
// Responsibilities:
//   - Validate business rules (not just data format)
//   - Orchestrate repository calls
//   - Map between DTOs and domain entities
//   - Trigger side effects (audit events, notifications)
//
// Methods:
//   GetAllClaimsAsync()     - Get all claims as DTOs
//   GetClaimByIdAsync(id)   - Get single claim by ID
//   CreateClaimAsync(dto)   - Create new claim from request DTO
// =============================================================================

using ClaimsService.DTOs;

namespace ClaimsService.Services;

/// <summary>
/// Service interface for claim business operations.
/// </summary>
public interface IClaimService
{
    /// <summary>
    /// Retrieves all claims.
    /// </summary>
    /// <returns>Collection of claim response DTOs</returns>
    Task<IEnumerable<ClaimResponse>> GetAllClaimsAsync();

    /// <summary>
    /// Retrieves a single claim by ID.
    /// </summary>
    /// <param name="id">Claim identifier</param>
    /// <returns>Claim response DTO if found, null otherwise</returns>
    Task<ClaimResponse?> GetClaimByIdAsync(Guid id);

    /// <summary>
    /// Creates a new claim.
    /// </summary>
    /// <param name="request">Claim creation request DTO</param>
    /// <returns>The created claim as response DTO</returns>
    Task<ClaimResponse> CreateClaimAsync(CreateClaimRequest request);
}

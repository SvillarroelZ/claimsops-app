// =============================================================================
// IClaimRepository - Data Access Interface
// =============================================================================
// Defines the contract for claim data persistence operations.
// This interface abstracts the data access layer, allowing different
// implementations (in-memory, Entity Framework, etc.) to be swapped.
//
// Pattern: Repository Pattern
// Purpose: Isolate data access logic from business logic
//
// Methods:
//   GetAllAsync()      - Retrieve all claims
//   GetByIdAsync(id)   - Retrieve a specific claim by ID
//   CreateAsync(claim) - Persist a new claim
//
// Implementation:
//   - ClaimRepository (in-memory) - Phase 4
//   - ClaimRepository (EF Core)   - Phase 5
// =============================================================================

using ClaimsService.Models;

namespace ClaimsService.Repositories;

/// <summary>
/// Repository interface for claim data access operations.
/// </summary>
public interface IClaimRepository
{
    /// <summary>
    /// Retrieves all claims from the data store.
    /// </summary>
    /// <returns>Collection of all claims</returns>
    Task<IEnumerable<Claim>> GetAllAsync();

    /// <summary>
    /// Retrieves a single claim by its unique identifier.
    /// </summary>
    /// <param name="id">The claim's unique identifier</param>
    /// <returns>The claim if found, null otherwise</returns>
    Task<Claim?> GetByIdAsync(Guid id);

    /// <summary>
    /// Persists a new claim to the data store.
    /// </summary>
    /// <param name="claim">The claim entity to persist</param>
    /// <returns>The persisted claim with any generated values</returns>
    Task<Claim> CreateAsync(Claim claim);
}

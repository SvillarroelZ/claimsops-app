// =============================================================================
// ClaimRepository - In-Memory Implementation
// =============================================================================
// Temporary in-memory implementation of IClaimRepository for development.
// This will be replaced with Entity Framework Core in Phase 5.
//
// Note: Data is stored in a static dictionary, so it persists across requests
// but is lost when the application restarts.
//
// Thread Safety:
//   - Uses ConcurrentDictionary for thread-safe operations
//   - Safe for use in async web request handling
// =============================================================================

using System.Collections.Concurrent;
using ClaimsService.Models;

namespace ClaimsService.Repositories;

/// <summary>
/// In-memory repository implementation for development and testing.
/// Data is lost when the application restarts.
/// </summary>
public class ClaimRepository : IClaimRepository
{
    // Thread-safe dictionary to store claims in memory
    // Key: Claim ID (Guid), Value: Claim entity
    private static readonly ConcurrentDictionary<Guid, Claim> _claims = new();

    private readonly ILogger<ClaimRepository> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="logger">Logger for repository operations</param>
    public ClaimRepository(ILogger<ClaimRepository> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all claims from the in-memory store.
    /// </summary>
    /// <returns>All stored claims</returns>
    public Task<IEnumerable<Claim>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all claims. Count: {Count}", _claims.Count);
        return Task.FromResult<IEnumerable<Claim>>(_claims.Values.ToList());
    }

    /// <summary>
    /// Retrieves a claim by ID from the in-memory store.
    /// </summary>
    /// <param name="id">Claim identifier</param>
    /// <returns>Claim if found, null otherwise</returns>
    public Task<Claim?> GetByIdAsync(Guid id)
    {
        _claims.TryGetValue(id, out var claim);
        
        if (claim == null)
        {
            _logger.LogWarning("Claim not found: {ClaimId}", id);
        }
        else
        {
            _logger.LogInformation("Retrieved claim: {ClaimId}", id);
        }
        
        return Task.FromResult(claim);
    }

    /// <summary>
    /// Adds a new claim to the in-memory store.
    /// </summary>
    /// <param name="claim">Claim to persist</param>
    /// <returns>The persisted claim</returns>
    public Task<Claim> CreateAsync(Claim claim)
    {
        _claims[claim.Id] = claim;
        _logger.LogInformation("Created claim: {ClaimId} for member: {MemberId}", claim.Id, claim.MemberId);
        return Task.FromResult(claim);
    }
}

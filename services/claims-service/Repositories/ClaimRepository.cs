// =============================================================================
// ClaimRepository - Entity Framework Core Implementation
// =============================================================================
// Implements IClaimRepository using EF Core for PostgreSQL persistence.
// All data is stored in the PostgreSQL database defined in ClaimsDbContext.
//
// Note: Uses ClaimsDbContext for database access.
// Thread Safety: DbContext is scoped per request, thread-safe by default.
// =============================================================================

using ClaimsService.Data;
using ClaimsService.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Repositories;

/// <summary>
/// Repository implementation using Entity Framework Core.
/// Provides data access to PostgreSQL database via ClaimsDbContext.
/// </summary>
public class ClaimRepository : IClaimRepository
{
    private readonly ClaimsDbContext _context;
    private readonly ILogger<ClaimRepository> _logger;

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="context">EF Core database context</param>
    /// <param name="logger">Logger for repository operations</param>
    public ClaimRepository(ClaimsDbContext context, ILogger<ClaimRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all claims from the database.
    /// </summary>
    /// <returns>All stored claims</returns>
    public async Task<IEnumerable<Claim>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all claims from database");
        var claims = await _context.Claims.ToListAsync();
        _logger.LogInformation("Retrieved {Count} claims from database", claims.Count);
        return claims;
    }

    /// <summary>
    /// Retrieves a claim by ID from the database.
    /// </summary>
    /// <param name="id">Claim identifier</param>
    /// <returns>Claim if found, null otherwise</returns>
    public async Task<Claim?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving claim by ID: {ClaimId}", id);
        var claim = await _context.Claims.FirstOrDefaultAsync(c => c.Id == id);
        
        if (claim == null)
        {
            _logger.LogWarning("Claim not found: {ClaimId}", id);
        }
        else
        {
            _logger.LogInformation("Retrieved claim: {ClaimId}", id);
        }
        
        return claim;
    }

    /// <summary>
    /// Persists a new claim to the database.
    /// </summary>
    /// <param name="claim">Claim entity to persist</param>
    /// <returns>The persisted claim with generated values</returns>
    public async Task<Claim> CreateAsync(Claim claim)
    {
        _logger.LogInformation("Creating claim for member: {MemberId} with amount: {Amount}", 
            claim.MemberId, claim.Amount);
        
        _context.Claims.Add(claim);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Successfully persisted claim: {ClaimId}", claim.Id);
        return claim;
    }
}

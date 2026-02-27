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
//   - Call audit-service to record events
//
// Dependencies:
//   - IClaimRepository: Data access
//   - ILogger: Logging
//   - IHttpClientFactory: HTTP calls to audit-service
//   - IConfiguration: Access to application settings
// =============================================================================

using System.Text;
using System.Text.Json;
using ClaimsService.DTOs;
using ClaimsService.Models;
using ClaimsService.Repositories;

namespace ClaimsService.Services;

/// <summary>
/// Service implementation for claim business operations.
/// </summary>
public class ClaimService : IClaimService
{
    // Purpose: Execute claim business logic and orchestrate persistence + audit integration.
    // Input: Claim requests from controller and IDs for read operations.
    // Output: ClaimResponse DTOs and side-effect call to audit-service.
    // Why this exists: Centralize domain flow so controller and repository stay simple and focused.
    private readonly IClaimRepository _repository;
    private readonly ILogger<ClaimService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="repository">Repository for data access</param>
    /// <param name="logger">Logger for service operations</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients</param>
    /// <param name="configuration">Application configuration</param>
    public ClaimService(
        IClaimRepository repository, 
        ILogger<ClaimService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _repository = repository;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
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
        // Purpose: Create and persist a claim, then emit an audit event.
        // Input: CreateClaimRequest from API layer.
        // Output: ClaimResponse representing the persisted claim.
        // Why this exists: This is the main use case path for POST /api/claims.
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

        // Call audit-service to record claim creation event
        await RecordAuditEventAsync(created.Id, "created", "system", $"Claim created for member {created.MemberId}");

        return MapToResponse(created);
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Calls audit-service to record an audit event.
    /// If the call fails, logs a warning but does not throw exception.
    /// This ensures claim creation succeeds even if audit service is unavailable.
    /// </summary>
    /// <param name="claimId">ID of the claim</param>
    /// <param name="eventType">Type of event (created, updated, etc.)</param>
    /// <param name="userId">User who triggered the event</param>
    /// <param name="details">Additional details about the event</param>
    private async Task RecordAuditEventAsync(Guid claimId, string eventType, string userId, string details)
    {
        // Purpose: Send a claim-related audit event to audit-service.
        // Input: Claim ID + event metadata (event type, user, details).
        // Output: HTTP POST call to /audit with logging for success/failure.
        // Why this exists: Keep an auditable trace without coupling claim creation to audit availability.
        try
        {
            var auditServiceUrl = _configuration["AuditService:BaseUrl"];
            if (string.IsNullOrEmpty(auditServiceUrl))
            {
                _logger.LogWarning("AuditService:BaseUrl not configured. Skipping audit event.");
                return;
            }

            var client = _httpClientFactory.CreateClient();
            var auditEvent = new
            {
                claim_id = claimId.ToString(),
                event_type = eventType,
                user_id = userId,
                details = details
            };

            var json = JsonSerializer.Serialize(auditEvent);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending audit event to {Url}/audit", auditServiceUrl);
            
            var response = await client.PostAsync($"{auditServiceUrl}/audit", content);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Audit event recorded successfully for claim {ClaimId}", claimId);
            }
            else
            {
                _logger.LogWarning("Failed to record audit event. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't throw - audit failures shouldn't break claim creation
            _logger.LogWarning(ex, "Error calling audit-service for claim {ClaimId}. Continuing without audit.", claimId);
        }
    }

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

// =============================================================================
// ClaimResponse - Data Transfer Object for Claim Responses
// =============================================================================
// Defines the structure of claim data returned by the API.
// This DTO controls what information is exposed to API consumers.
//
// Used by:
//   - GET /api/claims (list of claims)
//   - GET /api/claims/{id} (single claim)
//   - POST /api/claims (newly created claim)
//
// Example Response:
//   {
//     "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
//     "memberId": "MBR-12345",
//     "status": "Draft",
//     "amount": 150.00,
//     "currency": "USD",
//     "createdAt": "2026-02-19T00:00:00Z"
//   }
// =============================================================================

namespace ClaimsService.DTOs;

/// <summary>
/// Response payload containing claim information.
/// </summary>
public class ClaimResponse
{
    /// <summary>
    /// Unique identifier of the claim.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the member who owns this claim.
    /// </summary>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the claim as a string.
    /// Possible values: Draft, Submitted, Approved, Rejected
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Claim amount in the specified currency.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code.
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the claim was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

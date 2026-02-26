// =============================================================================
// Claim - Domain Entity
// =============================================================================
// Represents an insurance claim in the system. This is the core domain model
// that maps to the 'claims' table in the database.
//
// Properties:
//   Id        - Unique identifier (GUID)
//   MemberId  - Reference to the member who submitted the claim
//   Status    - Current state of the claim (Draft, Submitted, Approved, Rejected)
//   Amount    - Monetary value of the claim
//   Currency  - Currency code (default: USD)
//   CreatedAt - UTC timestamp when the claim was created
//
// Relationships:
//   - A claim belongs to one member (MemberId)
//   - A claim can have multiple audit events (tracked in audit-service)
// =============================================================================

namespace ClaimsService.Models;

/// <summary>
/// Domain entity representing an insurance claim.
/// </summary>
public class Claim
{
    /// <summary>
    /// Unique identifier for the claim.
    /// Generated automatically when the claim is created.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the member who submitted this claim.
    /// References an external member/user system.
    /// </summary>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the claim in the processing workflow.
    /// See ClaimStatus enum for possible values.
    /// </summary>
    public ClaimStatus Status { get; set; }

    /// <summary>
    /// Monetary amount being claimed.
    /// Must be greater than zero.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code for the claim amount.
    /// Default is USD. Stored as ISO 4217 code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// UTC timestamp when the claim was created.
    /// Set automatically by the system.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

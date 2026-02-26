// =============================================================================
// ClaimStatus - Enumeration of Possible Claim States
// =============================================================================
// Defines the lifecycle states a claim can be in. Claims transition through
// these states as they are processed by the system.
//
// State Flow:
//   Draft -> Submitted -> Approved (or Rejected)
//
// Usage:
//   var claim = new Claim { Status = ClaimStatus.Draft };
//   claim.Status = ClaimStatus.Submitted;
// =============================================================================

namespace ClaimsService.Models;

/// <summary>
/// Represents the possible states of an insurance claim.
/// </summary>
public enum ClaimStatus
{
    /// <summary>
    /// Initial state. Claim is created but not yet submitted for review.
    /// Can be edited by the member.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Claim has been submitted for review. Cannot be edited by member.
    /// Awaiting processing by claims department.
    /// </summary>
    Submitted = 1,

    /// <summary>
    /// Claim has been reviewed and approved. Payment will be processed.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Claim has been reviewed and rejected. No payment will be made.
    /// Member may appeal or submit additional documentation.
    /// </summary>
    Rejected = 3
}

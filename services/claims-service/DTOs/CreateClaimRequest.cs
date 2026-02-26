// =============================================================================
// CreateClaimRequest - Data Transfer Object for Claim Creation
// =============================================================================
// Defines the expected payload when creating a new claim via POST /api/claims.
// This DTO separates the API contract from the internal domain model.
//
// Validation:
//   - MemberId: Required, non-empty string
//   - Amount: Required, must be greater than zero
//   - Currency: Optional, defaults to "USD" if not provided
//
// Example Request Body:
//   {
//     "memberId": "MBR-12345",
//     "amount": 150.00,
//     "currency": "USD"
//   }
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace ClaimsService.DTOs;

/// <summary>
/// Request payload for creating a new insurance claim.
/// </summary>
public class CreateClaimRequest
{
    /// <summary>
    /// Identifier of the member submitting the claim.
    /// Must be a valid member ID from the member system.
    /// </summary>
    /// <example>MBR-12345</example>
    [Required(ErrorMessage = "Member ID is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Member ID must be between 1 and 50 characters")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// Amount being claimed in the specified currency.
    /// Must be a positive value greater than zero.
    /// </summary>
    /// <example>150.00</example>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be between 0.01 and 1,000,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO 4217 currency code for the claim amount.
    /// Defaults to USD if not specified.
    /// </summary>
    /// <example>USD</example>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code")]
    public string Currency { get; set; } = "USD";
}

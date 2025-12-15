using System.ComponentModel.DataAnnotations;

namespace WexCorporatePayments.Application.DTOs;

/// <summary>
/// DTO for creating a purchase transaction.
/// </summary>
public class CreatePurchaseTransactionRequest
{
    [Required(ErrorMessage = "Description is required.")]
    [MaxLength(50, ErrorMessage = "Description cannot exceed 50 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction date is required.")]
    public DateTime TransactionDate { get; set; }

    [Required(ErrorMessage = "Amount in USD is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount in USD must be greater than zero.")]
    public decimal AmountUsd { get; set; }
}

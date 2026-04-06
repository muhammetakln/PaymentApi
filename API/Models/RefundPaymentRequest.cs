using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public record RefundPaymentRequest
    {
        [Required]
        public string OrderNumber { get; init; } = null!;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; init; }

        [Required]
        public string Currency { get; init; } = "TRY";

        public string? Reason { get; init; }
    }
}
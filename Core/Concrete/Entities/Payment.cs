using Core.Abstract.Bases;
using Core.Concrete.Enums;

namespace Core.Concrete.Entities
{
    public class Payment:BaseEntity
    {
        public string OrderNumber { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public virtual ICollection<PaymentLog> Logs { get; set; } = [];
        public DateTime UpdatedAt { get; set; }
    }
    public class PaymentLog:BaseEntity
    {
        public int PaymentId { get; set; }
        public virtual Payment? Payment { get; set; }
        public TransactionType Type { get; set; } = TransactionType.Auth;
        public string ResponseCode { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public string RawResponse { get; set; } = null!;

    }
}

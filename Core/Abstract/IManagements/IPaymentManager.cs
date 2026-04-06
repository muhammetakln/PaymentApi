using Core.Utils;

namespace Core.Abstract.IManagements
{
    public interface IPaymentManager
    {
        Task<IApiResponse> ProcessPaymentAsync(string orderNumber, decimal totalAmount, string currency);
        Task<IApiResponse> RefundPaymentAsync(string orderNumber, decimal amount, string currency);
    }
}

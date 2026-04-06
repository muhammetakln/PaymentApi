using Core.Abstract.IManagements;
using Core.Concrete.Entities;
using Core.Concrete.Enums;
using Core.Utils;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Managements
{
    public class PaymentManager : IPaymentManager
    {
        private readonly PaymentContext context;

        public PaymentManager(PaymentContext context)
        {
            this.context = context;
        }

        public async Task<IApiResponse> ProcessPaymentAsync(string orderNumber, decimal totalAmount, string currency)
        {
            try
            {
                // 1. Parametrelerden gelen verilerle yeni bir ödeme nesnesi hazırlıyoruz
                var payment = new Payment
                {
                    OrderNumber = orderNumber,
                    TotalAmount = totalAmount,
                    Currency = currency
                };

                // 2. Hazırlanan nesneyi Entity Framework üzerinden veritabanı bağlamına (context) ekliyoruz
                await context.Payments.AddAsync(payment);

                // 3. Eklenen verinin veritabanına fiziksel olarak yazılmasını sağlıyoruz
                await context.SaveChangesAsync();

                return ApiResponse<Payment>.Success();
            }
            catch (Exception ex)
            {
                return ApiResponse<Payment>.Failure([ex.Message]);
            }
        }

        public async Task<IApiResponse> RefundPaymentAsync(string orderNumber, decimal amount, string currency)
        {
            try
            {
                // 1. İlgili ödemeyi sipariş numarasına göre veritabanında bul
                var payment = await context.Payments.FirstOrDefaultAsync(p => p.OrderNumber == orderNumber);

                // 2. Ödeme bulunamazsa Failure (başarısız) yanıtı dön
                if (payment == null)
                {
                    return ApiResponse<string>.Failure(["Belirtilen sipariş numarasına ait ödeme bulunamadı."], "İade işlemi başarısız.");
                }

                // 3. Ödeme durumunu iade edildi (Refunded) olarak güncelle
                payment.Status = PaymentStatus.Refunded;

                // 4. İade işlemi için bir log kaydı oluştur
                PaymentLog paymentLog = new()
                {
                    Payment = payment, // Navigation property kullanılarak Foreign Key ataması otomatik yapılır
                    Type = TransactionType.Refund,
                    ResponseCode = "00", // İşlem sonucunu temsil eden kod (temsili başarılı kod)
                    RawResponse = "İade işlemi sistem tarafından onaylandı."
                };

                // 5. Log kaydını veritabanı bağlamına ekle
                await context.PaymentLogs.AddAsync(paymentLog);

                // 6. Yapılan durum güncellemesi ve yeni log kaydını veritabanına fiziksel olarak yaz
                await context.SaveChangesAsync();

                // 7. Başarılı (Success) yanıtını IApiResponse interface'ini karşılayacak şekilde dön
                return ApiResponse<string>.Success("İade işlemi başarıyla tamamlandı.");
            }
            catch (Exception ex)
            {

                return ApiResponse<Payment>.Failure([ex.Message]);
            }
        }
    }
}

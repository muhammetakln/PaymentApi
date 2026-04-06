using Core.Abstract.IManagements;
using Core.Concrete.Entities;
using Core.Concrete.Enums;
using Core.Utils;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Business.Managements
{
    /// <summary>
    /// Ödeme işlemlerini yönetmek için kullanılan manager sınıfı
    /// </summary>
    public class PaymentManager : IPaymentManager
    {
        private readonly PaymentContext context;

        public PaymentManager(PaymentContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Ödemeyi işleme alır ve veritabanına kaydeder
        /// </summary>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="totalAmount">Ödeme tutarı</param>
        /// <param name="currency">Para birimi (TRY, USD, EUR vb.)</param>
        /// <returns>İşlem sonucu ve hata mesajları</returns>
        public async Task<IApiResponse> ProcessPaymentAsync(string orderNumber, decimal totalAmount, string currency)
        {
            try
            {
                var errors = new List<string>();

                // Giriş verilerini doğrula
                if (string.IsNullOrWhiteSpace(orderNumber))
                    errors.Add("Sipariş numarası gereklidir");

                if (totalAmount <= 0)
                    errors.Add("Ödeme tutarı 0'dan büyük olmalıdır");

                if (string.IsNullOrWhiteSpace(currency))
                    errors.Add("Para birimi gereklidir");

                // Hata varsa döndür
                if (errors.Any())
                    return ApiResponse<object>.Failure(errors);

                // Aynı sipariş için daha önce ödeme yapılmış mı kontrol et
                var existingPayment = await context.Payments
                    .FirstOrDefaultAsync(p => p.OrderNumber == orderNumber && p.Status == PaymentStatus.Completed);

                if (existingPayment != null)
                    return ApiResponse<object>.Failure(new[] { "Bu sipariş için ödeme zaten işlenmiştir" });

                // Yeni ödeme kaydı oluştur
                var payment = new Payment
                {
                    OrderNumber = orderNumber,
                    TotalAmount = totalAmount,
                    Currency = currency,
                    Status = PaymentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                context.Payments.Add(payment);
                await context.SaveChangesAsync();

                // Ödeme başlangıç logunu kaydet (Auth işlemi)
                var authLog = new PaymentLog
                {
                    PaymentId = payment.Id,
                    Type = TransactionType.Auth,
                    ResponseCode = "BEKLEMEDE",
                    RawResponse = $"Ödeme işlemi başlatıldı - Sipariş: {orderNumber}",
                    CreatedAt = DateTime.UtcNow
                };

                context.Add(authLog);
                await context.SaveChangesAsync();

                // Ödemeyi ödeme ağ geçidine gönder
                bool isPaymentSuccessful = await ProcessWithPaymentGateway(payment);

                if (isPaymentSuccessful)
                {
                    // Ödeme başarılıysa
                    payment.Status = PaymentStatus.Completed;
                    payment.UpdatedAt = DateTime.UtcNow;

                    // Başarı logunu kaydet (Capture işlemi)
                    var successLog = new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Type = TransactionType.Capture,
                        ResponseCode = "BAŞARILI",
                        RawResponse = $"Ödeme başarıyla işlendi - Sipariş: {orderNumber}",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Payments.Update(payment);
                    context.Add(successLog);
                    await context.SaveChangesAsync();

                    // Başarılı cevap verisi oluştur
                    var responseData = new
                    {
                        payment.Id,
                        payment.OrderNumber,
                        payment.TotalAmount,
                        payment.Currency,
                        payment.Status,
                        payment.UpdatedAt
                    };

                    return ApiResponse<object>.Success(
                        responseData,
                        "Ödeme başarıyla işlendi"
                    );
                }
                else
                {
                    // Ödeme başarısızsa
                    payment.Status = PaymentStatus.Failed;
                    payment.UpdatedAt = DateTime.UtcNow;

                    // Başarısızlık logunu kaydet
                    var failureLog = new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Type = TransactionType.Capture,
                        ResponseCode = "BAŞARISIZ",
                        ErrorMessage = "Ödeme ağ geçidi işlemi reddetti",
                        RawResponse = "Ödeme işlemi başarısız oldu",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Payments.Update(payment);
                    context.Add(failureLog);
                    await context.SaveChangesAsync();

                    return ApiResponse<object>.Failure(
                        new[] { "Ödeme işlemi başarısız oldu" }
                    );
                }
            }
            catch (DbUpdateException ex)
            {
                // Veritabanı hatası
                var errorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}";
                return ApiResponse<object>.Failure(new[] { errorMessage });
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return ApiResponse<object>.Failure(new[] { ex.Message });
            }
        }

        /// <summary>
        /// Daha önce yapılan ödemeyi geri alır (iade)
        /// </summary>
        /// <param name="orderNumber">Sipariş numarası</param>
        /// <param name="amount">İade edilecek tutar</param>
        /// <param name="currency">Para birimi</param>
        /// <returns>İşlem sonucu ve hata mesajları</returns>
        public async Task<IApiResponse> RefundPaymentAsync(string orderNumber, decimal amount, string currency)
        {
            try
            {
                var errors = new List<string>();

                // Giriş verilerini doğrula
                if (string.IsNullOrWhiteSpace(orderNumber))
                    errors.Add("Sipariş numarası gereklidir");

                if (amount <= 0)
                    errors.Add("İade tutarı 0'dan büyük olmalıdır");

                // Hata varsa döndür
                if (errors.Any())
                    return ApiResponse<object>.Failure(errors);

                // Ödemeyi loglarıyla birlikte getir
                var payment = await context.Payments
                    .Include(p => p.Logs)
                    .FirstOrDefaultAsync(p => p.OrderNumber == orderNumber && p.Status == PaymentStatus.Completed);

                // Ödeme bulunamazsa hata döndür
                if (payment == null)
                    return ApiResponse<object>.Failure(
                        new[] { "Ödeme bulunamadı veya tamamlanmadı" }
                    );

                // İade tutarı orijinal tutardan fazla olamaz
                if (amount > payment.TotalAmount)
                    return ApiResponse<object>.Failure(
                        new[] { "İade tutarı orijinal ödeme tutarından fazla olamaz" }
                    );

                // Daha önce iade yapılmış mı kontrol et
                var existingRefund = payment.Logs
                    .FirstOrDefault(l => l.Type == TransactionType.Refund);

                if (existingRefund != null)
                    return ApiResponse<object>.Failure(
                        new[] { "Bu ödeme zaten iade edilmiştir" }
                    );

                // İade işlemini ödeme ağ geçidine gönder
                bool isRefundSuccessful = await ProcessRefundWithPaymentGateway(payment, amount);

                if (isRefundSuccessful)
                {
                    // İade tutarına göre ödeme statüsünü güncelle
                    if (amount == payment.TotalAmount)
                    {
                        // Tam iade
                        payment.Status = PaymentStatus.Refunded;
                    }
                    else
                    {
                        // Kısmi iade
                        payment.Status = PaymentStatus.PartiallyRefunded;
                    }

                    payment.UpdatedAt = DateTime.UtcNow;

                    // İade logunu kaydet
                    var refundLog = new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Type = TransactionType.Refund,
                        ResponseCode = "BAŞARILI",
                        RawResponse = $"{amount} {currency} tutarında iade başarıyla işlendi",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Payments.Update(payment);
                    context.Add(refundLog);
                    await context.SaveChangesAsync();

                    // İade başarılı cevap verisi oluştur
                    var responseData = new
                    {
                        payment.Id,
                        payment.OrderNumber,
                        IadeTutari = amount,
                        payment.Currency,
                        payment.Status,
                        IadeTarihi = DateTime.UtcNow
                    };

                    return ApiResponse<object>.Success(
                        responseData,
                        "İade işlemi başarıyla tamamlandı"
                    );
                }
                else
                {
                    // İade başarısızsa başarısızlık logunu kaydet
                    var failureLog = new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Type = TransactionType.Refund,
                        ResponseCode = "BAŞARISIZ",
                        ErrorMessage = "İade ağ geçidi işlemi reddetti",
                        RawResponse = "İade işlemi başarısız oldu",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Add(failureLog);
                    await context.SaveChangesAsync();

                    return ApiResponse<object>.Failure(
                        new[] { "İade işlemi başarısız oldu" }
                    );
                }
            }
            catch (DbUpdateException ex)
            {
                // Veritabanı hatası
                var errorMessage = $"Veritabanı hatası: {ex.InnerException?.Message ?? ex.Message}";
                return ApiResponse<object>.Failure(new[] { errorMessage });
            }
            catch (Exception ex)
            {
                // Diğer hatalar
                return ApiResponse<object>.Failure(new[] { ex.Message });
            }
        }

        /// <summary>
        /// Ödemeyi ödeme ağ geçidine gönderir ve sonucu alır (Simülasyon)
        /// Gerçek implementasyonda Stripe, PayPal, 3D Secure gibi sistemler kullanılacak
        /// </summary>
        /// <param name="payment">Ödeme bilgisi</param>
        /// <returns>İşlem başarılı ise true, aksi takdirde false</returns>
        private async Task<bool> ProcessWithPaymentGateway(Payment payment)
        {
            try
            {
                // TODO: Gerçek ödeme ağ geçidi entegrasyonu yapılacak
                // (Stripe, PayPal, iyzipay, 3D Secure vb.)

                // Şimdilik simülasyon
                await Task.Delay(1000); // API çağrısını taklit et

                // %90 başarı oranı
                var random = new Random();
                return random.Next(100) < 90;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// İade işlemini ödeme ağ geçidine gönderir ve sonucu alır (Simülasyon)
        /// Gerçek implementasyonda Stripe, PayPal, 3D Secure gibi sistemler kullanılacak
        /// </summary>
        /// <param name="payment">Ödeme bilgisi</param>
        /// <param name="amount">İade tutarı</param>
        /// <returns>İşlem başarılı ise true, aksi takdirde false</returns>
        private async Task<bool> ProcessRefundWithPaymentGateway(Payment payment, decimal amount)
        {
            try
            {
                // TODO: Gerçek ödeme ağ geçidi iade entegrasyonu yapılacak
                // (Stripe, PayPal, iyzipay, 3D Secure vb.)

                // Şimdilik simülasyon
                await Task.Delay(1000); // API çağrısını taklit et

                // %95 başarı oranı
                var random = new Random();
                return random.Next(100) < 95;
            }
            catch
            {
                return false;
            }
        }
    }
}
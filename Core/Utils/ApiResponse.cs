using System.Text.Json.Serialization;

namespace Core.Utils
{
    /// <summary>
    /// Generic API yanıt sınıfı - T türünde veri döndürür
    /// </summary>
    public class ApiResponse<T> : IApiResponse
    {
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        [JsonPropertyName("message")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Message { get; set; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<string>? Errors { get; set; }

        [JsonPropertyName("responseTime")]
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// Constructor - Varsayılan değerleri ayarla
        /// </summary>
        public ApiResponse()
        {
            ResponseTime = DateTime.UtcNow;
            IsSuccess = false;
        }

        /// <summary>
        /// Veri ile başarılı yanıt oluştur
        /// </summary>
        public static ApiResponse<T> Success(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message ?? "İşlem başarıyla tamamlandı",
                ResponseTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Veri olmadan başarılı yanıt oluştur
        /// </summary>
        public static ApiResponse<T> Success(string? message = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = default,
                Message = message ?? "İşlem başarıyla tamamlandı",
                ResponseTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Hata mesajları ile başarısız yanıt oluştur - FIXED!
        /// </summary>
        public static ApiResponse<T> Failure(IEnumerable<string>? errors = null, string? message = null)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Data = default,
                Message = message ?? "İşlem başarısız oldu",
                Errors = errors ?? new List<string> { "Bir hata oluştu" },
                ResponseTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Tek hata mesajı ile başarısız yanıt oluştur
        /// </summary>
        public static ApiResponse<T> Failure(string error)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                Data = default,
                Message = "İşlem başarısız oldu",
                Errors = new List<string> { error },
                ResponseTime = DateTime.UtcNow
            };
        }
    }
}
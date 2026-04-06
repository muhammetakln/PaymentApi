namespace Core.Utils
{
    public class ApiResponse<T> : IApiResponse<T>
    {
        public T? Data { get ; set; }
        public bool IsSuccess { get ; set ; }
        public string? Message { get ; set ; }
        public IEnumerable<string>? Errors { get; set; }
        public DateTime ResponseTime { get; set; }
        public static ApiResponse<T> Success(T data, string? message = null) => new() { IsSuccess = true, Data = data, Message = message };
        public static ApiResponse<T> Success(string? message = null) => new() {IsSuccess=true,Message = message};
        public static ApiResponse<T> Failure(IEnumerable<string>? message = null) => new() { };
    }
}

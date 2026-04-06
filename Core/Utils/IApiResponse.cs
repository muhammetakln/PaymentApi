namespace Core.Utils
{
    /// <summary>
    /// API yanıt standardı - Interface
    /// </summary>
    public interface IApiResponse
    {
        bool IsSuccess { get; set; }
        string? Message { get; set; }
        IEnumerable<string>? Errors { get; set; }
        DateTime ResponseTime { get; set; }
    }
}
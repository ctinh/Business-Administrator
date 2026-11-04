namespace QTKD.Api.DTOs;

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public string? ErrorCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ServiceResult<T> Success(T data, string message = "success")
    {
        return new ServiceResult<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static ServiceResult<T> Failure(string code, string message)
    {
        return new ServiceResult<T>
        {
            IsSuccess = false,
            ErrorCode = code,
            Message = message,
            Data = default
        };
    }
}
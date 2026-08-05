namespace Contract.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; init; }

    public int StatusCode { get; init; }

    public T? Data { get; init; }

    public string? ErrorMessage { get; init; }

    private ServiceResult(
        bool isSuccess,
        int statusCode,
        T? data,
        string? errorMessage)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static ServiceResult<T> Success(T data)
    {
        return new ServiceResult<T>(
            true,
            200,
            data,
            null);
    }

    public static ServiceResult<T> Created(T data)
    {
        return new ServiceResult<T>(
            true,
            201,
            data,
            null);
    }

    public static ServiceResult<T> BadRequest(string message)
    {
        return new ServiceResult<T>(
            false,
            400,
            default,
            message);
    }

    public static ServiceResult<T> Unauthorized(string message)
    {
        return new ServiceResult<T>(
            false,
            401,
            default,
            message);
    }

    public static ServiceResult<T> Forbidden(string message)
    {
        return new ServiceResult<T>(
            false,
            403,
            default,
            message);
    }

    public static ServiceResult<T> NotFound(string message)
    {
        return new ServiceResult<T>(
            false,
            404,
            default,
            message);
    }

    public static ServiceResult<T> Conflict(string message)
    {
        return new ServiceResult<T>(
            false,
            409,
            default,
            message);
    }
}
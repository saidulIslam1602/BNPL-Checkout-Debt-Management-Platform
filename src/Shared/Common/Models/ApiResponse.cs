using System.Text.Json.Serialization;

namespace YourCompanyBNPL.Common.Models;

/// <summary>
/// Standard API response wrapper for consistent response format across all services
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    
    public T? Data { get; set; }
    
    public string? Message { get; set; }
    
    public List<string>? Errors { get; set; }
    
    public string? TraceId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public int StatusCode { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200
        };
    }

    public static ApiResponse<T> ErrorResult(string error, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };
    }

    public static ApiResponse<T> ErrorResult(List<string> errors, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Non-generic API response for operations that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };
    }

    public new static ApiResponse ErrorResult(string error, int statusCode = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Errors = new List<string> { error },
            StatusCode = statusCode
        };
    }

    public new static ApiResponse ErrorResult(List<string> errors, int statusCode = 400)
    {
        return new ApiResponse
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Paginated API response for list operations
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PagedApiResponse<T> : ApiResponse<List<T>>
{
    public int Page { get; set; }
    
    public int PageSize { get; set; }
    
    public int TotalCount { get; set; }
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    
    public bool HasNextPage => Page < TotalPages;
    
    public bool HasPreviousPage => Page > 1;

    public static PagedApiResponse<T> SuccessResult(
        List<T> data, 
        int totalCount, 
        int page, 
        int pageSize, 
        string? message = null)
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            StatusCode = 200
        };
    }

    public new static PagedApiResponse<T> ErrorResult(string error, int statusCode = 400)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            Errors = new List<string> { error },
            StatusCode = statusCode,
            Page = 1,
            PageSize = 0,
            TotalCount = 0
        };
    }

    public new static PagedApiResponse<T> ErrorResult(List<string> errors, int statusCode = 400)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            Errors = errors,
            StatusCode = statusCode,
            Page = 1,
            PageSize = 0,
            TotalCount = 0
        };
    }
}
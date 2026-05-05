namespace ApiResponses;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyCollection<string> Errors,
    PaginationMetadata? Pagination)
{
    public static ApiResponse<T> Ok(T? data, string message = "Request completed successfully.", PaginationMetadata? pagination = null)
    {
        return new ApiResponse<T>(true, message, data, Array.Empty<string>(), pagination);
    }

    public static ApiResponse<T> Fail(IReadOnlyCollection<string> errors, string message = "Request failed.")
    {
        return new ApiResponse<T>(false, message, default, errors, null);
    }
}

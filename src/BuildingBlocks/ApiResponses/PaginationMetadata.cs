namespace ApiResponses;

public sealed record PaginationMetadata(
    int PageNumber,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

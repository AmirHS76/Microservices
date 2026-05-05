namespace ApiResponses;

public sealed record PaginatedResult<T>(IReadOnlyCollection<T> Items, PaginationMetadata Pagination);

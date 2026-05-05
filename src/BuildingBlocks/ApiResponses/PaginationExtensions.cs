namespace ApiResponses;

public static class PaginationExtensions
{
    public static PaginatedResult<T> Paginate<T>(this IReadOnlyCollection<T> source, PaginationRequest request)
    {
        var totalItems = source.Count;
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)request.PageSize);
        var items = source
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();

        var metadata = new PaginationMetadata(
            request.PageNumber,
            request.PageSize,
            totalItems,
            totalPages,
            request.PageNumber > 1,
            request.PageNumber < totalPages);

        return new PaginatedResult<T>(items, metadata);
    }
}

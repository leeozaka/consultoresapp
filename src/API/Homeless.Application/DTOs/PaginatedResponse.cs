using System.Text.Json.Serialization;

namespace Homeless.Application.DTOs;

public sealed record PaginatedResponse<T>(
    [property: JsonPropertyName("items")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_size")] int PageSize,
    [property: JsonPropertyName("total_pages")] int TotalPages
)
{
    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize
    )
    {
        var p = Math.Max(1, page);
        var ps = Math.Max(1, pageSize);

        return new(items, totalCount, p, ps, (int)Math.Ceiling(totalCount / (double)ps));
    }

    public static PaginatedResponse<T> CreateFrom(IReadOnlyList<T> allItems, int page, int pageSize)
    {
        var p = Math.Max(1, page);
        var ps = Math.Max(1, pageSize);
        var total = allItems.Count;
        var skip = (long)(p - 1) * ps;
        IReadOnlyList<T> slice;
        if (skip >= total)
        {
            slice = Array.Empty<T>();
        }
        else
        {
            var take = Math.Min(ps, (int)(total - skip));
            var list = new List<T>(take);
            for (var i = 0; i < take; i++)
            {
                list.Add(allItems[(int)skip + i]);
            }

            slice = list;
        }

        return Create(slice, total, p, ps);
    }
}

using System.Text;
using Ardalis.Result;
using MediatR;
using Homeless.Application.DTOs;
using Homeless.Application.Interfaces;

namespace Homeless.Application.UseCases.Tenants;

public sealed class GetAllTenantsHandler(ITenantQueryService tenantQueryService)
    : IRequestHandler<GetAllTenantsQuery, Result<CursorPaginatedResponse<TenantResponse>>>
{
    public async Task<Result<CursorPaginatedResponse<TenantResponse>>> Handle(
        GetAllTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var (afterName, afterId) = DecodeCursor(request.After);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var (items, hasNextPage) = await tenantQueryService
            .GetPagedAsync(pageSize, afterName, afterId, cancellationToken)
            .ConfigureAwait(false);

        var nextCursor = hasNextPage && items.Count > 0
            ? EncodeCursor(items[^1].Name, items[^1].Id)
            : null;

        return Result.Success(new CursorPaginatedResponse<TenantResponse>(items, nextCursor, hasNextPage));
    }

    private static string EncodeCursor(string name, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{name}|{id}"));

    private static (string? Name, Guid? Id) DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return (null, null);

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var sep = decoded.LastIndexOf('|');
            if (sep < 0) return (null, null);

            var name = decoded[..sep];
            var idStr = decoded[(sep + 1)..];
            return Guid.TryParse(idStr, out var id) ? (name, id) : (null, null);
        }
        catch
        {
            return (null, null);
        }
    }
}

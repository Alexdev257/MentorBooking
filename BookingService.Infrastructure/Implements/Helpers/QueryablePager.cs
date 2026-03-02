using BookingService.Application.Interfaces.Helpers;
using Shared.Contracts.Common.Wrappers;
using Shared.Infrastructure.Extensions;

namespace BookingService.Infrastructure.Implements.Helpers;

public class QueryablePager : IQueryablePager
{
    public async Task<PaginationResponse<T>> ToPagedListAsync<T>(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return await source.ToPagedListAsync(pageNumber, pageSize, cancellationToken);
    }
}

using Shared.Contracts.Common.Wrappers;

namespace AuthService.Application.Interfaces.Helpers;

public interface IQueryablePager
{
    Task<PaginationResponse<T>> ToPagedListAsync<T>(IQueryable<T> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}

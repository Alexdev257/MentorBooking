using Microsoft.EntityFrameworkCore;
using SharedContracts.Common.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedInfrastructure.Extensions
{
    public static class QueryableExtensions
    {
        public static async Task<PaginationResponse<T>> ToPagedListAsync<T>
            (
                this IQueryable<T> source,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default
            )
        {
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var count = await source.CountAsync(cancellationToken);

            var item = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            return new PaginationResponse<T>
            {
                Items = item,
                TotalItems = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}

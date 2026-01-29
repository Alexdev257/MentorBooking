using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SharedKernel.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        IQueryable<T> GetAllAsync();
        IQueryable<T> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        void UpdateAsync(T entity);
        void DeleteAsync(T entity);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}

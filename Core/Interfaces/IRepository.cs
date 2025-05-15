using System.Linq;
using System.Linq.Expressions;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.Interfaces
{
    public interface IRepository<T> where T:BaseEntity
    {
        IQueryable<T> GetAll(Func<IQueryable<T>, IIncludableQueryable<T, object>>? includes = null);
        Task<IEnumerable<T>> GetAllAsync();
        T Find(Expression<Func<T, bool>> expression);
        Task<T> FindAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IIncludableQueryable<T, object>>? includes = null);
        IQueryable<T> FindAll(Func<IQueryable<T>, IQueryable<T>> func);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> expression);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entityList);
        Task<IEnumerable<T>> DeleteRangeAsync(IEnumerable<T> entityList);
        Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entityList);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}

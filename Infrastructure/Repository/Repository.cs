using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Infrastructure.Data;
using Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repository
{
    public class Repository<T>:IRepository<T> where T:BaseEntity
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets a collection of all objects in the database
        /// </summary>
        /// <remarks>Synchronous</remarks>
        public IQueryable<T> GetAll(Func<IQueryable<T>, IIncludableQueryable<T, object>>? includes = null)
        {
            var query = _context.Set<T>().AsQueryable();
            if (includes != null)
                return includes(query);
            return query;
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().AnyAsync(predicate);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Gets a collection of all objects in the database
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<IEnumerable<T>> GetAllAsync()=> await _context.Set<T>().ToListAsync();
        
        /// <summary>
        /// Returns a single object which matches the provided expression
        /// </summary>
        /// <remarks>Synchronous</remarks>
        public T Find(Expression<Func<T, bool>> expression)=> _context.Set<T>().SingleOrDefault(expression);

        /// <summary>
        /// Returns a single object which matches the provided expression
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<T> FindAsync(Expression<Func<T, bool>> expression, Func<IQueryable<T>, IIncludableQueryable<T, object>>? includes = null)
        {
            var query=_context.Set<T>().AsQueryable();
            if (includes != null)
                query = includes(query);
            return await query.FirstOrDefaultAsync(expression);
        }
        /// <summary>
        /// Returns a collection of objects which match the provided expression
        /// </summary>
        /// <remarks>Synchronous</remarks>
        public IQueryable<T> FindAll(Func<IQueryable<T>, IQueryable<T>> func) => func(_context.Set<T>().AsNoTracking());

        /// <summary>
        /// Returns a collection of objects which match the provided expression
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> expression)=> await _context.Set<T>().Where(expression).ToListAsync();

        /// <summary>
        /// Adds a single object to the database and commits the change
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<T> AddAsync(T entity)
        {
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Updates a single object and commits the change
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<T> UpdateAsync(T entity)
        {
            if (entity == null)
                return null;

            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        /// <summary>
        /// Deletes a single object from the database and commits the change
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<int> DeleteAsync(T entity)
        {
            try{
                _context.Set<T>().Remove(entity);
                return await _context.SaveChangesAsync();
            }
            catch(Exception ex){
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Adds a collection of objects into the database and commits the changes
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entityList)
        {
            _context.Set<T>().AddRange(entityList);
            await _context.SaveChangesAsync();
            return entityList;
        }

        /// <summary>
        /// Deletes a collection of objects into the database and commits the chфnges
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<IEnumerable<T>> DeleteRangeAsync(IEnumerable<T> entityList)   
        {
            try {
                _context.Set<T>().RemoveRange(entityList);
                await _context.SaveChangesAsync();
                return entityList;
            }
            catch (Exception ex){
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// Updates a collection of objects into the database and commits the changes
        /// </summary>
        /// <remarks>Asynchronous</remarks>
        public async Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entityList)
        {
            _context.Set<T>().UpdateRange(entityList);
            await _context.SaveChangesAsync();
            return entityList;
        }
    }
}

using System.Linq.Expressions;

namespace SalesInventoryAnalytics.Domain.Interfaces.Repositories
{
    // Repositorio genérico con operaciones CRUD comunes.

    /// <typeparam name="T">Entidad del dominio</typeparam>
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();

        Task<T?> GetByIdAsync(int id);

        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        Task AddRangeAsync(IEnumerable<T> entities);

        void Update(T entity);

        void Remove(T entity);

        void RemoveRange(IEnumerable<T> entities);

        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        //  Para verificar si existe alguna entidad que cumpla un criterio.
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    }
}


namespace SalesInventoryAnalytics.Domain.Interfaces.Repositories
{

    // Patrón Unit of Work para manejar transacciones.
    public interface IUnitOfWork : IDisposable
    {

       // Guarda todos los cambios pendientes en la base de datos.
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Inicia una transacción.
        Task BeginTransactionAsync();

        // Confirma la transacción actual.
        Task CommitTransactionAsync();

        // Para revertir la transacción actual.
        Task RollbackTransactionAsync();
    }
}

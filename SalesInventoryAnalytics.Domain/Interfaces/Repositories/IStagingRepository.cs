using SalesInventoryAnalytics.Domain.Entities.Staging;

namespace SalesInventoryAnalytics.Domain.Interfaces.Repositories
{
    // Repositorio para operaciones sobre tablas de Staging.
    public interface IStagingRepository
    {
        // Clientes
        Task<IEnumerable<StagingCustomer>> GetUnprocessedCustomersAsync();
        Task MarkCustomersAsProcessedAsync(IEnumerable<StagingCustomer> customers);

        // Productos
        Task<IEnumerable<StagingProduct>> GetUnprocessedProductsAsync();
        Task MarkProductsAsProcessedAsync(IEnumerable<StagingProduct> products);

        // Ventas
        Task<IEnumerable<StagingSale>> GetUnprocessedSalesAsync();
        Task MarkSalesAsProcessedAsync(IEnumerable<StagingSale> sales);
        Task UpdateSalesValidationAsync(IEnumerable<StagingSale> sales);
    }
}
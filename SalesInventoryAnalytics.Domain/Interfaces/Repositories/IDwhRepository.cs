using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;

namespace SalesInventoryAnalytics.Domain.Interfaces.Repositories
{
    // Repositorio para operaciones sobre el Data Warehouse.
    public interface IDwhRepository
    {
        // Clientes
        Task<DimCliente?> GetActiveClienteByCodigoAsync(string codigoCliente);
        Task AddClienteAsync(DimCliente cliente);
        Task UpdateClienteAsync(DimCliente cliente);
        Task<int> SaveChangesAsync();

        //Clientes pero carga masiva.
        Task<Dictionary<string, DimCliente>> GetAllActiveClientesAsync();

        // Productos
        Task<DimProducto?> GetActiveProductoByCodigoAsync(string codigoProducto);
        Task AddProductoAsync(DimProducto producto);
        Task UpdateProductoAsync(DimProducto producto);

        //Productos pero carga masiva.
        Task<Dictionary<string, DimProducto>> GetAllActiveProductosAsync();

        // Fechas
        Task<DimFecha?> GetFechaByIdAsync(int fechaId);

        //Fecha pero carga masiva.
        Task<Dictionary<int, DimFecha>> GetAllFechasAsync();

        // Ventas  con el Bulk Insert
        Task BulkInsertVentasAsync(IEnumerable<FactVentas> ventas);


    }
}
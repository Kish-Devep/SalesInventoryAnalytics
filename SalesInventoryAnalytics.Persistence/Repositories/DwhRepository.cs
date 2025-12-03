using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;
using SalesInventoryAnalytics.Persistence.Context;
using Z.EntityFramework.Extensions;

namespace SalesInventoryAnalytics.Persistence.Repositories
{
    public class DwhRepository : IDwhRepository
    {
        private readonly DwhContext _context;

        public DwhRepository(DwhContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Clientes
        public async Task<DimCliente?> GetActiveClienteByCodigoAsync(string codigoCliente)
        {
            return await _context.DimClientes
                .Where(x => x.CodigoCliente == codigoCliente && x.EsActivo)
                .FirstOrDefaultAsync();
        }

        public async Task AddClienteAsync(DimCliente cliente)
        {
            await _context.DimClientes.AddAsync(cliente);
        }

        public async Task UpdateClienteAsync(DimCliente cliente)
        {
            _context.DimClientes.Update(cliente);
            await Task.CompletedTask;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Preductos
        public async Task<DimProducto?> GetActiveProductoByCodigoAsync(string codigoProducto)
        {
            return await _context.DimProductos
                .Where(x => x.CodigoProducto == codigoProducto && x.EsActivo)
                .FirstOrDefaultAsync();
        }

        public async Task AddProductoAsync(DimProducto producto)
        {
            await _context.DimProductos.AddAsync(producto);
        }

        public async Task UpdateProductoAsync(DimProducto producto)
        {
            _context.DimProductos.Update(producto);
            await Task.CompletedTask;
        }

        // pa las fechas
        public async Task<DimFecha?> GetFechaByIdAsync(int fechaId)
        {
            return await _context.DimFechas
                .Where(x => x.FechaId == fechaId)
                .FirstOrDefaultAsync();
        }

        // Ventas
        public async Task BulkInsertVentasAsync(IEnumerable<FactVentas> ventas)
        {
            var ventasList = ventas.ToList();
            const int batchSize = 6000;

            // Insertar en lotes para mejor rendimiento
            for (int i = 0; i < ventasList.Count; i += batchSize)
            {
                var batch = ventasList.Skip(i).Take(batchSize).ToList();
                _context.AddRange(batch);
                await _context.SaveChangesAsync();
            }
        }

        // Carga masiva de mis tables de dimensiones.
        public async Task<Dictionary<string, DimCliente>> GetAllActiveClientesAsync()
        {
            return await _context.DimClientes
                .Where(x => x.EsActivo)
                .ToDictionaryAsync(x => x.CodigoCliente, x => x);
        }

        public async Task<Dictionary<string, DimProducto>> GetAllActiveProductosAsync()
        {
            return await _context.DimProductos
                .Where(x => x.EsActivo)
                .ToDictionaryAsync(x => x.CodigoProducto, x => x);
        }

        public async Task<Dictionary<int, DimFecha>> GetAllFechasAsync()
        {
            return await _context.DimFechas
                .ToDictionaryAsync(x => x.FechaId, x => x);
        }
    }
}
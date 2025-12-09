using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;
using SalesInventoryAnalytics.Persistence.Context;
using System.Data;

namespace SalesInventoryAnalytics.Persistence.Repositories
{
    public class DwhRepository : IDwhRepository
    {
        private readonly DwhContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DwhRepository> _logger;

        public DwhRepository(
            DwhContext context,
            IConfiguration configuration,
            ILogger<DwhRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // Productos
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

        // Fechas
        public async Task<DimFecha?> GetFechaByIdAsync(int fechaId)
        {
            return await _context.DimFechas
                .Where(x => x.FechaId == fechaId)
                .FirstOrDefaultAsync();
        }

        public async Task BulkInsertVentasAsync(IEnumerable<FactVentas> ventas)
        {
            var ventasList = ventas.ToList();

            if (!ventasList.Any())
            {
                _logger.LogWarning("No hay ventas para insertar");
                return;
            }

            var connectionString = _configuration.GetConnectionString("DwhConnection");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                var dataTable = new DataTable();

                // Crear columnas (sin VentaId porque es IDENTITY)
                dataTable.Columns.Add("ClienteId", typeof(int));
                dataTable.Columns.Add("ProductoId", typeof(int));
                dataTable.Columns.Add("FechaId", typeof(int));
                dataTable.Columns.Add("Cantidad", typeof(int));
                dataTable.Columns.Add("PrecioUnitario", typeof(decimal));
                dataTable.Columns.Add("TotalVenta", typeof(decimal));
                dataTable.Columns.Add("NumeroOrden", typeof(string));
                dataTable.Columns.Add("Estado", typeof(string));
                dataTable.Columns.Add("OrigenDatos", typeof(string));
                dataTable.Columns.Add("FechaCreacion", typeof(DateTime));

                // Llenar filas
                foreach (var venta in ventasList)
                {
                    dataTable.Rows.Add(
                        venta.ClienteId,
                        venta.ProductoId,
                        venta.FechaId,
                        venta.Cantidad,
                        venta.PrecioUnitario,
                        venta.TotalVenta,
                        venta.NumeroOrden,
                        venta.Estado,
                        venta.OrigenDatos,
                        venta.FechaCreacion
                    );
                }

                using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                {
                    DestinationTableName = "Fact_Ventas",
                    BatchSize = 10000,
                    BulkCopyTimeout = 600
                };

                // Mapear columnas
                bulkCopy.ColumnMappings.Add("ClienteId", "ClienteId");
                bulkCopy.ColumnMappings.Add("ProductoId", "ProductoId");
                bulkCopy.ColumnMappings.Add("FechaId", "FechaId");
                bulkCopy.ColumnMappings.Add("Cantidad", "Cantidad");
                bulkCopy.ColumnMappings.Add("PrecioUnitario", "PrecioUnitario");
                bulkCopy.ColumnMappings.Add("TotalVenta", "TotalVenta");
                bulkCopy.ColumnMappings.Add("NumeroOrden", "NumeroOrden");
                bulkCopy.ColumnMappings.Add("Estado", "Estado");
                bulkCopy.ColumnMappings.Add("OrigenDatos", "OrigenDatos");
                bulkCopy.ColumnMappings.Add("FechaCreacion", "FechaCreacion");

                await bulkCopy.WriteToServerAsync(dataTable);
                await transaction.CommitAsync();

                stopwatch.Stop();
                _logger.LogInformation(
                    " BulkInsert: {Count:N0} ventas en {Seconds:N2}s ({Rate:N0} reg/seg)",
                    ventasList.Count,
                    stopwatch.Elapsed.TotalSeconds,
                    ventasList.Count / stopwatch.Elapsed.TotalSeconds);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, " Error en BulkInsert de ventas");
                throw;
            }
        }

        // Carga masiva de dimensiones
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
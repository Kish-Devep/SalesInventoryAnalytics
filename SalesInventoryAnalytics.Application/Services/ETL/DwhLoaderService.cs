using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;

namespace SalesInventoryAnalytics.Application.Services.ETL
{
    // Servicio para cargar datos desde Staging al Data Warehouse.
    public class DwhLoaderService
    {
        private readonly IStagingRepository _stagingRepo;
        private readonly IDwhRepository _dwhRepo;
        private readonly ILogger<DwhLoaderService> _logger;

        public DwhLoaderService(
            IStagingRepository stagingRepo,
            IDwhRepository dwhRepo,
            ILogger<DwhLoaderService> logger)
        {
            _stagingRepo = stagingRepo ?? throw new ArgumentNullException(nameof(stagingRepo));
            _dwhRepo = dwhRepo ?? throw new ArgumentNullException(nameof(dwhRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Cargar clientes desde Staging a Dim_Cliente con SCD Type 2.
        public async Task<int> LoadDimClienteAsync()
        {
            _logger.LogInformation("* Cargando Dim_Cliente (SCD Type 2)...");

            var stagingClientes = await _stagingRepo.GetUnprocessedCustomersAsync();

            if (!stagingClientes.Any())
            {
                _logger.LogInformation("  * No hay clientes nuevos para procesar");
                return 0;
            }

            int insertados = 0;
            int actualizados = 0;

            foreach (var staging in stagingClientes)
            {
                var clienteExistente = await _dwhRepo.GetActiveClienteByCodigoAsync(staging.CodigoCliente);

                if (clienteExistente == null)
                {
                    var nuevoCliente = new DimCliente
                    {
                        CodigoCliente = staging.CodigoCliente,
                        Nombre = staging.Nombre,
                        Apellido = staging.Apellido,
                        Email = staging.Email,
                        Telefono = staging.Telefono,
                        Ciudad = staging.Ciudad,
                        Pais = staging.Pais,
                        Version = 1,
                        EsActivo = true,
                        FechaInicioValidez = DateTime.Now,
                        FechaFinValidez = null
                    };

                    await _dwhRepo.AddClienteAsync(nuevoCliente);
                    insertados++;
                }
                else
                {
                    bool cambio = clienteExistente.Nombre != staging.Nombre ||
                                  clienteExistente.Apellido != staging.Apellido ||
                                  clienteExistente.Email != staging.Email ||
                                  clienteExistente.Telefono != staging.Telefono ||
                                  clienteExistente.Ciudad != staging.Ciudad ||
                                  clienteExistente.Pais != staging.Pais;

                    if (cambio)
                    {
                        clienteExistente.EsActivo = false;
                        clienteExistente.FechaFinValidez = DateTime.Now;
                        clienteExistente.FechaActualizacion = DateTime.Now;
                        await _dwhRepo.UpdateClienteAsync(clienteExistente);

                        var nuevaVersion = new DimCliente
                        {
                            CodigoCliente = staging.CodigoCliente,
                            Nombre = staging.Nombre,
                            Apellido = staging.Apellido,
                            Email = staging.Email,
                            Telefono = staging.Telefono,
                            Ciudad = staging.Ciudad,
                            Pais = staging.Pais,
                            Version = clienteExistente.Version + 1,
                            EsActivo = true,
                            FechaInicioValidez = DateTime.Now,
                            FechaFinValidez = null
                        };

                        await _dwhRepo.AddClienteAsync(nuevaVersion);
                        actualizados++;
                    }
                }
            }

            await _dwhRepo.SaveChangesAsync();
            await _stagingRepo.MarkCustomersAsProcessedAsync(stagingClientes);

            _logger.LogInformation("  * Dim_Cliente: {Insertados} insertados, {Actualizados} actualizados",
                insertados, actualizados);

            return insertados + actualizados;
        }

        // Cargar productos desde Staging a Dim_Producto con SCD Type 2.
        public async Task<int> LoadDimProductoAsync()
        {
            _logger.LogInformation("* Cargando Dim_Producto (SCD Type 2)...");

            var stagingProductos = await _stagingRepo.GetUnprocessedProductsAsync();

            if (!stagingProductos.Any())
            {
                _logger.LogInformation("  * No hay productos nuevos para procesar");
                return 0;
            }

            int insertados = 0;
            int actualizados = 0;

            foreach (var staging in stagingProductos)
            {
                var productoExistente = await _dwhRepo.GetActiveProductoByCodigoAsync(staging.CodigoProducto);

                if (productoExistente == null)
                {
                    var nuevoProducto = new DimProducto
                    {
                        CodigoProducto = staging.CodigoProducto,
                        NombreProducto = staging.NombreProducto,
                        Categoria = staging.Categoria ?? string.Empty,
                        PrecioUnitario = staging.PrecioUnitario ?? 0,
                        StockActual = staging.StockActual ?? 0,
                        Version = 1,
                        EsActivo = true,
                        FechaInicioValidez = DateTime.Now,
                        FechaFinValidez = null
                    };

                    await _dwhRepo.AddProductoAsync(nuevoProducto);
                    insertados++;
                }
                else
                {
                    bool cambio = productoExistente.NombreProducto != staging.NombreProducto ||
                                  productoExistente.Categoria != staging.Categoria ||
                                  productoExistente.PrecioUnitario != staging.PrecioUnitario ||
                                  productoExistente.StockActual != staging.StockActual;

                    if (cambio)
                    {
                        productoExistente.EsActivo = false;
                        productoExistente.FechaFinValidez = DateTime.Now;
                        productoExistente.FechaActualizacion = DateTime.Now;
                        await _dwhRepo.UpdateProductoAsync(productoExistente);

                        var nuevaVersion = new DimProducto
                        {
                            CodigoProducto = staging.CodigoProducto,
                            NombreProducto = staging.NombreProducto,
                            Categoria = staging.Categoria ?? string.Empty,
                            PrecioUnitario = staging.PrecioUnitario ?? 0,
                            StockActual = staging.StockActual ?? 0,
                            Version = productoExistente.Version + 1,
                            EsActivo = true,
                            FechaInicioValidez = DateTime.Now,
                            FechaFinValidez = null
                        };

                        await _dwhRepo.AddProductoAsync(nuevaVersion);
                        actualizados++;
                    }
                }
            }

            await _dwhRepo.SaveChangesAsync();
            await _stagingRepo.MarkProductsAsProcessedAsync(stagingProductos);

            _logger.LogInformation("  * Dim_Producto: {Insertados} insertados, {Actualizados} actualizados",
                insertados, actualizados);

            return insertados + actualizados;
        }

        // Carga ventas desde Staging a  mi tabla Fact_Ventas
        public async Task<int> LoadFactVentasAsync()
        {
            _logger.LogInformation("→ Cargando Fact_Ventas...");

            var stagingVentas = await _stagingRepo.GetUnprocessedSalesAsync();

            if (!stagingVentas.Any())
            {
                _logger.LogInformation("  * No hay ventas nuevas para procesar");
                return 0;
            }

            _logger.LogInformation("  * Cargando diccionarios en memoria...");

            var clientesDict = await _dwhRepo.GetAllActiveClientesAsync();
            var productosDict = await _dwhRepo.GetAllActiveProductosAsync();
            var fechasDict = await _dwhRepo.GetAllFechasAsync();

            _logger.LogInformation("  * Diccionarios cargados: {Clientes} clientes, {Productos} productos, {Fechas} fechas",
                clientesDict.Count, productosDict.Count, fechasDict.Count);

            var factVentas = new List<FactVentas>();
            var ventasInvalidas = new List<StagingSale>();
            int errores = 0;

            foreach (var staging in stagingVentas)
            {
                try
                {
                    if (!clientesDict.TryGetValue(staging.CodigoCliente, out var cliente))
                    {
                        _logger.LogWarning("  * Cliente no encontrado: {Codigo}", staging.CodigoCliente);
                        staging.ErrorValidacion = $"Cliente {staging.CodigoCliente} no existe";
                        staging.EsValido = false;
                        ventasInvalidas.Add(staging);
                        errores++;
                        continue;
                    }

                    if (!productosDict.TryGetValue(staging.CodigoProducto, out var producto))
                    {
                        _logger.LogWarning("  * Producto no encontrado: {Codigo}", staging.CodigoProducto);
                        staging.ErrorValidacion = $"Producto {staging.CodigoProducto} no existe";
                        staging.EsValido = false;
                        ventasInvalidas.Add(staging);
                        errores++;
                        continue;
                    }

                    if (!staging.FechaOrden.HasValue)
                    {
                        staging.ErrorValidacion = "Fecha de orden es nula";
                        staging.EsValido = false;
                        ventasInvalidas.Add(staging);
                        errores++;
                        continue;
                    }

                    int fechaId = int.Parse(staging.FechaOrden.Value.ToString("yyyyMMdd"));

                    if (!fechasDict.TryGetValue(fechaId, out var fecha))
                    {
                        _logger.LogWarning("  * Fecha no encontrada: {FechaId}", fechaId);
                        staging.ErrorValidacion = $"Fecha {fechaId} no existe";
                        staging.EsValido = false;
                        ventasInvalidas.Add(staging);
                        errores++;
                        continue;
                    }

                    var factVenta = new FactVentas
                    {
                        ClienteId = cliente.ClienteId,
                        ProductoId = producto.ProductoId,
                        FechaId = fechaId,
                        Cantidad = staging.Cantidad ?? 0,
                        PrecioUnitario = staging.PrecioUnitario ?? 0,
                        TotalVenta = staging.TotalVenta ?? 0,
                        NumeroOrden = staging.NumeroOrden,
                        Estado = staging.Estado,
                        OrigenDatos = staging.OrigenDatos
                    };

                    factVentas.Add(factVenta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "  * Error procesando venta {NumeroOrden}", staging.NumeroOrden);
                    staging.ErrorValidacion = ex.Message;
                    staging.EsValido = false;
                    ventasInvalidas.Add(staging);
                    errores++;
                }
            }

            if (factVentas.Any())
            {
                _logger.LogInformation("  * Ejecutando BulkInsert de {Count} ventas...", factVentas.Count);

                await _dwhRepo.BulkInsertVentasAsync(factVentas);

                _logger.LogInformation("  * Fact_Ventas: {Count} ventas cargadas", factVentas.Count);
            }

            await _stagingRepo.MarkSalesAsProcessedAsync(stagingVentas.Where(x => !ventasInvalidas.Contains(x)));

            if (ventasInvalidas.Any())
            {
                await _stagingRepo.UpdateSalesValidationAsync(ventasInvalidas);
            }

            if (errores > 0)
            {
                _logger.LogWarning("  * {Errores} ventas con errores (no se cargaron)", errores);
            }

            return factVentas.Count;
        }
    }
}
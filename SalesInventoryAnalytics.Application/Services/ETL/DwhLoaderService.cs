using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using SalesInventoryAnalytics.Domain.Interfaces.Repositories;

namespace SalesInventoryAnalytics.Application.Services.ETL
{
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

        public async Task<int> LoadDimClienteAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(" * Cargando Dim_Cliente (SCD Type 2)...");

            var stagingClientes = await _stagingRepo.GetUnprocessedCustomersAsync();

            if (!stagingClientes.Any())
            {
                _logger.LogInformation("  * No hay clientes nuevos para procesar");
                return 0;
            }

            var clientesExistentes = await _dwhRepo.GetAllActiveClientesAsync();
            _logger.LogInformation("  * {Count} clientes activos cargados en memoria", clientesExistentes.Count);

            int insertados = 0;
            int actualizados = 0;
            var clientesAActualizar = new List<DimCliente>();
            var clientesAInsertar = new List<DimCliente>();

            foreach (var staging in stagingClientes)
            {
                if (!clientesExistentes.TryGetValue(staging.CodigoCliente, out var clienteExistente))
                {
                    // Cliente nuevo
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

                    clientesAInsertar.Add(nuevoCliente);
                    insertados++;
                }
                else
                {
                    // Cliente existente - verificar cambios
                    bool cambio = clienteExistente.Nombre != staging.Nombre ||
                                  clienteExistente.Apellido != staging.Apellido ||
                                  clienteExistente.Email != staging.Email ||
                                  clienteExistente.Telefono != staging.Telefono ||
                                  clienteExistente.Ciudad != staging.Ciudad ||
                                  clienteExistente.Pais != staging.Pais;

                    if (cambio)
                    {
                        // Marcar versión anterior como inactiva
                        clienteExistente.EsActivo = false;
                        clienteExistente.FechaFinValidez = DateTime.Now;
                        clienteExistente.FechaActualizacion = DateTime.Now;
                        clientesAActualizar.Add(clienteExistente);

                        // Crear nueva versión
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

                        clientesAInsertar.Add(nuevaVersion);
                        actualizados++;
                    }
                }
            }

            if (clientesAActualizar.Any())
            {
                foreach (var cliente in clientesAActualizar)
                {
                    await _dwhRepo.UpdateClienteAsync(cliente);
                }
            }

            if (clientesAInsertar.Any())
            {
                foreach (var cliente in clientesAInsertar)
                {
                    await _dwhRepo.AddClienteAsync(cliente);
                }
            }

            if (clientesAActualizar.Any() || clientesAInsertar.Any())
            {
                await _dwhRepo.SaveChangesAsync();
            }

            await _stagingRepo.MarkCustomersAsProcessedAsync(stagingClientes);

            sw.Stop();
            _logger.LogInformation(" Dim_Cliente: {Insertados} insertados, {Actualizados} actualizados en {Time}ms",
                insertados, actualizados, sw.ElapsedMilliseconds);

            return insertados + actualizados;
        }

        public async Task<int> LoadDimProductoAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("* Cargando Dim_Producto (SCD Type 2)...");

            var stagingProductos = await _stagingRepo.GetUnprocessedProductsAsync();

            if (!stagingProductos.Any())
            {
                _logger.LogInformation("  * No hay productos nuevos para procesar");
                return 0;
            }

            var productosExistentes = await _dwhRepo.GetAllActiveProductosAsync();
            _logger.LogInformation("  * {Count} productos activos cargados en memoria", productosExistentes.Count);

            int insertados = 0;
            int actualizados = 0;
            var productosAActualizar = new List<DimProducto>();
            var productosAInsertar = new List<DimProducto>();

            foreach (var staging in stagingProductos)
            {
                if (!productosExistentes.TryGetValue(staging.CodigoProducto, out var productoExistente))
                {
                    // Producto nuevo
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

                    productosAInsertar.Add(nuevoProducto);
                    insertados++;
                }
                else
                {
                    // Producto existente - verificar cambios
                    bool cambio = productoExistente.NombreProducto != staging.NombreProducto ||
                                  productoExistente.Categoria != staging.Categoria ||
                                  productoExistente.PrecioUnitario != staging.PrecioUnitario ||
                                  productoExistente.StockActual != staging.StockActual;

                    if (cambio)
                    {
                        // Marcar versión anterior como inactiva
                        productoExistente.EsActivo = false;
                        productoExistente.FechaFinValidez = DateTime.Now;
                        productoExistente.FechaActualizacion = DateTime.Now;
                        productosAActualizar.Add(productoExistente);

                        // Crear nueva versión
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

                        productosAInsertar.Add(nuevaVersion);
                        actualizados++;
                    }
                }
            }

            if (productosAActualizar.Any())
            {
                foreach (var producto in productosAActualizar)
                {
                    await _dwhRepo.UpdateProductoAsync(producto);
                }
            }

            if (productosAInsertar.Any())
            {
                foreach (var producto in productosAInsertar)
                {
                    await _dwhRepo.AddProductoAsync(producto);
                }
            }

            if (productosAActualizar.Any() || productosAInsertar.Any())
            {
                await _dwhRepo.SaveChangesAsync();
            }

            await _stagingRepo.MarkProductsAsProcessedAsync(stagingProductos);

            sw.Stop();
            _logger.LogInformation("   Dim_Producto: {Insertados} insertados, {Actualizados} actualizados en {Time}ms",
                insertados, actualizados, sw.ElapsedMilliseconds);

            return insertados + actualizados;
        }

        public async Task<int> LoadFactVentasAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
                        staging.ErrorValidacion = $"Cliente {staging.CodigoCliente} no existe";
                        staging.EsValido = false;
                        ventasInvalidas.Add(staging);
                        errores++;
                        continue;
                    }

                    if (!productosDict.TryGetValue(staging.CodigoProducto, out var producto))
                    {
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
                        OrigenDatos = staging.OrigenDatos,
                        FechaCreacion = DateTime.Now
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

            sw.Stop();
            _logger.LogInformation("   Fact_Ventas: {Count} ventas cargadas en {Time}ms", factVentas.Count, sw.ElapsedMilliseconds);

            return factVentas.Count;
        }
    }
}
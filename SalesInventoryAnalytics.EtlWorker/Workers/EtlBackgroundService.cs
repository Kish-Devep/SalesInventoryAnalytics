using Microsoft.EntityFrameworkCore;
using SalesInventoryAnalytics.Application.Services.ETL;
using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;
using SalesInventoryAnalytics.Persistence.Context;

namespace SalesInventoryAnalytics.EtlWorker.Workers
{
    public class EtlBackgroundService : BackgroundService
    {
        private readonly ILogger<EtlBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public EtlBackgroundService(
            ILogger<EtlBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ETL Worker Service iniciado en: {time}", DateTimeOffset.Now);

            var intervalMinutes = _configuration.GetValue<int>("EtlSettings:IntervalMinutes", 60);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("╔════════════════════════════════════╗");
                    _logger.LogInformation("║   INICIANDO PROCESO ETL            ║");
                    _logger.LogInformation("╚════════════════════════════════════╝");

                    var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    using var scope = _serviceProvider.CreateScope();

                    await CleanupPhaseAsync(scope, stoppingToken);

                    await ExtractPhaseAsync(scope, stoppingToken);

                    await TransformPhaseAsync(scope, stoppingToken);

                    await LoadPhaseAsync(scope, stoppingToken);

                    totalStopwatch.Stop();

                    _logger.LogInformation("╔════════════════════════════════════╗");
                    _logger.LogInformation("║  ETL COMPLETADO: {Time:N2}s          ", totalStopwatch.Elapsed.TotalSeconds);
                    _logger.LogInformation("╚════════════════════════════════════╝");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crítico en el proceso ETL");
                }

                _logger.LogInformation("Próxima ejecución en {minutes} minutos", intervalMinutes);
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }

            _logger.LogInformation("ETL Worker Service detenido en: {time}", DateTimeOffset.Now);
        }

        private async Task CleanupPhaseAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            _logger.LogInformation("╔════════════════════════════════════╗");
            _logger.LogInformation("║   FASE 0: LIMPIEZA DE TABLAS       ║");
            _logger.LogInformation("╚════════════════════════════════════╝");

            var dwhContext = scope.ServiceProvider.GetRequiredService<DwhContext>();
            var stagingContext = scope.ServiceProvider.GetRequiredService<StagingContext>();

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                _logger.LogInformation(" Limpiando tablas DWH...");

                dwhContext.Database.SetCommandTimeout(180);
                stagingContext.Database.SetCommandTimeout(180);

                var ventasDeleted = await dwhContext.FactVentas.ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Fact_Ventas: {Count} registros eliminados", ventasDeleted);

                var productosDeleted = await dwhContext.DimProductos
                    .Where(x => x.EsActivo)
                    .ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Dim_Producto: {Count} registros eliminados", productosDeleted);

                var clientesDeleted = await dwhContext.DimClientes
                    .Where(x => x.EsActivo)
                    .ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Dim_Cliente: {Count} registros eliminados", clientesDeleted);

                _logger.LogInformation(" Limpiando tablas Staging...");

                var salesDeleted = await stagingContext.StagingSales.ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Staging_Sale: {Count} registros eliminados", salesDeleted);

                var productsDeleted = await stagingContext.StagingProducts.ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Staging_Product: {Count} registros eliminados", productsDeleted);

                var customersDeleted = await stagingContext.StagingCustomers.ExecuteDeleteAsync(cancellationToken);
                _logger.LogInformation("   Staging_Customer: {Count} registros eliminados", customersDeleted);

                sw.Stop();

                var totalDeleted = ventasDeleted + productosDeleted + clientesDeleted +
                                   salesDeleted + productsDeleted + customersDeleted;

                _logger.LogInformation("╔════════════════════════════════════╗");
                _logger.LogInformation("║  LIMPIEZA COMPLETADA: {Time:N2}s      ", sw.Elapsed.TotalSeconds);
                _logger.LogInformation("║  Total eliminados: {Total:N0}         ", totalDeleted);
                _logger.LogInformation("╚════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error en fase de limpieza");
                throw;
            }
        }

        private async Task ExtractPhaseAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            _logger.LogInformation("╔════════════════════════════════════╗");
            _logger.LogInformation("║   FASE 1: EXTRACCIÓN               ║");
            _logger.LogInformation("╚════════════════════════════════════╝");

            var csvBasePath = _configuration.GetValue<string>("EtlSettings:CsvBasePath")
                ?? throw new InvalidOperationException("CsvBasePath no configurado");

            var customerExtractor = scope.ServiceProvider.GetRequiredService<IExtractor<CustomerCsv>>();
            var productExtractor = scope.ServiceProvider.GetRequiredService<IExtractor<ProductCsv>>();
            var orderExtractor = scope.ServiceProvider.GetRequiredService<IExtractor<OrderCsv>>();
            var orderDetailExtractor = scope.ServiceProvider.GetRequiredService<IExtractor<OrderDetailCsv>>();

            var customerTransformer = scope.ServiceProvider.GetRequiredService<CustomerTransformerService>();
            var productTransformer = scope.ServiceProvider.GetRequiredService<ProductTransformerService>();
            var saleTransformer = scope.ServiceProvider.GetRequiredService<SaleTransformerService>();

            var stagingContext = scope.ServiceProvider.GetRequiredService<StagingContext>();

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                var customersCsv = await customerExtractor.ExtractAsync(Path.Combine(csvBasePath, "customers.csv"));
                var stagingCustomers = await customerTransformer.TransformAsync(customersCsv, "CSV");
                stagingContext.AddRange(stagingCustomers.Where(x => x.EsValido).ToList());
                await stagingContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("   Clientes cargados en Staging: {Count}", stagingCustomers.Count(x => x.EsValido));

                var productsCsv = await productExtractor.ExtractAsync(Path.Combine(csvBasePath, "products.csv"));
                var stagingProducts = await productTransformer.TransformAsync(productsCsv, "CSV");
                stagingContext.AddRange(stagingProducts.Where(x => x.EsValido).ToList());
                await stagingContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("   Productos cargados en Staging: {Count}", stagingProducts.Count(x => x.EsValido));

                var ordersCsv = await orderExtractor.ExtractAsync(Path.Combine(csvBasePath, "orders.csv"));
                var orderDetailsCsv = await orderDetailExtractor.ExtractAsync(Path.Combine(csvBasePath, "order_details.csv"));
                var stagingSales = await saleTransformer.TransformAsync(ordersCsv, orderDetailsCsv, "CSV");
                stagingContext.AddRange(stagingSales.Where(x => x.EsValido).ToList());
                await stagingContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("   Ventas cargadas en Staging: {Count}", stagingSales.Count(x => x.EsValido));

                sw.Stop();
                _logger.LogInformation("╔════════════════════════════════════╗");
                _logger.LogInformation("║  FASE 1 COMPLETADA: {Time:N2}s       ", sw.Elapsed.TotalSeconds);
                _logger.LogInformation("╚════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en fase de extracción");
                throw;
            }
        }

        private async Task TransformPhaseAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            _logger.LogInformation("╔════════════════════════════════════╗");
            _logger.LogInformation("║   FASE 2: TRANSFORMACIÓN           ║");
            _logger.LogInformation("╚════════════════════════════════════╝");

            var stagingContext = scope.ServiceProvider.GetRequiredService<StagingContext>();

            _logger.LogInformation("   Transformación completada");
            await Task.CompletedTask;
        }

        private async Task LoadPhaseAsync(IServiceScope scope, CancellationToken cancellationToken)
        {
            _logger.LogInformation("╔════════════════════════════════════╗");
            _logger.LogInformation("║   FASE 3: CARGA AL DWH             ║");
            _logger.LogInformation("╚════════════════════════════════════╝");

            var dwhLoader = scope.ServiceProvider.GetRequiredService<DwhLoaderService>();

            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                _logger.LogInformation("* Cargando dimensiones...");

                var clientesCargados = await dwhLoader.LoadDimClienteAsync();
                var productosCargados = await dwhLoader.LoadDimProductoAsync();

                _logger.LogInformation("  * Total dimensiones procesadas: {Total}",
                    clientesCargados + productosCargados);

                _logger.LogInformation("* Cargando hechos (ventas)...");

                var ventasCargadas = await dwhLoader.LoadFactVentasAsync();

                _logger.LogInformation("  * Total ventas cargadas: {Total}", ventasCargadas);

                sw.Stop();
                _logger.LogInformation("╔════════════════════════════════════╗");
                _logger.LogInformation("║  FASE 3 COMPLETADA: {Time:N2}s       ", sw.Elapsed.TotalSeconds);
                _logger.LogInformation("╚════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "* Error en fase de carga al DWH");
                throw;
            }
        }
    }
}
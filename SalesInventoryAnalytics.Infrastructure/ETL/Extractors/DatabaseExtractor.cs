using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;

namespace SalesInventoryAnalytics.Infrastructure.ETL.Extractors
{

    // Extractor que lee datos validados desde la base de datos Staging.
    // tambien para extraer datos de cualquier base de datos SQL Server.
    public class DatabaseExtractor : IExtractor<StagingSale>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseExtractor> _logger;

        public DatabaseExtractor(IConfiguration configuration, ILogger<DatabaseExtractor> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Extrae ventas validadas desde la base de datos Staging.
        /// <param name="source">Query personalizado o tabla (ej: "Staging_Sale" o condición WHERE)</param>
        public async Task<IEnumerable<StagingSale>> ExtractAsync(string source)
        {
            var connectionString = _configuration.GetConnectionString("StagingConnection")
                ?? throw new InvalidOperationException("StagingConnection no configurado");

            var sales = new List<StagingSale>();

            try
            {
                _logger.LogInformation("Extrayendo ventas desde base de datos Staging...");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        StagingSaleId,
                        NumeroOrden,
                        CodigoCliente,
                        CodigoProducto,
                        FechaOrden,
                        Cantidad,
                        PrecioUnitario,
                        TotalVenta,
                        Estado,
                        OrigenDatos,
                        EsValido,
                        Procesado
                    FROM Staging_Sale
                    WHERE EsValido = 1 
                      AND Procesado = 0
                    ORDER BY FechaOrden DESC";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    sales.Add(new StagingSale
                    {
                        StagingSaleId = reader.GetInt32("StagingSaleId"),
                        NumeroOrden = reader.GetString("NumeroOrden"),
                        CodigoCliente = reader.GetString("CodigoCliente"),
                        CodigoProducto = reader.GetString("CodigoProducto"),
                        FechaOrden = reader.IsDBNull("FechaOrden") ? null : reader.GetDateTime("FechaOrden"),
                        Cantidad = reader.IsDBNull("Cantidad") ? null : reader.GetInt32("Cantidad"),
                        PrecioUnitario = reader.IsDBNull("PrecioUnitario") ? null : reader.GetDecimal("PrecioUnitario"),
                        TotalVenta = reader.IsDBNull("TotalVenta") ? null : reader.GetDecimal("TotalVenta"),
                        Estado = reader.GetString("Estado"),
                        OrigenDatos = reader.GetString("OrigenDatos"),
                        EsValido = reader.GetBoolean("EsValido"),
                        Procesado = reader.GetBoolean("Procesado")
                    });
                }

                _logger.LogInformation("Extracción desde BD completada: {Count} ventas válidas", sales.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos desde base de datos Staging");
                throw;
            }

            return sales;
        }
    }
}
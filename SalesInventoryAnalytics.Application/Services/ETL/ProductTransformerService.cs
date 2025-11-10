using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using Microsoft.Extensions.Logging;

namespace SalesInventoryAnalytics.Application.Services.ETL
{
    // Transformar datos de productos desde CSV a Staging.
    public class ProductTransformerService
    {
        private readonly ILogger<ProductTransformerService> _logger;

        public ProductTransformerService(ILogger<ProductTransformerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Transformar ProductCsv a StagingProduct con validación.
        public async Task<List<StagingProduct>> TransformAsync(IEnumerable<ProductCsv> csvData, string origen)
        {
            var result = new List<StagingProduct>();

            foreach (var csv in csvData)
            {
                var staging = new StagingProduct
                {
                    CodigoProducto = csv.ProductID?.Trim() ?? string.Empty,
                    NombreProducto = csv.ProductName?.Trim() ?? string.Empty,
                    Categoria = csv.Category?.Trim() ?? string.Empty,
                    PrecioUnitario = ParseDecimal(csv.Price),
                    StockActual = ParseInt(csv.Stock),
                    OrigenDatos = origen,
                    FechaCreacion = DateTime.Now
                };

                var (esValido, error) = ValidateProduct(staging);
                staging.EsValido = esValido;
                staging.ErrorValidacion = error;

                result.Add(staging);
            }

            _logger.LogInformation("Transformados {Count} productos. Válidos: {Valid}, Inválidos: {Invalid}",
                result.Count,
                result.Count(x => x.EsValido),
                result.Count(x => !x.EsValido));

            return await Task.FromResult(result);
        }

        private (bool EsValido, string? Error) ValidateProduct(StagingProduct product)
        {
            if (string.IsNullOrWhiteSpace(product.CodigoProducto))
                return (false, "CodigoProducto es requerido");

            if (string.IsNullOrWhiteSpace(product.NombreProducto))
                return (false, "NombreProducto es requerido");

            if (product.PrecioUnitario == null || product.PrecioUnitario <= 0)
                return (false, "PrecioUnitario debe ser mayor a 0");

            if (product.StockActual == null || product.StockActual < 0)
                return (false, "StockActual no puede ser negativo");

            return (true, null);
        }

        private decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (decimal.TryParse(value, out var result))
                return result;

            return null;
        }

        private int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out var result))
                return result;

            return null;
        }
    }
}
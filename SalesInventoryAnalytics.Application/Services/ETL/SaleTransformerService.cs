using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using Microsoft.Extensions.Logging;

namespace SalesInventoryAnalytics.Application.Services.ETL
{
    // Transformar datos de ventas desde CSV a Staging.
    public class SaleTransformerService
    {
        private readonly ILogger<SaleTransformerService> _logger;

        public SaleTransformerService(ILogger<SaleTransformerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Transformar OrderCsv y OrderDetailCsv a StagingSale con validación.
        public async Task<List<StagingSale>> TransformAsync(
            IEnumerable<OrderCsv> orders,
            IEnumerable<OrderDetailCsv> orderDetails,
            string origen)
        {
            var result = new List<StagingSale>();

            var ordersList = orders.ToList();
            var detailsList = orderDetails.ToList();

            foreach (var order in ordersList)
            {
                var details = detailsList.Where(d => d.OrderID == order.OrderID);

                foreach (var detail in details)
                {
                    var staging = new StagingSale
                    {
                        NumeroOrden = order.OrderID?.Trim() ?? string.Empty,
                        CodigoCliente = order.CustomerID?.Trim() ?? string.Empty,
                        CodigoProducto = detail.ProductID?.Trim() ?? string.Empty,
                        FechaOrden = ParseDate(order.OrderDate),
                        Cantidad = ParseInt(detail.Quantity),
                        TotalVenta = ParseDecimal(detail.TotalPrice),
                        Estado = order.Status?.Trim() ?? string.Empty,
                        OrigenDatos = origen,
                        FechaCreacion = DateTime.Now
                    };

                    if (staging.TotalVenta.HasValue && staging.Cantidad.HasValue && staging.Cantidad > 0)
                    {
                        staging.PrecioUnitario = staging.TotalVenta.Value / staging.Cantidad.Value;
                    }

                    var (esValido, error) = ValidateSale(staging);
                    staging.EsValido = esValido;
                    staging.ErrorValidacion = error;

                    result.Add(staging);
                }
            }

            _logger.LogInformation("Transformadas {Count} ventas. Válidas: {Valid}, Inválidas: {Invalid}",
                result.Count,
                result.Count(x => x.EsValido),
                result.Count(x => !x.EsValido));

            return await Task.FromResult(result);
        }

        private (bool EsValido, string? Error) ValidateSale(StagingSale sale)
        {
            if (string.IsNullOrWhiteSpace(sale.NumeroOrden))
                return (false, "NumeroOrden es requerido");

            if (string.IsNullOrWhiteSpace(sale.CodigoCliente))
                return (false, "CodigoCliente es requerido");

            if (string.IsNullOrWhiteSpace(sale.CodigoProducto))
                return (false, "CodigoProducto es requerido");

            if (sale.FechaOrden == null)
                return (false, "FechaOrden es requerida");

            if (sale.Cantidad == null || sale.Cantidad <= 0)
                return (false, "Cantidad debe ser mayor a 0");

            if (sale.TotalVenta == null || sale.TotalVenta <= 0)
                return (false, "TotalVenta debe ser mayor a 0");

            return (true, null);
        }

        private DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, out var result))
                return result;

            return null;
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
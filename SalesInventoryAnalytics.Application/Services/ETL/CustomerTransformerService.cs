using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Entities.Staging;
using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Application.Interfaces;

namespace SalesInventoryAnalytics.Application.Services.ETL
{

    // Servicio para transformar datos de clientes desde CSV a Staging.
    public class CustomerTransformerService : ICustomerTransformerService
    {
        private readonly ILogger<CustomerTransformerService> _logger;

        public CustomerTransformerService(ILogger<CustomerTransformerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Transformar CustomerCsv a StagingCustomer con validación.
        public async Task<List<StagingCustomer>> TransformAsync(IEnumerable<CustomerCsv> csvData, string origen)
        {
            var result = new List<StagingCustomer>();

            foreach (var csv in csvData)
            {
                var staging = new StagingCustomer
                {
                    CodigoCliente = csv.CustomerID?.Trim() ?? string.Empty,
                    Nombre = csv.FirstName?.Trim() ?? string.Empty,
                    Apellido = csv.LastName?.Trim() ?? string.Empty,
                    Email = csv.Email?.Trim() ?? string.Empty,
                    Telefono = csv.Phone?.Trim() ?? string.Empty,
                    Ciudad = csv.City?.Trim() ?? string.Empty,
                    Pais = csv.Country?.Trim() ?? string.Empty,
                    OrigenDatos = origen,
                    FechaCreacion = DateTime.Now
                };

                var (esValido, error) = ValidateCustomer(staging);
                staging.EsValido = esValido;
                staging.ErrorValidacion = error;

                result.Add(staging);
            }

            _logger.LogInformation("Transformados {Count} clientes. Válidos: {Valid}, Inválidos: {Invalid}",
                result.Count,
                result.Count(x => x.EsValido),
                result.Count(x => !x.EsValido));

            return await Task.FromResult(result);
        }

        private (bool EsValido, string? Error) ValidateCustomer(StagingCustomer customer)
        {
            if (string.IsNullOrWhiteSpace(customer.CodigoCliente))
                return (false, "CodigoCliente es requerido");

            if (string.IsNullOrWhiteSpace(customer.Nombre))
                return (false, "Nombre es requerido");

            if (string.IsNullOrWhiteSpace(customer.Apellido))
                return (false, "Apellido es requerido");

            if (!string.IsNullOrWhiteSpace(customer.Email) && !customer.Email.Contains("@"))
                return (false, "Email inválido");

            return (true, null);
        }
    }
}
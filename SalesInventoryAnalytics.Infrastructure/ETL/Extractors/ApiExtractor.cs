using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;

namespace SalesInventoryAnalytics.Infrastructure.ETL.Extractors
{

    // Extractor genérico para APIs REST.
    public class ApiExtractor<T> : IExtractor<T> where T : class
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiExtractor<T>> _logger;

        public ApiExtractor(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ApiExtractor<T>> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // Extraer datos de una API REST.

        /// <param name="source">Endpoint de la API (ej: /api/products)</param>
        public async Task<IEnumerable<T>> ExtractAsync(string source)
        {
            var baseUrl = _configuration.GetValue<string>("ApiSettings:BaseUrl")
                ?? throw new InvalidOperationException("ApiSettings:BaseUrl no configurado");

            var apiKey = _configuration.GetValue<string>("ApiSettings:ApiKey");

            try
            {
                _logger.LogInformation("Extrayendo datos de API: {BaseUrl}{Source}", baseUrl, source);

                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);

                // Agregar API Key si existe
                if (!string.IsNullOrEmpty(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                }

                var response = await client.GetAsync(source);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<IEnumerable<T>>();

                _logger.LogInformation("Extracción de API completada: {Count} registros", data?.Count() ?? 0);

                return data ?? Enumerable.Empty<T>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error HTTP al consultar API: {Source}", source);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos de API: {Source}", source);
                throw;
            }
        }
    }
}
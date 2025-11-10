using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;
using Microsoft.Extensions.Logging;

namespace SalesInventoryAnalytics.Infrastructure.ETL.Extractors
{
    // Extractor genérico para archivos CSV  con el nugget CsvHelper.

    /// <typeparam name="T">Tipo de entidad a extraer del CSV</typeparam>
    public class CsvExtractor<T> : IExtractor<T> where T : class
    {
        private readonly ILogger<CsvExtractor<T>> _logger;

        public CsvExtractor(ILogger<CsvExtractor<T>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // para extrae datos de un archivo CSV.

        /// <param name="source">Ruta completa del archivo CSV</param>
        public async Task<IEnumerable<T>> ExtractAsync(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("La ruta del archivo CSV no puede estar vacía.", nameof(source));

            if (!File.Exists(source))
                throw new FileNotFoundException($"El archivo CSV no existe: {source}");

            try
            {
                _logger.LogInformation("Iniciando extracción de CSV: {FilePath}", source);

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    TrimOptions = TrimOptions.Trim
                };

                using var reader = new StreamReader(source);
                using var csv = new CsvReader(reader, config);

                var records = csv.GetRecords<T>().ToList();

                _logger.LogInformation("Extracción completada. Total de registros: {Count}", records.Count);

                return await Task.FromResult(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer datos del archivo CSV: {FilePath}", source);
                throw new InvalidOperationException($"Error al leer el archivo CSV: {source}", ex);
            }
        }
    }
}
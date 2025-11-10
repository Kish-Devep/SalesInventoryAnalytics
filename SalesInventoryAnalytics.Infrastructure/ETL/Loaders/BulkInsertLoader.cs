using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesInventoryAnalytics.Domain.Interfaces.ETL;

namespace SalesInventoryAnalytics.Infrastructure.ETL.Loaders
{

    // Cargador optimizado usando Bulk Insert
    /// <typeparam name="T">Tipo de entidad a cargar</typeparam>
    public class BulkInsertLoader<T> : ILoader<T> where T : class
    {
        private readonly DbContext _context;
        private readonly ILogger<BulkInsertLoader<T>> _logger;

        public BulkInsertLoader(DbContext context, ILogger<BulkInsertLoader<T>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // Carga datos con Bulk Insert
        public async Task<int> BulkInsertAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                _logger.LogWarning("No hay entidades para cargar con BulkInsert.");
                return 0;
            }

            try
            {
                var entityList = entities.ToList();
                _logger.LogInformation("Iniciando BulkInsert de {Count} registros de tipo {Type}",
                    entityList.Count, typeof(T).Name);

                await _context.BulkInsertAsync(entityList);

                _logger.LogInformation("BulkInsert completado exitosamente: {Count} registros", entityList.Count);

                return entityList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar BulkInsert de tipo {Type}", typeof(T).Name);
                throw;
            }
        }

        // Pa cargar datos uno por uno
        public async Task<int> LoadAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                _logger.LogWarning("No hay entidades para cargar.");
                return 0;
            }

            try
            {
                var entityList = entities.ToList();
                _logger.LogInformation("Iniciando carga estándar de {Count} registros de tipo {Type}",
                    entityList.Count, typeof(T).Name);

                await _context.Set<T>().AddRangeAsync(entityList);
                var rowsAffected = await _context.SaveChangesAsync();

                _logger.LogInformation("Carga completada: {Count} registros insertados", rowsAffected);

                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar entidades de tipo {Type}", typeof(T).Name);
                throw;
            }
        }
    }
}
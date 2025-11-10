using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Interfaces.ETL
{
    // Interfaz para cargadores de datos.

    /// <typeparam name="T">Tipo de entidad a cargar</typeparam>
    public interface ILoader<T> where T : class
    {
        Task<int> BulkInsertAsync(IEnumerable<T> entities);

        // Pa cargar mis datos 1 por 1 en dado caso.
        Task<int> LoadAsync(IEnumerable<T> entities);
    }
}

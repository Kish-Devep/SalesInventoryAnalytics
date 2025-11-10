using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Interfaces.ETL
{

    // Interfz para extraer los datos

    /// <typeparam name="T">Tipo de entidad a extraer</typeparam>
    public interface IExtractor<T> where T : class
    {
        /// <param name="source">Ruta o identificador de  lo que viene siendo la fuente</param>
        Task<IEnumerable<T>> ExtractAsync(string source);
    }
}

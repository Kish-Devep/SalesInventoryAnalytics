using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Interfaces.ETL
{

    // Interfaz para transformadores de datos.

    /// <typeparam name="TSource">Tipo de origen</typeparam>
    /// <typeparam name="TDestination">Tipo de destino</typeparam>
    public interface ITransformer<TSource, TDestination>
        where TSource : class
        where TDestination : class
    {
 
        // Transforma datos del tipo origen al tipo destino.
        Task<IEnumerable<TDestination>> TransformAsync(IEnumerable<TSource> sourceData);

        // Valida los datos transformados.
        Task<(IEnumerable<TDestination> Valid, IEnumerable<TDestination> Invalid)> ValidateAsync(
            IEnumerable<TDestination> data);
    }
}

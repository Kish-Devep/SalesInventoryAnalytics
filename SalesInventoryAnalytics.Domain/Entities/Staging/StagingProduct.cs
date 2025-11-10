using SalesInventoryAnalytics.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Entities.Staging
{
    // Staging table para productos.
    public class StagingProduct : BaseEntity
    {
        public int StagingProductId { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal? PrecioUnitario { get; set; }
        public int? StockActual { get; set; }

        // Campos de control ETL
        public string OrigenDatos { get; set; } = string.Empty;
        public bool EsValido { get; set; } = false;
        public string? ErrorValidacion { get; set; }
        public bool Procesado { get; set; } = false;
    }
}

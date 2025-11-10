using SalesInventoryAnalytics.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Entities.Staging
{
    // Staging table para ventas.
    public class StagingSale : BaseEntity
    {
        public int StagingSaleId { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public string CodigoCliente { get; set; } = string.Empty;
        public string CodigoProducto { get; set; } = string.Empty;
        public DateTime? FechaOrden { get; set; }
        public int? Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }
        public decimal? TotalVenta { get; set; }
        public string Estado { get; set; } = string.Empty;

        // Campos de control ETL
        public string OrigenDatos { get; set; } = string.Empty;
        public bool EsValido { get; set; } = false;
        public string? ErrorValidacion { get; set; }
        public bool Procesado { get; set; } = false;
    }
}

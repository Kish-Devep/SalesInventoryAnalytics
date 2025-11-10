using SalesInventoryAnalytics.Domain.Entities.Common;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions;

namespace SalesInventoryAnalytics.Domain.Entities.Dwh.Facts
{
    public class FactVentas : BaseFactEntity
    {
        public int VentaId { get; set; }

        // Fk's
        public int ClienteId { get; set; }
        public int ProductoId { get; set; }
        public int FechaId { get; set; }

        // Métricas
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal TotalVenta { get; set; }

        // Atributos degenerados
        public string NumeroOrden { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // Navigation property
        public virtual DimCliente Cliente { get; set; } = null!;
        public virtual DimProducto Producto { get; set; } = null!;
        public virtual DimFecha Fecha { get; set; } = null!;
    }
}

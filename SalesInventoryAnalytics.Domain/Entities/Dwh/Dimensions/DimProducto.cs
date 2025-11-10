using SalesInventoryAnalytics.Domain.Entities.Common;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;

namespace SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions
{
    public class DimProducto : BaseDimensionEntity
    {
        public int ProductoId { get; set; }
        public string CodigoProducto { get; set; } = string.Empty;
        public string NombreProducto { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int StockActual { get; set; }

        // Navigation property
        public virtual ICollection<FactVentas> Ventas { get; set; } = new List<FactVentas>();
    }
}

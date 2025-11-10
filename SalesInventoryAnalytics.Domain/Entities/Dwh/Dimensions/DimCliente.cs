using SalesInventoryAnalytics.Domain.Entities.Common;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;

namespace SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions
{
    public class DimCliente : BaseDimensionEntity
    {
        public int ClienteId { get; set; }
        public string CodigoCliente { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;

        // Navigation property
        public virtual ICollection<FactVentas> Ventas { get; set; } = new List<FactVentas>();
    }
}

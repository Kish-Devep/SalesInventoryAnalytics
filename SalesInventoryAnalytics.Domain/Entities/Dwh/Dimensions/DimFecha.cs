using SalesInventoryAnalytics.Domain.Entities.Common;
using SalesInventoryAnalytics.Domain.Entities.Dwh.Facts;

namespace SalesInventoryAnalytics.Domain.Entities.Dwh.Dimensions
{
    public class DimFecha : BaseEntity
    {
        public int FechaId { get; set; } 
        public DateTime Fecha { get; set; }
        public int Anio { get; set; }
        public int Trimestre { get; set; }
        public int Mes { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public int Dia { get; set; }
        public int DiaSemana { get; set; } // 1 = Lunes, 7 = Domingo
        public string NombreDia { get; set; } = string.Empty;
        public int NumeroSemana { get; set; }
        public bool EsFinDeSemana { get; set; }

        // Navigation property
        public virtual ICollection<FactVentas> Ventas { get; set; } = new List<FactVentas>();
    }
}

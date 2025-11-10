
namespace SalesInventoryAnalytics.Domain.Entities.Common
{
    public abstract class BaseDimensionEntity : BaseEntity
    {
        public int Version { get; set; } = 1;
        public bool EsActivo { get; set; } = true;
        public DateTime FechaInicioValidez { get; set; } = DateTime.Now;
        public DateTime? FechaFinValidez { get; set; }
    }
}

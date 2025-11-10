
namespace SalesInventoryAnalytics.Domain.Entities.Common
{
    public abstract class BaseEntity
    {
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaActualizacion { get; set; }
    }
}

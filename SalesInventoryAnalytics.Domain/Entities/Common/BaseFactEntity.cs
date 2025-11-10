
namespace SalesInventoryAnalytics.Domain.Entities.Common
{

    public abstract class BaseFactEntity : BaseEntity
    {
        public string OrigenDatos { get; set; } = string.Empty;
    }
}

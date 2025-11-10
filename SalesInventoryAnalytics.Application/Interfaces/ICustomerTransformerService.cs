using SalesInventoryAnalytics.Domain.Entities.SourceData.Csv;
using SalesInventoryAnalytics.Domain.Entities.Staging;

namespace SalesInventoryAnalytics.Application.Interfaces
{
    public interface ICustomerTransformerService
    {
        Task<List<StagingCustomer>> TransformAsync(IEnumerable<CustomerCsv> csvData, string origen);
    }
}
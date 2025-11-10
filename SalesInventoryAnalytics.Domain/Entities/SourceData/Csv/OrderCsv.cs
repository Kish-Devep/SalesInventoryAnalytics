using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Entities.SourceData.Csv
{
    public class OrderCsv
    {
        public string OrderID { get; set; } = string.Empty;
        public string CustomerID { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}

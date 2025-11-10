using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SalesInventoryAnalytics.Domain.Entities.SourceData.Csv
{
    public class OrderDetailCsv
    {
        public string OrderID { get; set; } = string.Empty;
        public string ProductID { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string TotalPrice { get; set; } = string.Empty;
    }
}

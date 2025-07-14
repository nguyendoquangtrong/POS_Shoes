// Areas/Marketing/Models/MarketingDashboardViewModel.cs
namespace POS_Shoes.Areas.Marketing.Models
{
    public class MarketingDashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public double TotalRevenue { get; set; }
        public double AverageOrderValue { get; set; }

        // Top selling products
        public List<TopProductItem> TopProducts { get; set; } = new List<TopProductItem>();

        // Customer analytics
        public List<CustomerSegmentItem> CustomerSegments { get; set; } = new List<CustomerSegmentItem>();

        // Sales trends
        public SalesTrendChart SalesTrend { get; set; } = new SalesTrendChart();

        // Customer acquisition
        public CustomerAcquisitionChart CustomerAcquisition { get; set; } = new CustomerAcquisitionChart();

        // Product performance
        public ProductPerformanceChart ProductPerformance { get; set; } = new ProductPerformanceChart();
    }

    public class TopProductItem
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
        public string Image { get; set; }
        public double GrowthRate { get; set; }
    }

    public class CustomerSegmentItem
    {
        public string SegmentName { get; set; }
        public int CustomerCount { get; set; }
        public double AverageSpending { get; set; }
        public double Percentage { get; set; }
    }

    public class SalesTrendChart
    {
        public List<string> Months { get; set; } = new List<string>();
        public List<double> Revenue { get; set; } = new List<double>();
        public List<int> OrderCounts { get; set; } = new List<int>();
        public List<int> CustomerCounts { get; set; } = new List<int>();
    }

    public class CustomerAcquisitionChart
    {
        public List<string> Weeks { get; set; } = new List<string>();
        public List<int> NewCustomers { get; set; } = new List<int>();
        public List<int> ReturningCustomers { get; set; } = new List<int>();
    }

    public class ProductPerformanceChart
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<int> UnitsSold { get; set; } = new List<int>();
        public List<double> Revenue { get; set; } = new List<double>();
    }
}

// Areas/Manager/Models/DashboardViewModel.cs
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Areas.Manager.Models
{
    public class DashboardViewModel
    {
        // Basic Statistics
        public int TotalProducts { get; set; }
        public int TotalSalers { get; set; }
        public int TotalAssignments { get; set; }
        public int TodayAssignments { get; set; }
        public int LowStockProducts { get; set; }
        public int PendingReports { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }

        // Recent Data
        public List<Assignment> RecentAssignments { get; set; } = new List<Assignment>();
        public List<Report> RecentReports { get; set; } = new List<Report>();
        public List<Order> RecentOrders { get; set; } = new List<Order>();

        // Chart Data
        public ProductStockChartData ProductStockChart { get; set; } = new ProductStockChartData();
        public AssignmentChartData AssignmentChart { get; set; } = new AssignmentChartData();
        public SalesChartData SalesChart { get; set; } = new SalesChartData();
        public UserActivityChartData UserActivityChart { get; set; } = new UserActivityChartData();
        public RevenueChartData RevenueChart { get; set; } = new RevenueChartData();
    }

    public class ProductStockChartData
    {
        public List<string> Categories { get; set; } = new List<string>();
        public List<int> StockQuantities { get; set; } = new List<int>();
        public List<int> LowStockCounts { get; set; } = new List<int>();
    }

    public class AssignmentChartData
    {
        public List<string> WeekDays { get; set; } = new List<string>();
        public List<int> AssignmentCounts { get; set; } = new List<int>();
        public List<string> SalerNames { get; set; } = new List<string>();
        public List<int> SalerAssignments { get; set; } = new List<int>();
    }

    public class SalesChartData
    {
        public List<string> Months { get; set; } = new List<string>();
        public List<decimal> Revenue { get; set; } = new List<decimal>();
        public List<int> OrderCounts { get; set; } = new List<int>();
    }

    public class UserActivityChartData
    {
        public List<string> SalerNames { get; set; } = new List<string>();
        public List<int> TotalAssigned { get; set; } = new List<int>();
        public List<int> CompletedTasks { get; set; } = new List<int>();
    }

    public class RevenueChartData
    {
        public List<string> Days { get; set; } = new List<string>();
        public List<decimal> DailyRevenue { get; set; } = new List<decimal>();
    }
}

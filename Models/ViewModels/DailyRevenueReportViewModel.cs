// Models/ViewModels/DailyRevenueReportViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.ViewModels
{
    public class DailyRevenueReportViewModel
    {
        [Required]
        [Display(Name = "Ngày báo cáo")]
        public DateTime ReportDate { get; set; } = DateTime.Today;

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // Thông tin doanh thu
        public double TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageOrderValue { get; set; }

        // Chi tiết theo sản phẩm
        public List<ProductRevenueItem> ProductRevenues { get; set; } = new List<ProductRevenueItem>();

        // Chi tiết theo khách hàng
        public List<CustomerRevenueItem> CustomerRevenues { get; set; } = new List<CustomerRevenueItem>();

        // Chi tiết theo giờ
        public List<HourlyRevenueItem> HourlyRevenues { get; set; } = new List<HourlyRevenueItem>();

        // Thống kê thanh toán
        public int CashOrders { get; set; }
        public int MemberOrders { get; set; }
        public int GuestOrders { get; set; }
    }

    public class ProductRevenueItem
    {
        public string ProductName { get; set; }
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
        public double Percentage { get; set; }
    }

    public class CustomerRevenueItem
    {
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int OrderCount { get; set; }
        public double TotalSpent { get; set; }
    }

    public class HourlyRevenueItem
    {
        public int Hour { get; set; }
        public int OrderCount { get; set; }
        public double Revenue { get; set; }
    }
}

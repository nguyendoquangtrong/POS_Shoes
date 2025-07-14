// Models/ViewModels/RevenueViewModels.cs
using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.ViewModels
{
    public class RevenueOverviewViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan doanh thu
        public double TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageOrderValue { get; set; }

        // Doanh thu theo nhân viên
        public List<StaffRevenueItem> StaffRevenues { get; set; } = new List<StaffRevenueItem>();

        // Doanh thu theo ngày
        public List<DailyRevenueItem> DailyRevenues { get; set; } = new List<DailyRevenueItem>();

        // Doanh thu theo sản phẩm
        public List<ProductRevenueItem> ProductRevenues { get; set; } = new List<ProductRevenueItem>();
    }

    public class StaffRevenueItem
    {
        public string StaffName { get; set; }
        public int OrderCount { get; set; }
        public double Revenue { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyRevenueItem
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public double Revenue { get; set; }
    }

    // public class ProductRevenueItem
    // {
    // public string ProductName { get; set; }
    // public int QuantitySold { get; set; }
    // public double Revenue { get; set; }
    // public double Percentage { get; set; }
    // }

    public class MonthlyRevenueReportViewModel
    {
        [Required]
        [Display(Name = "Tháng")]
        [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12")]
        public int Month { get; set; }

        [Required]
        [Display(Name = "Năm")]
        [Range(2020, 2030, ErrorMessage = "Năm không hợp lệ")]
        public int Year { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // Dữ liệu báo cáo
        public double TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageOrderValue { get; set; }
        public List<StaffRevenueItem> StaffRevenues { get; set; } = new List<StaffRevenueItem>();
        public List<DailyRevenueItem> DailyRevenues { get; set; } = new List<DailyRevenueItem>();
        public List<ProductRevenueItem> ProductRevenues { get; set; } = new List<ProductRevenueItem>();
    }
}

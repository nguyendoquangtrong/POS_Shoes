// Models/Entities/Report.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class Report
    {
        [Key]
        public Guid ReportID { get; set; }

        [Required]
        public string Type { get; set; } // "DAILY_REVENUE", "MONTHLY_REVENUE", etc.

        [Required]
        public DateTime Date { get; set; }

        public string Description { get; set; }

        // Thêm các trường cho báo cáo doanh thu
        public double TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageOrderValue { get; set; }
        public string ReportContent { get; set; } // JSON string chứa chi tiết báo cáo
        public string Status { get; set; } // "Generated", "Approved", "Rejected"

        [ForeignKey("UserID")]
        public Guid UserID { get; set; }
        public User User { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

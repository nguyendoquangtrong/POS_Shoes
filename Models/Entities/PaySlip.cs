// Models/Entities/PaySlip.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class PaySlip
    {
        [Key]
        public Guid PaySlipID { get; set; }

        [Required]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        public DateTime PayPeriodEnd { get; set; }

        [Required]
        public decimal HourlyRate { get; set; }

        [Required]
        public decimal TotalHours { get; set; }

        [Required]
        public decimal BasicSalary { get; set; }

        public decimal Bonus { get; set; } = 0;
        public decimal Deduction { get; set; } = 0;
        public decimal NetSalary { get; set; }

        public string Note { get; set; }
        public string Status { get; set; } = "Generated";

        // Chỉ giữ foreign keys - không có navigation properties
        [ForeignKey("User")]
        public Guid UserID { get; set; }
        public User User { get; set; } // Chỉ giữ relationship chính

        // Chỉ lưu ID, không tạo navigation property
        public Guid CreatedByUserID { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

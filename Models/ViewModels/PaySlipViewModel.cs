// Models/ViewModels/PaySlipViewModels.cs
using System.ComponentModel.DataAnnotations;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Models.ViewModels
{
    public class CreatePaySlipViewModel
    {
        [Required]
        [Display(Name = "Nhân viên")]
        public Guid UserID { get; set; }

        [Required]
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        public DateTime PayPeriodStart { get; set; }

        [Required]
        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        public DateTime PayPeriodEnd { get; set; }

        [Display(Name = "Tiền thưởng")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền thưởng không được âm")]
        public decimal Bonus { get; set; } = 0;

        [Display(Name = "Tiền khấu trừ")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền khấu trừ không được âm")]
        public decimal Deduction { get; set; } = 0;

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        // Thông tin tính toán
        public decimal HourlyRate { get; set; }
        public decimal TotalHours { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal NetSalary { get; set; }
        public List<AssignmentSummary> Assignments { get; set; } = new List<AssignmentSummary>();
    }

    public class AssignmentSummary
    {
        public DateOnly Date { get; set; }
        public string Shift { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public decimal Hours { get; set; }
        public string Description { get; set; }
    }

    public class PaySlipListViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public Guid? UserID { get; set; }
        public string Status { get; set; }
        public List<PaySlip> PaySlips { get; set; } = new List<PaySlip>();
    }
}

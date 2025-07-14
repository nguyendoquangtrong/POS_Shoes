// Models/Entities/User.cs
using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.Entities
{
    public class User
    {
        [Key]
        public Guid UserID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        // Thông tin lương
        public decimal HourlyRate { get; set; } = 50000;
        public decimal MonthlyBonus { get; set; } = 0;
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Chỉ giữ các navigation properties cần thiết
        public ICollection<Order> Orders { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<Report> Reports { get; set; }
        public ICollection<PaySlip> PaySlips { get; set; }

        // Bỏ CreatedPaySlips để tránh circular reference
    }
}

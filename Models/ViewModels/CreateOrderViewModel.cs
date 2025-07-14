// Models/ViewModels/CreateOrderViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        [Display(Name = "Khách hàng")]
        public Guid? CustomerID { get; set; }

        [Display(Name = "Tên khách hàng")]
        public string? CustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? CustomerPhone { get; set; }

        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Tiền khách đưa")]
        [Required(ErrorMessage = "Vui lòng nhập số tiền khách đưa")]
        public decimal CustomerPaid { get; set; }

        [Display(Name = "Tiền thừa")]
        public decimal Change => CustomerPaid - TotalAmount;
    }

    public class OrderItemViewModel
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
    }
}

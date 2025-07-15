using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.ViewModels
{
    public class CreateReturnReceiptViewModel
    {
        [Required]
        public Guid OrderID { get; set; }

        [Required]
        [Display(Name = "Lý do trả hàng")]
        public string Reason { get; set; }

        public List<ReturnItemViewModel> ReturnItems { get; set; } = new();

        [Display(Name = "Tổng tiền hoàn")]
        public double TotalRefundAmount { get; set; }

        // Order information for display
        public string OrderCode { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    public class ReturnItemViewModel
    {
        public Guid ProductID { get; set; }
        public string ProductName { get; set; }
        public string Size { get; set; }
        public int MaxQuantity { get; set; }
        public int ReturnQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public string Note { get; set; }
    }

}

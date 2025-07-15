using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class ReturnReceipt
    {
        [Key]
        public Guid ReturnReceiptID { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Reason { get; set; }
        public string Status { get; set; } = "Progressing";
        public double TotalRefundAmount { get; set; }

        [ForeignKey("OrderID")]
        public Guid OrderID { get; set; }
        public Order Order { get; set; }

        [ForeignKey("UserID")]
        public Guid UserID { get; set; }
        public User User { get; set; }

        public ICollection<ReturnReceiptDetail> ReturnReceiptDetails { get; set; }
    }
}

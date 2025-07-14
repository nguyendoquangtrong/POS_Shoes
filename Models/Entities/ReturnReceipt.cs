using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class ReturnReceipt
    {
        [Key]
        public Guid ReturnReceiptID { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
        public string Quantity { get; set; }

        public string Status { get; set; } = "Progressing";
        [ForeignKey("OrderID")]
        public Guid OrderID { get; set; }
    }
}
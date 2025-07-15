using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class ReturnReceiptDetail
    {
        [Key]
        public Guid ReturnReceiptDetailID { get; set; }

        [ForeignKey("ReturnReceiptID")]
        public Guid ReturnReceiptID { get; set; }
        public ReturnReceipt ReturnReceipt { get; set; }

        [ForeignKey("ProductID")]
        public Guid ProductID { get; set; }
        public Product Product { get; set; }

        public string Size { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public string Note { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class OrderDetail
    {
        [Key]
        public Guid OrderDetailID { get; set; }

        [ForeignKey("OrderID")]
        public Guid OrderID { get; set; }
        public Order Order { get; set; }

        [ForeignKey("ProductID")]
        public Guid ProductID { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
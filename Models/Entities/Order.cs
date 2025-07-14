
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class Order
    {
        [Key]
        public Guid OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; }

        [ForeignKey("CustomerID")]
        public Guid? CustomerID { get; set; }
        public Customer? Customer { get; set; }

        [ForeignKey("UserID")]
        public Guid? UserID { get; set; }
        public User? User { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
        public ICollection<ReturnReceipt> ReturnReceipts { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.Entities
{
    public class Customer
    {
        [Key]
        public Guid CustomerID { get; set; }
        public string Name { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; }

        public ICollection<Order> Orders { get; set; }
    }
}
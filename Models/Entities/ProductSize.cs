using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace POS_Shoes.Models.Entities
{
    public class ProductSize
    {
        [Key]
        public Guid SizeID { get; set; }
        public string Size { get; set; }
        public int Quantity { get; set; }
        [ForeignKey("ProductID")]
        public Guid ProductID { get; set; }
        public Product Product { get; set; }
    }
}
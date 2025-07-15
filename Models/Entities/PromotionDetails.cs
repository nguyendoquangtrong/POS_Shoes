using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POS_Shoes.Models.Entities
{
    public class PromotionDetails
    {
        [Key]
        public Guid PromotionDetailsID { get; set; }
        [ForeignKey("PromotionID")]
        public Guid PromotionID { get; set; }
        public Promotion Promotion { get; set; }

        [ForeignKey("ProductID")]
        public Guid ProductID { get; set; }
        public Product Product { get; set; }
    }
}
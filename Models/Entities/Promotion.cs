
using System.ComponentModel.DataAnnotations;

namespace POS_Shoes.Models.Entities
{
    public class Promotion
    {
        [Key]
        public Guid PromotionID { get; set; }
        public string Name { get; set; }
        public decimal discount { get; set; }
        public string Status { get; set; } = "Spending";
        public bool IsActive { get; set; } = true;


        public ICollection<PromotionDetails> PromotionDetails { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
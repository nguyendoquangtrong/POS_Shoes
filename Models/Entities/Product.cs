using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace POS_Shoes.Models.Entities
{
    public class Product
    {
        [Key]
        public Guid ProductID { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string? Image { get; set; }
        public string? ImagePublicId { get; set; }
        public string Barcode { get; set; }
        public string Supplier { get; set; }
        public string Status { get; set; }


        [ForeignKey("PromotionID")]
        public Guid? PromotionID { get; set; }
        public Promotion? Promotion { get; set; }

        public ICollection<ProductSize>? ProductSizes { get; set; }

        [NotMapped]
        public int Quantity => ProductSizes?.Sum(ps => ps.Quantity) ?? 0;

    }
}
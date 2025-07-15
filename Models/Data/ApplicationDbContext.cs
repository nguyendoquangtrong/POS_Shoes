
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Models.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
            // content
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // content
        }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<PaySlip> PaySlips { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionDetails> PromotionDetails { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReturnReceipt> ReturnReceipts { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
// Models/ViewModels/ApprovedReportsViewModel.cs
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Models.ViewModels
{
    public class ApprovedReportsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<Report> Reports { get; set; } = new List<Report>();
        public double TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public double AverageOrderValue { get; set; }
    }
}

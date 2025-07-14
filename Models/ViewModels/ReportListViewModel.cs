// Models/ViewModels/ReportListViewModel.cs
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Models.ViewModels
{
    public class ReportListViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Status { get; set; }
        public List<Report> Reports { get; set; } = new List<Report>();
    }
}

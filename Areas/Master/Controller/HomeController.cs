// Areas/Master/Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Master.Controllers
{
    public class HomeController : MasterBaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            // Thống kê tổng quan cho Master
            ViewBag.PendingPaySlips = await _context.PaySlips
                .CountAsync(p => p.Status == "Generated");

            ViewBag.PendingMonthlyReports = await _context.Reports
                .CountAsync(r => r.Type == "MONTHLY_REVENUE" && r.Status == "Generated");

            ViewBag.PendingPromotions = await _context.Promotions
                .CountAsync(p => p.Status == "Spending");

            ViewBag.PendingReturnReceipts = await _context.ReturnReceipts
                .CountAsync(r => r.Status == "Progressing");

            // Thống kê đã duyệt trong tháng
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            ViewBag.ApprovedPaySlipsThisMonth = await _context.PaySlips
                .CountAsync(p => p.Status == "Approved" &&
                           p.UpdatedAt.HasValue &&
                           p.UpdatedAt.Value.Month == currentMonth &&
                           p.UpdatedAt.Value.Year == currentYear);

            ViewBag.ApprovedReportsThisMonth = await _context.Reports
                .CountAsync(r => r.Type == "MONTHLY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue &&
                           r.UpdatedAt.Value.Month == currentMonth &&
                           r.UpdatedAt.Value.Year == currentYear);

            ViewBag.ApprovedPromotionsThisMonth = await _context.Promotions
                .CountAsync(p => p.Status == "Approved" &&
                           p.CreatedAt.Month == currentMonth &&
                           p.CreatedAt.Year == currentYear);

            ViewBag.ApprovedReturnReceiptsThisMonth = await _context.ReturnReceipts
                .CountAsync(r => r.Status == "Approved" &&
                           r.Date.Month == currentMonth &&
                           r.Date.Year == currentYear);

            return View();
        }
    }
}

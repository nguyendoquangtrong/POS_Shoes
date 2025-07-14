// Areas/Accountant/Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Accountant.Controllers
{
    public class HomeController : AccountantBaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var today = DateTime.Today;

            // Statistics for dashboard
            ViewBag.TotalEmployees = await _context.Users
                .CountAsync(u => u.IsActive && u.Role != "Accountant");

            ViewBag.MonthlyPaySlips = await _context.PaySlips
                .CountAsync(p => p.PayPeriodStart.Month == currentMonth && p.PayPeriodStart.Year == currentYear);

            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear && o.Status == "Completed")
                .SumAsync(o => (double?)o.TotalPrice) ?? 0;

            ViewBag.TotalOrders = await _context.Orders
                .CountAsync(o => o.OrderDate.Month == currentMonth && o.OrderDate.Year == currentYear && o.Status == "Completed");

            ViewBag.PendingPaySlips = await _context.PaySlips
                .CountAsync(p => p.Status == "Generated");

            ViewBag.MonthlyReports = await _context.Reports
                .CountAsync(r => r.Type == "MONTHLY_REVENUE" && r.Date.Month == currentMonth && r.Date.Year == currentYear);

            // Thống kê báo cáo đã được duyệt
            ViewBag.ApprovedReportsToday = await _context.Reports
                .CountAsync(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue && r.UpdatedAt.Value.Date == today);

            ViewBag.ApprovedReportsThisMonth = await _context.Reports
                .CountAsync(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue &&
                           r.UpdatedAt.Value.Month == currentMonth &&
                           r.UpdatedAt.Value.Year == currentYear);

            // Doanh thu từ báo cáo đã duyệt
            ViewBag.ApprovedRevenueThisMonth = await _context.Reports
                .Where(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue &&
                           r.UpdatedAt.Value.Month == currentMonth &&
                           r.UpdatedAt.Value.Year == currentYear)
                .SumAsync(r => r.TotalRevenue);

            return View();
        }
    }
}

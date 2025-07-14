// Areas/Manager/Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Areas.Manager.Models;
using POS_Shoes.Models.Data;
using System.Globalization;

namespace POS_Shoes.Areas.Manager.Controllers
{
    public class DashboardController : BaseManagerController
    {
        public DashboardController(ApplicationDbContext context) : base(context) { }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            await LoadBasicStats(model);
            await LoadProductStockChart(model);
            await LoadAssignmentChart(model);
            await LoadSalesChart(model);
            await LoadUserActivityChart(model);
            await LoadRevenueChart(model);
            await LoadRecentData(model);

            return View(model);
        }

        private async Task LoadBasicStats(DashboardViewModel model)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            model.TotalProducts = await _context.Products.CountAsync();
            model.TotalSalers = await _context.Users
                .CountAsync(u => u.Role == "Saler" && u.IsActive);

            model.TodayAssignments = await _context.Assignments
                .CountAsync(a => a.Date == today);

            model.LowStockProducts = await _context.Products
                .CountAsync(p => p.ProductSizes.Sum(ps => ps.Quantity) < 10);

            model.PendingReports = await _context.Reports
                .CountAsync(r => r.Status == "Generated" && r.Type == "DAILY_REVENUE");

            // Today's revenue and orders
            var todayRevenue = await _context.Orders
                .Where(o => o.OrderDate.Date == DateTime.Today && o.Status == "Completed")
                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

            model.TodayRevenue = todayRevenue;
            model.TodayOrders = await _context.Orders
                .CountAsync(o => o.OrderDate.Date == DateTime.Today && o.Status == "Completed");
        }

        private async Task LoadProductStockChart(DashboardViewModel model)
        {
            var productStats = await _context.Products
                .Include(p => p.ProductSizes)
                .GroupBy(p => p.Supplier)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalStock = g.Sum(p => p.ProductSizes.Sum(ps => ps.Quantity)),
                    LowStockCount = g.Count(p => p.ProductSizes.Sum(ps => ps.Quantity) < 10)
                })
                .OrderByDescending(x => x.TotalStock)
                .Take(6)
                .ToListAsync();

            model.ProductStockChart.Categories = productStats.Select(x => x.Category ?? "Không rõ").ToList();
            model.ProductStockChart.StockQuantities = productStats.Select(x => x.TotalStock).ToList();
            model.ProductStockChart.LowStockCounts = productStats.Select(x => x.LowStockCount).ToList();
        }

        private async Task LoadAssignmentChart(DashboardViewModel model)
        {
            var startDate = DateTime.Today.AddDays(-6);
            var culture = new CultureInfo("vi-VN");

            var weeklyStats = await _context.Assignments
                .Where(a => a.Date >= DateOnly.FromDateTime(startDate) && a.User.Role == "Saler")
                .GroupBy(a => a.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            model.AssignmentChart.WeekDays = weeklyStats
                .Select(x => x.Date.ToDateTime(TimeOnly.MinValue).ToString("dddd", culture))
                .ToList();
            model.AssignmentChart.AssignmentCounts = weeklyStats.Select(x => x.Count).ToList();

            var salerStats = await _context.Assignments
                .Include(a => a.User)
                .Where(a => a.User.Role == "Saler" && a.Date >= DateOnly.FromDateTime(DateTime.Today.AddDays(-30)))
                .GroupBy(a => a.User.Username)
                .Select(g => new
                {
                    SalerName = g.Key,
                    AssignmentCount = g.Count()
                })
                .OrderByDescending(x => x.AssignmentCount)
                .Take(5)
                .ToListAsync();

            model.AssignmentChart.SalerNames = salerStats.Select(x => x.SalerName).ToList();
            model.AssignmentChart.SalerAssignments = salerStats.Select(x => x.AssignmentCount).ToList();
        }

        private async Task LoadSalesChart(DashboardViewModel model)
        {
            var startDate = DateTime.Today.AddMonths(-5);

            var monthlyStats = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.Status == "Completed")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalPrice),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            model.SalesChart.Months = monthlyStats
                .Select(x => $"T{x.Month}/{x.Year}")
                .ToList();
            model.SalesChart.Revenue = monthlyStats.Select(x => Convert.ToDecimal(x.Revenue)).ToList();
            model.SalesChart.OrderCounts = monthlyStats.Select(x => x.OrderCount).ToList();
        }

        private async Task LoadUserActivityChart(DashboardViewModel model)
        {
            var userActivity = await _context.Users
                .Where(u => u.Role == "Saler" && u.IsActive)
                .Select(u => new
                {
                    SalerName = u.FullName ?? u.Username,
                    TotalAssigned = u.Assignments.Count(),
                    CompletedTasks = u.Assignments.Count(a => a.Date < DateOnly.FromDateTime(DateTime.Today))
                })
                .OrderByDescending(x => x.CompletedTasks)
                .Take(6)
                .ToListAsync();

            model.UserActivityChart.SalerNames = userActivity.Select(x => x.SalerName).ToList();
            model.UserActivityChart.TotalAssigned = userActivity.Select(x => x.TotalAssigned).ToList();
            model.UserActivityChart.CompletedTasks = userActivity.Select(x => x.CompletedTasks).ToList();
        }

        private async Task LoadRevenueChart(DashboardViewModel model)
        {
            var startDate = DateTime.Today.AddDays(-6);
            var culture = new CultureInfo("vi-VN");

            var dailyRevenue = await _context.Orders
                .Where(o => o.OrderDate.Date >= startDate && o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalPrice)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            model.RevenueChart.Days = dailyRevenue
                .Select(x => x.Date.ToString("dd/MM", culture))
                .ToList();
            model.RevenueChart.DailyRevenue = dailyRevenue
                .Select(x => Convert.ToDecimal(x.Revenue))
                .ToList();
        }

        private async Task LoadRecentData(DashboardViewModel model)
        {
            model.RecentAssignments = await _context.Assignments
                .Include(a => a.User)
                .Where(a => a.User.Role == "Saler")
                .OrderByDescending(a => a.Date)
                .Take(5)
                .ToListAsync();

            model.RecentReports = await _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "DAILY_REVENUE")
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            model.RecentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Customer)
                .Where(o => o.Status == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();
        }

        // API endpoints for real-time updates
        [HttpGet]
        public async Task<IActionResult> GetTodayStats()
        {
            var stats = new
            {
                TodayRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Date == DateTime.Today && o.Status == "Completed")
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0,
                TodayOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == DateTime.Today && o.Status == "Completed"),
                PendingReports = await _context.Reports
                    .CountAsync(r => r.Status == "Generated" && r.Type == "DAILY_REVENUE"),
                OnlineStaff = await _context.Assignments
                    .CountAsync(a => a.Date == DateOnly.FromDateTime(DateTime.Today))
            };

            return Json(stats);
        }
    }
}

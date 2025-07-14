// Areas/Accountant/Controllers/RevenueController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;
using System.Text.Json;

namespace POS_Shoes.Areas.Accountant.Controllers
{
    public class RevenueController : AccountantBaseController
    {
        public RevenueController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Accountant/Revenue/ApprovedReports
        public async Task<IActionResult> ApprovedReports(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!fromDate.HasValue) fromDate = DateTime.Today.AddDays(-30);
            if (!toDate.HasValue) toDate = DateTime.Today;

            var approvedReports = await _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue &&
                           r.UpdatedAt.Value.Date >= fromDate.Value.Date &&
                           r.UpdatedAt.Value.Date <= toDate.Value.Date)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            var viewModel = new ApprovedReportsViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                Reports = approvedReports,
                TotalRevenue = approvedReports.Sum(r => r.TotalRevenue),
                TotalOrders = approvedReports.Sum(r => r.TotalOrders),
                AverageOrderValue = approvedReports.Sum(r => r.TotalOrders) > 0 ?
                    approvedReports.Sum(r => r.TotalRevenue) / approvedReports.Sum(r => r.TotalOrders) : 0
            };

            return View(viewModel);
        }

        // GET: Accountant/Revenue/Overview
        public async Task<IActionResult> Overview(DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (!fromDate.HasValue) fromDate = DateTime.Today.AddDays(-30);
            if (!toDate.HasValue) toDate = DateTime.Today;

            // Chỉ lấy dữ liệu từ báo cáo đã được duyệt
            var approvedReports = await _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Approved" &&
                           r.UpdatedAt.HasValue &&
                           r.UpdatedAt.Value.Date >= fromDate.Value.Date &&
                           r.UpdatedAt.Value.Date <= toDate.Value.Date)
                .ToListAsync();

            var model = new RevenueOverviewViewModel
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                TotalRevenue = approvedReports.Sum(r => r.TotalRevenue),
                TotalOrders = approvedReports.Sum(r => r.TotalOrders),
                AverageOrderValue = approvedReports.Sum(r => r.TotalOrders) > 0 ?
                    approvedReports.Sum(r => r.TotalRevenue) / approvedReports.Sum(r => r.TotalOrders) : 0
            };

            // Revenue by staff từ báo cáo đã duyệt
            model.StaffRevenues = approvedReports
                .GroupBy(r => r.User)
                .Select(g => new StaffRevenueItem
                {
                    StaffName = g.Key?.FullName ?? "Unknown",
                    OrderCount = g.Sum(r => r.TotalOrders),
                    Revenue = g.Sum(r => r.TotalRevenue)
                })
                .OrderByDescending(s => s.Revenue)
                .ToList();

            // Calculate percentage
            var totalRevenue = model.StaffRevenues.Sum(s => s.Revenue);
            model.StaffRevenues.ForEach(s => s.Percentage = totalRevenue > 0 ? (s.Revenue / totalRevenue) * 100 : 0);

            // Daily revenue từ báo cáo đã duyệt
            model.DailyRevenues = approvedReports
                .GroupBy(r => r.Date.Date)
                .Select(g => new DailyRevenueItem
                {
                    Date = g.Key,
                    OrderCount = g.Sum(r => r.TotalOrders),
                    Revenue = g.Sum(r => r.TotalRevenue)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return View(model);
        }

        // GET: Accountant/Revenue/CreateMonthlyReport
        public IActionResult CreateMonthlyReport()
        {
            var model = new MonthlyRevenueReportViewModel
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            };
            return View(model);
        }

        // POST: Accountant/Revenue/CreateMonthlyReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMonthlyReport(MonthlyRevenueReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                var startDate = new DateTime(model.Year, model.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Chỉ lấy dữ liệu từ báo cáo đã được duyệt
                var approvedReports = await _context.Reports
                    .Include(r => r.User)
                    .Where(r => r.Type == "DAILY_REVENUE" &&
                               r.Status == "Approved" &&
                               r.UpdatedAt.HasValue &&
                               r.UpdatedAt.Value.Date >= startDate &&
                               r.UpdatedAt.Value.Date <= endDate)
                    .ToListAsync();

                model.TotalRevenue = approvedReports.Sum(r => r.TotalRevenue);
                model.TotalOrders = approvedReports.Sum(r => r.TotalOrders);
                model.AverageOrderValue = model.TotalOrders > 0 ? model.TotalRevenue / model.TotalOrders : 0;

                // Thống kê theo nhân viên
                model.StaffRevenues = approvedReports
                    .GroupBy(r => r.User)
                    .Select(g => new StaffRevenueItem
                    {
                        StaffName = g.Key?.FullName ?? "Unknown",
                        OrderCount = g.Sum(r => r.TotalOrders),
                        Revenue = g.Sum(r => r.TotalRevenue)
                    })
                    .OrderByDescending(s => s.Revenue)
                    .ToList();

                // Calculate percentage
                var totalRevenue = model.StaffRevenues.Sum(s => s.Revenue);
                model.StaffRevenues.ForEach(s => s.Percentage = totalRevenue > 0 ? (s.Revenue / totalRevenue) * 100 : 0);

                // Thống kê theo ngày
                model.DailyRevenues = approvedReports
                    .GroupBy(r => r.Date.Date)
                    .Select(g => new DailyRevenueItem
                    {
                        Date = g.Key,
                        OrderCount = g.Sum(r => r.TotalOrders),
                        Revenue = g.Sum(r => r.TotalRevenue)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Save report to database
                var report = new Report
                {
                    ReportID = Guid.NewGuid(),
                    Type = "MONTHLY_REVENUE",
                    Date = startDate,
                    Description = model.Description ?? $"Báo cáo doanh thu tháng {model.Month}/{model.Year} (Từ báo cáo đã duyệt)",
                    TotalRevenue = model.TotalRevenue,
                    TotalOrders = model.TotalOrders,
                    AverageOrderValue = model.AverageOrderValue,
                    ReportContent = JsonSerializer.Serialize(model),
                    Status = "Generated",
                    UserID = GetCurrentUserGuid(),
                    CreatedAt = DateTime.Now
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Báo cáo doanh thu tháng đã được tạo thành công từ dữ liệu đã duyệt!";
                return RedirectToAction("ViewMonthlyReport", new { id = report.ReportID });
            }

            return View(model);
        }

        // GET: Accountant/Revenue/ViewMonthlyReport/5
        public async Task<IActionResult> ViewMonthlyReport(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReportID == id);

            if (report == null)
            {
                return NotFound();
            }

            var model = JsonSerializer.Deserialize<MonthlyRevenueReportViewModel>(report.ReportContent);
            ViewBag.Report = report;

            return View(model);
        }

        // GET: Accountant/Revenue/Reports
        public async Task<IActionResult> Reports()
        {
            var reports = await _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "MONTHLY_REVENUE")
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View(reports);
        }
    }
}

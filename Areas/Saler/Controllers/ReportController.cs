// Areas/Saler/Controllers/ReportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;
using System.Text.Json;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class ReportController : SalerBaseController
    {
        public ReportController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Saler/Report/Index
        public async Task<IActionResult> Index()
        {
            var userGuid = GetCurrentUserGuid();
            var model = new ReportListViewModel();

            model.Reports = await _context.Reports
                .Where(r => r.UserID == userGuid)
                .OrderByDescending(r => r.Date)
                .Take(20)
                .ToListAsync();

            return View(model);
        }

        // GET: Saler/Report/CreateDailyRevenue
        public IActionResult CreateDailyRevenue()
        {
            var model = new DailyRevenueReportViewModel
            {
                ReportDate = DateTime.Today
            };
            return View(model);
        }

        // POST: Saler/Report/CreateDailyRevenue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDailyRevenue(DailyRevenueReportViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userGuid = GetCurrentUserGuid();
                var reportData = await GenerateDailyRevenueData(userGuid, model.ReportDate);

                // Cập nhật model với dữ liệu generated
                model.TotalRevenue = reportData.TotalRevenue;
                model.TotalOrders = reportData.TotalOrders;
                model.AverageOrderValue = reportData.AverageOrderValue;
                model.ProductRevenues = reportData.ProductRevenues;
                model.CustomerRevenues = reportData.CustomerRevenues;
                model.HourlyRevenues = reportData.HourlyRevenues;
                model.CashOrders = reportData.CashOrders;
                model.MemberOrders = reportData.MemberOrders;
                model.GuestOrders = reportData.GuestOrders;

                // Lưu báo cáo vào database
                var report = new Report
                {
                    ReportID = Guid.NewGuid(),
                    Type = "DAILY_REVENUE",
                    Date = model.ReportDate,
                    Description = model.Description ?? $"Báo cáo doanh thu ngày {model.ReportDate:dd/MM/yyyy}",
                    TotalRevenue = model.TotalRevenue,
                    TotalOrders = model.TotalOrders,
                    AverageOrderValue = model.AverageOrderValue,
                    ReportContent = JsonSerializer.Serialize(model),
                    Status = "Generated",
                    UserID = userGuid,
                    CreatedAt = DateTime.Now
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Báo cáo doanh thu đã được tạo thành công!";
                return RedirectToAction("ViewReport", new { id = report.ReportID });
            }

            return View(model);
        }

        // GET: Saler/Report/ViewReport/5
        public async Task<IActionResult> ViewReport(Guid id)
        {
            var userGuid = GetCurrentUserGuid();
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReportID == id && r.UserID == userGuid);

            if (report == null)
            {
                return NotFound();
            }

            if (report.Type == "DAILY_REVENUE")
            {
                var model = JsonSerializer.Deserialize<DailyRevenueReportViewModel>(report.ReportContent);
                ViewBag.Report = report;
                return View("DailyRevenueReport", model);
            }

            return View("GenericReport", report);
        }

        // Hàm tạo dữ liệu báo cáo doanh thu
        private async Task<DailyRevenueReportViewModel> GenerateDailyRevenueData(Guid userGuid, DateTime reportDate)
        {
            var startDate = reportDate.Date;
            var endDate = startDate.AddDays(1);

            // Lấy tất cả đơn hàng trong ngày
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.UserID == userGuid &&
                           o.OrderDate >= startDate &&
                           o.OrderDate < endDate &&
                           o.Status == "Completed")
                .ToListAsync();

            var model = new DailyRevenueReportViewModel
            {
                ReportDate = reportDate,
                TotalRevenue = orders.Sum(o => o.TotalPrice),
                TotalOrders = orders.Count,
                AverageOrderValue = orders.Count > 0 ? orders.Average(o => o.TotalPrice) : 0
            };

            // Doanh thu theo sản phẩm
            var productRevenues = orders
                .SelectMany(o => o.OrderDetails)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new ProductRevenueItem
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => (double)(od.UnitPrice * od.Quantity))
                })
                .OrderByDescending(pr => pr.Revenue)
                .ToList();

            // Tính phần trăm
            var totalRevenue = productRevenues.Sum(pr => pr.Revenue);
            productRevenues.ForEach(pr => pr.Percentage = totalRevenue > 0 ? (pr.Revenue / totalRevenue) * 100 : 0);
            model.ProductRevenues = productRevenues;

            // Doanh thu theo khách hàng
            model.CustomerRevenues = orders
                .Where(o => o.Customer != null)
                .GroupBy(o => o.Customer)
                .Select(g => new CustomerRevenueItem
                {
                    CustomerName = g.Key.Name,
                    CustomerPhone = g.Key.Phone,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalPrice)
                })
                .OrderByDescending(cr => cr.TotalSpent)
                .Take(10)
                .ToList();

            // Doanh thu theo giờ
            model.HourlyRevenues = orders
                .GroupBy(o => o.OrderDate.Hour)
                .Select(g => new HourlyRevenueItem
                {
                    Hour = g.Key,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalPrice)
                })
                .OrderBy(hr => hr.Hour)
                .ToList();

            // Thống kê loại khách hàng
            model.MemberOrders = orders.Count(o => o.Customer != null);
            model.GuestOrders = orders.Count(o => o.Customer == null);
            model.CashOrders = orders.Count; // Tất cả đều thanh toán tiền mặt

            return model;
        }

        // GET: Saler/Report/ExportPDF/5
        public async Task<IActionResult> ExportPDF(Guid id)
        {
            var userGuid = GetCurrentUserGuid();
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReportID == id && r.UserID == userGuid);

            if (report == null)
            {
                return NotFound();
            }

            // Tạo PDF (cần thêm thư viện như iTextSharp hoặc DinkToPdf)
            // Đây là placeholder - bạn có thể implement PDF export

            return Json(new { success = true, message = "Chức năng xuất PDF đang được phát triển" });
        }

        // POST: Saler/Report/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userGuid = GetCurrentUserGuid();
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.ReportID == id && r.UserID == userGuid);

            if (report == null)
            {
                return NotFound();
            }

            _context.Reports.Remove(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Báo cáo đã được xóa thành công!";
            return RedirectToAction("Index");
        }
    }
}

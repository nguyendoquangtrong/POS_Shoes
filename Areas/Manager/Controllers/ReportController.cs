// Areas/Manager/Controllers/ReportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Areas.Manager.Controllers
{
    public class ReportController : BaseManagerController
    {
        private readonly ILogger<ReportController> _logger;

        public ReportController(ApplicationDbContext context, ILogger<ReportController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Manager/Report/Index
        public async Task<IActionResult> Index(DateTime? date = null, string status = "")
        {
            if (!date.HasValue)
                date = DateTime.Today;

            var query = _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "DAILY_REVENUE" &&
                           r.CreatedAt.Date == date.Value.Date &&
                           r.User.Role == "Saler");

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedDate = date.Value;
            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.Reports
                .CountAsync(r => r.Type == "DAILY_REVENUE" && r.Status == "Generated" && r.User.Role == "Saler");

            return View(reports);
        }

        // GET: Manager/Report/PendingReports
        public async Task<IActionResult> PendingReports()
        {
            var reports = await _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "DAILY_REVENUE" &&
                           r.Status == "Generated" &&
                           r.User.Role == "Saler")
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // GET: Manager/Report/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReportID == id);

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // POST: Manager/Report/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = "Approved";
            report.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(approvalNote))
            {
                report.Description += $" | Ghi chú duyệt: {approvalNote}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Report {id} approved by Manager {GetCurrentUserId()}");

            TempData["SuccessMessage"] = "Báo cáo đã được duyệt thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Manager/Report/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrEmpty(rejectionReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            var report = await _context.Reports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = "Rejected";
            report.UpdatedAt = DateTime.Now;
            report.Description += $" | Lý do từ chối: {rejectionReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Report {id} rejected by Manager {GetCurrentUserId()}: {rejectionReason}");

            TempData["SuccessMessage"] = "Báo cáo đã bị từ chối!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Manager/Report/ApproveMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMultiple(List<Guid> reportIds)
        {
            if (reportIds == null || !reportIds.Any())
            {
                TempData["ErrorMessage"] = "Không có báo cáo nào được chọn!";
                return RedirectToAction("PendingReports");
            }

            var reports = await _context.Reports
                .Where(r => reportIds.Contains(r.ReportID) && r.Status == "Generated")
                .ToListAsync();

            foreach (var report in reports)
            {
                report.Status = "Approved";
                report.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt {reports.Count} báo cáo thành công!";
            return RedirectToAction("PendingReports");
        }

        // API: Get report statistics
        [HttpGet]
        public async Task<IActionResult> GetReportStats()
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-7);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                TodayPending = await _context.Reports
                    .CountAsync(r => r.Type == "DAILY_REVENUE" && r.Status == "Generated" &&
                               r.CreatedAt.Date == today && r.User.Role == "Saler"),
                WeeklyApproved = await _context.Reports
                    .CountAsync(r => r.Type == "DAILY_REVENUE" && r.Status == "Approved" &&
                               r.UpdatedAt >= thisWeek && r.User.Role == "Saler"),
                MonthlyApproved = await _context.Reports
                    .CountAsync(r => r.Type == "DAILY_REVENUE" && r.Status == "Approved" &&
                               r.UpdatedAt >= thisMonth && r.User.Role == "Saler"),
                TotalRejected = await _context.Reports
                    .CountAsync(r => r.Type == "DAILY_REVENUE" && r.Status == "Rejected" &&
                               r.User.Role == "Saler")
            };

            return Json(stats);
        }
    }
}

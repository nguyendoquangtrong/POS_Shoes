// Areas/Master/Controllers/ReportApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Master.Controllers
{
    public class ReportApprovalController : MasterBaseController
    {
        private readonly ILogger<ReportApprovalController> _logger;

        public ReportApprovalController(ApplicationDbContext context, ILogger<ReportApprovalController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Master/ReportApproval/Index
        public async Task<IActionResult> Index(string status = "")
        {
            var query = _context.Reports
                .Include(r => r.User)
                .Where(r => r.Type == "MONTHLY_REVENUE") // Chỉ báo cáo tháng
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.Reports
                .CountAsync(r => r.Type == "MONTHLY_REVENUE" && r.Status == "Generated");

            return View(reports);
        }

        // GET: Master/ReportApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var report = await _context.Reports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReportID == id && r.Type == "MONTHLY_REVENUE");

            if (report == null)
            {
                return NotFound();
            }

            return View(report);
        }

        // POST: Master/ReportApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null || report.Type != "MONTHLY_REVENUE")
            {
                return NotFound();
            }

            report.Status = "Approved";
            report.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(approvalNote))
            {
                report.Description += $" | Master duyệt: {approvalNote}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Monthly Report {id} approved by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Báo cáo tháng đã được duyệt thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/ReportApproval/Reject
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
            if (report == null || report.Type != "MONTHLY_REVENUE")
            {
                return NotFound();
            }

            report.Status = "Rejected";
            report.UpdatedAt = DateTime.Now;
            report.Description += $" | Master từ chối: {rejectionReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Monthly Report {id} rejected by Master {User.Identity.Name}: {rejectionReason}");

            TempData["SuccessMessage"] = "Báo cáo tháng đã bị từ chối!";
            return RedirectToAction("Details", new { id });
        }
    }
}

// Areas/Master/Controllers/PaySlipApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Master.Controllers
{
    public class PaySlipApprovalController : MasterBaseController
    {
        private readonly ILogger<PaySlipApprovalController> _logger;

        public PaySlipApprovalController(ApplicationDbContext context, ILogger<PaySlipApprovalController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Master/PaySlipApproval/Index
        public async Task<IActionResult> Index(string status = "")
        {
            var query = _context.PaySlips
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var paySlips = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.PaySlips.CountAsync(p => p.Status == "Generated");

            return View(paySlips);
        }

        // GET: Master/PaySlipApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var paySlip = await _context.PaySlips
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaySlipID == id);

            if (paySlip == null)
            {
                return NotFound();
            }

            return View(paySlip);
        }

        // POST: Master/PaySlipApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var paySlip = await _context.PaySlips.FindAsync(id);
            if (paySlip == null)
            {
                return NotFound();
            }

            paySlip.Status = "Approved";
            paySlip.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(approvalNote))
            {
                paySlip.Note += $" | Master duyệt: {approvalNote}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"PaySlip {id} approved by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Phiếu lương đã được duyệt thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PaySlipApproval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrEmpty(rejectionReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            var paySlip = await _context.PaySlips.FindAsync(id);
            if (paySlip == null)
            {
                return NotFound();
            }

            paySlip.Status = "Rejected";
            paySlip.UpdatedAt = DateTime.Now;
            paySlip.Note += $" | Master từ chối: {rejectionReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"PaySlip {id} rejected by Master {User.Identity.Name}: {rejectionReason}");

            TempData["SuccessMessage"] = "Phiếu lương đã bị từ chối!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PaySlipApproval/MarkAsPaid
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(Guid id)
        {
            var paySlip = await _context.PaySlips.FindAsync(id);
            if (paySlip == null)
            {
                return NotFound();
            }

            if (paySlip.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh dấu đã thanh toán cho phiếu lương đã được duyệt!";
                return RedirectToAction("Details", new { id });
            }

            paySlip.Status = "Paid";
            paySlip.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Phiếu lương đã được đánh dấu là đã thanh toán!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PaySlipApproval/ApproveMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMultiple(List<Guid> paySlipIds)
        {
            if (paySlipIds == null || !paySlipIds.Any())
            {
                TempData["ErrorMessage"] = "Không có phiếu lương nào được chọn!";
                return RedirectToAction("Index");
            }

            var paySlips = await _context.PaySlips
                .Where(p => paySlipIds.Contains(p.PaySlipID) && p.Status == "Generated")
                .ToListAsync();

            foreach (var paySlip in paySlips)
            {
                paySlip.Status = "Approved";
                paySlip.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã duyệt {paySlips.Count} phiếu lương thành công!";
            return RedirectToAction("Index");
        }
    }
}

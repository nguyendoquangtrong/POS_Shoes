using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Master.Controllers
{
    public class ReturnReceiptApprovalController : MasterBaseController
    {
        private readonly ILogger<ReturnReceiptApprovalController> _logger;

        public ReturnReceiptApprovalController(ApplicationDbContext context, ILogger<ReturnReceiptApprovalController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Master/ReturnReceiptApproval/Index
        public async Task<IActionResult> Index(string status = "")
        {
            var query = _context.ReturnReceipts
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.User)
                .Include(r => r.ReturnReceiptDetails)
                    .ThenInclude(rd => rd.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var returnReceipts = await query
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.ReturnReceipts.CountAsync(r => r.Status == "Progressing");
            ViewBag.ApprovedCount = await _context.ReturnReceipts.CountAsync(r => r.Status == "Approved");
            ViewBag.RejectedCount = await _context.ReturnReceipts.CountAsync(r => r.Status == "Rejected");
            ViewBag.TotalCount = await _context.ReturnReceipts.CountAsync();

            return View(returnReceipts);
        }

        // GET: Master/ReturnReceiptApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var returnReceipt = await _context.ReturnReceipts
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.User)
                .Include(r => r.ReturnReceiptDetails)
                    .ThenInclude(rd => rd.Product)
                .FirstOrDefaultAsync(r => r.ReturnReceiptID == id);

            if (returnReceipt == null)
            {
                return NotFound();
            }

            return View(returnReceipt);
        }

        // POST: Master/ReturnReceiptApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var returnReceipt = await _context.ReturnReceipts
                .Include(r => r.ReturnReceiptDetails)
                .FirstOrDefaultAsync(r => r.ReturnReceiptID == id);
            if (returnReceipt == null)
            {
                return NotFound();
            }
            foreach (var dt in returnReceipt.ReturnReceiptDetails)
            {
                dt.RefundAmount = dt.Quantity * dt.UnitPrice;
            }

            var actualRefundAmount = returnReceipt.ReturnReceiptDetails
                .Sum(detail => detail.RefundAmount);

            returnReceipt.Status = "Approved";
            returnReceipt.TotalRefundAmount = (double)actualRefundAmount;

            if (!string.IsNullOrEmpty(approvalNote))
            {
                returnReceipt.Reason += $" | Master duyệt: {approvalNote}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"ReturnReceipt {id} approved by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Phiếu trả hàng đã được duyệt thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/ReturnReceiptApproval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrEmpty(rejectionReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            var returnReceipt = await _context.ReturnReceipts.FindAsync(id);
            if (returnReceipt == null)
            {
                return NotFound();
            }

            returnReceipt.Status = "Rejected";
            returnReceipt.Reason += $" | Master từ chối: {rejectionReason}";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"ReturnReceipt {id} rejected by Master {User.Identity.Name}: {rejectionReason}");

            TempData["SuccessMessage"] = "Phiếu trả hàng đã bị từ chối!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/ReturnReceiptApproval/BulkApprove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(List<Guid> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một phiếu trả hàng!";
                return RedirectToAction("Index");
            }

            var returnReceipts = await _context.ReturnReceipts
                .Where(r => selectedIds.Contains(r.ReturnReceiptID) && r.Status == "Progressing")
                .ToListAsync();

            foreach (var receipt in returnReceipts)
            {
                receipt.Status = "Approved";
                receipt.Reason += $" | Master duyệt hàng loạt";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Bulk approved {returnReceipts.Count} return receipts by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = $"Đã duyệt thành công {returnReceipts.Count} phiếu trả hàng!";
            return RedirectToAction("Index");
        }
    }
}

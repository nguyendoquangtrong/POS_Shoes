// Areas/Master/Controllers/ReturnReceiptApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Manager.Controllers
{
    public class ReturnReceiptApprovalController : BaseManagerController
    {
        private readonly ILogger<ReturnReceiptApprovalController> _logger;

        public ReturnReceiptApprovalController(ApplicationDbContext context, ILogger<ReturnReceiptApprovalController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Master/ReturnReceiptApproval/Index
        public async Task<IActionResult> Index(string status = "")
        {
            var query = _context.ReturnReceipts.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var returnReceipts = await query
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.ReturnReceipts.CountAsync(r => r.Status == "Progressing");

            return View(returnReceipts);
        }

        // GET: Master/ReturnReceiptApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var returnReceipt = await _context.ReturnReceipts.FindAsync(id);
            if (returnReceipt == null)
            {
                return NotFound();
            }

            // Load related order information
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == returnReceipt.OrderID);

            ViewBag.Order = order;

            return View(returnReceipt);
        }

        // POST: Master/ReturnReceiptApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var returnReceipt = await _context.ReturnReceipts.FindAsync(id);
            if (returnReceipt == null)
            {
                return NotFound();
            }

            returnReceipt.Status = "Approved";
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
    }
}

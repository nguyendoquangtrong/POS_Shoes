// Areas/Master/Controllers/PromotionApprovalController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Master.Controllers
{
    public class PromotionApprovalController : MasterBaseController
    {
        private readonly ILogger<PromotionApprovalController> _logger;

        public PromotionApprovalController(ApplicationDbContext context, ILogger<PromotionApprovalController> logger) : base(context)
        {
            _logger = logger;
        }

        // GET: Master/PromotionApproval/Index
        public async Task<IActionResult> Index(string status = "")
        {
            var query = _context.Promotions.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            ViewBag.PendingCount = await _context.Promotions.CountAsync(p => p.Status == "Spending");

            return View(promotions);
        }

        // GET: Master/PromotionApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: Master/PromotionApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            promotion.Status = "Approved";
            promotion.IsActive = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} approved by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Khuyến mãi đã được duyệt và kích hoạt!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PromotionApproval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrEmpty(rejectionReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            promotion.Status = "Rejected";
            promotion.IsActive = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} rejected by Master {User.Identity.Name}: {rejectionReason}");

            TempData["SuccessMessage"] = "Khuyến mãi đã bị từ chối!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PromotionApproval/Expire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Expire(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return NotFound();
            }

            promotion.Status = "Expired";
            promotion.IsActive = false;
            promotion.EndDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Khuyến mãi đã được đánh dấu hết hạn!";
            return RedirectToAction("Details", new { id });
        }
    }
}

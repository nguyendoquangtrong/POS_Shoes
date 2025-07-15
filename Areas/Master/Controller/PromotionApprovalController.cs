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
            ViewBag.PendingCount = await _context.Promotions.CountAsync(p => p.Status == "Generated");
            ViewBag.TotalPromotions = promotions.Count;

            return View(promotions);
        }

        // GET: Master/PromotionApproval/PendingPromotions
        public async Task<IActionResult> PendingPromotions()
        {
            var pendingPromotions = await _context.Promotions
                .Where(p => p.Status == "Generated")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pendingPromotions);
        }

        // GET: Master/PromotionApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var promotion = await _context.Promotions
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction("Index");
            }

            return View(promotion);
        }

        // POST: Master/PromotionApproval/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction("Index");
            }

            if (promotion.Status != "Generated")
            {
                TempData["ErrorMessage"] = "Khuyến mãi này đã được xử lý trước đó!";
                return RedirectToAction("Details", new { id });
            }

            // Kiểm tra thời gian hợp lệ
            if (promotion.EndDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể duyệt khuyến mãi đã hết hạn!";
                return RedirectToAction("Details", new { id });
            }

            promotion.Status = "Approved";
            promotion.IsActive = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} approved by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Khuyến mãi đã được duyệt và kích hoạt thành công!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PromotionApproval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction("Index");
            }

            if (promotion.Status != "Generated")
            {
                TempData["ErrorMessage"] = "Khuyến mãi này đã được xử lý trước đó!";
                return RedirectToAction("Details", new { id });
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
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction("Index");
            }

            if (promotion.Status != "Approved")
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh dấu hết hạn cho khuyến mãi đã được duyệt!";
                return RedirectToAction("Details", new { id });
            }

            promotion.Status = "Expired";
            promotion.IsActive = false;
            promotion.EndDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} expired by Master {User.Identity.Name}");

            TempData["SuccessMessage"] = "Khuyến mãi đã được đánh dấu hết hạn!";
            return RedirectToAction("Details", new { id });
        }

        // POST: Master/PromotionApproval/ApproveMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMultiple(List<Guid> promotionIds)
        {
            if (promotionIds == null || !promotionIds.Any())
            {
                TempData["ErrorMessage"] = "Không có khuyến mãi nào được chọn!";
                return RedirectToAction("PendingPromotions");
            }

            var promotions = await _context.Promotions
                .Where(p => promotionIds.Contains(p.PromotionID) && p.Status == "Generated")
                .ToListAsync();

            var successCount = 0;
            var errorCount = 0;

            foreach (var promotion in promotions)
            {
                if (promotion.EndDate > DateTime.Now)
                {
                    promotion.Status = "Approved";
                    promotion.IsActive = true;
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            if (successCount > 0)
            {
                TempData["SuccessMessage"] = $"Đã duyệt {successCount} khuyến mãi thành công!";
            }

            if (errorCount > 0)
            {
                TempData["ErrorMessage"] = $"{errorCount} khuyến mãi không thể duyệt do đã hết hạn!";
            }

            return RedirectToAction("PendingPromotions");
        }

        // API: Get promotion statistics
        [HttpGet]
        public async Task<IActionResult> GetPromotionStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                TodayPending = await _context.Promotions
                    .CountAsync(p => p.Status == "Generated" && p.CreatedAt.Date == today),
                TotalApproved = await _context.Promotions
                    .CountAsync(p => p.Status == "Approved"),
                ActivePromotions = await _context.Promotions
                    .CountAsync(p => p.Status == "Approved" && p.IsActive && p.EndDate > DateTime.Now),
                ExpiredThisMonth = await _context.Promotions
                    .CountAsync(p => p.Status == "Expired" && p.EndDate >= thisMonth)
            };

            return Json(stats);
        }
    }
}

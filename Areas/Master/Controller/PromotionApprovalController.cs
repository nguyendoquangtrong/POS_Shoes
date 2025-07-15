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
            ViewBag.PendingCount = await _context.Promotions.CountAsync(p => p.Status == "Pending");
            ViewBag.TotalPromotions = promotions.Count;
            ViewBag.ApprovedCount = await _context.Promotions.CountAsync(p => p.Status == "Approved");
            ViewBag.RejectedCount = await _context.Promotions.CountAsync(p => p.Status == "Rejected");

            return View(promotions);
        }

        // GET: Master/PromotionApproval/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionDetails)
                    .ThenInclude(pd => pd.Product)
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promotion == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khuyến mãi!";
                return RedirectToAction("Index");
            }

            return View(promotion);
        }

        // POST: Master/PromotionApproval/Approve - ✅ CHÍNH SỬA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string approvalNote = "")
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
            }

            // Kiểm tra trạng thái hiện tại
            if (promotion.Status == "Approved")
            {
                return Json(new { success = false, message = "Khuyến mãi đã được duyệt trước đó!" });
            }

            // Kiểm tra thời gian hợp lệ
            if (promotion.EndDate <= DateTime.Now)
            {
                return Json(new { success = false, message = "Không thể duyệt khuyến mãi đã hết hạn!" });
            }

            // ✅ CẬP NHẬT TRẠNG THÁI THÀNH "Approved"
            promotion.Status = "Approved";
            promotion.IsActive = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} approved by Master {User.Identity.Name}");

            return Json(new
            {
                success = true,
                message = "Khuyến mãi đã được duyệt thành công!",
                newStatus = "Approved",
                isActive = true
            });
        }

        // POST: Master/PromotionApproval/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                return Json(new { success = false, message = "Vui lòng nhập lý do từ chối!" });
            }

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
            }

            if (promotion.Status == "Rejected")
            {
                return Json(new { success = false, message = "Khuyến mãi đã bị từ chối trước đó!" });
            }

            // ✅ CẬP NHẬT TRẠNG THÁI THÀNH "Rejected"
            promotion.Status = "Rejected";
            promotion.IsActive = false;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} rejected by Master {User.Identity.Name}: {rejectionReason}");

            return Json(new
            {
                success = true,
                message = "Khuyến mãi đã bị từ chối!",
                newStatus = "Rejected",
                isActive = false
            });
        }

        // POST: Master/PromotionApproval/Expire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Expire(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khuyến mãi!" });
            }

            if (promotion.Status != "Approved")
            {
                return Json(new { success = false, message = "Chỉ có thể đánh dấu hết hạn cho khuyến mãi đã được duyệt!" });
            }

            // ✅ CẬP NHẬT TRẠNG THÁI THÀNH "Expired"
            promotion.Status = "Expired";
            promotion.IsActive = false;
            promotion.EndDate = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Promotion {id} expired by Master {User.Identity.Name}");

            return Json(new
            {
                success = true,
                message = "Khuyến mãi đã được đánh dấu hết hạn!",
                newStatus = "Expired",
                isActive = false
            });
        }

        // POST: Master/PromotionApproval/ApproveMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMultiple(List<Guid> promotionIds)
        {
            if (promotionIds == null || !promotionIds.Any())
            {
                return Json(new { success = false, message = "Không có khuyến mãi nào được chọn!" });
            }

            var promotions = await _context.Promotions
                .Where(p => promotionIds.Contains(p.PromotionID) && p.Status == "Pending")
                .ToListAsync();

            var successCount = 0;
            var errorCount = 0;

            foreach (var promotion in promotions)
            {
                if (promotion.EndDate > DateTime.Now)
                {
                    // ✅ CẬP NHẬT TRẠNG THÁI THÀNH "Approved"
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

            var message = "";
            if (successCount > 0)
            {
                message += $"Đã duyệt {successCount} khuyến mãi thành công!";
            }
            if (errorCount > 0)
            {
                message += $" {errorCount} khuyến mãi không thể duyệt do đã hết hạn!";
            }

            return Json(new
            {
                success = true,
                message = message,
                successCount = successCount,
                errorCount = errorCount
            });
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
                    .CountAsync(p => p.Status == "Pending" && p.CreatedAt.Date == today),
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Areas.Marketing.Controllers
{
    public class PromotionController : MarketingBaseController
    {
        public PromotionController(ApplicationDbContext ctx) : base(ctx) { }

        // Danh sách Promotion
        public async Task<IActionResult> Index()
        {
            var promotions = await _context.Promotions
                .Include(p => p.PromotionDetails)
                    .ThenInclude(pd => pd.Product)
                .Where(p => p.IsActive) // Chỉ lấy promotion active
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(promotions);
        }

        // GET: Marketing/Promotion/Create
        public async Task<IActionResult> Create()
        {
            // Load danh sách products chưa có promotion hoặc promotion đã hết hạn
            var activePromotionProductIds = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => pd.Promotion.IsActive &&
                            pd.Promotion.Status == "Approved" &&
                            pd.Promotion.StartDate <= DateTime.Now &&
                            pd.Promotion.EndDate >= DateTime.Now)
                .Select(pd => pd.ProductID)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Status == "Active" && !activePromotionProductIds.Contains(p.ProductID))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.AllProducts = await _context.Products
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(new Promotion());
        }

        // POST: Marketing/Promotion/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion model, List<Guid> selectedProductIds, bool replaceExisting = false)
        {
            // ✅ Validation cải thiện
            if (model.StartDate >= model.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (model.discount <= 0 || model.discount > 100)
            {
                ModelState.AddModelError("discount", "Phần trăm giảm giá phải từ 1% đến 100%");
            }

            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sản phẩm để áp dụng promotion");
            }

            // ✅ Loại bỏ validation cho các field Marketing không được quyền set
            ModelState.Remove("PromotionDetails");
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                await ReloadProductsForView();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                model.PromotionID = Guid.NewGuid();

                // ✅ Marketing chỉ có thể tạo promotion với status "Pending"
                model.Status = "Pending";
                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Add(model);

                // Xử lý thay thế promotion hiện tại nếu cần
                if (replaceExisting)
                {
                    var existingPromotionDetails = await _context.PromotionDetails
                        .Where(pd => selectedProductIds.Contains(pd.ProductID))
                        .ToListAsync();

                    if (existingPromotionDetails.Any())
                    {
                        _context.PromotionDetails.RemoveRange(existingPromotionDetails);
                    }
                }

                // Thêm PromotionDetails cho các sản phẩm được chọn
                foreach (var productId in selectedProductIds)
                {
                    var promotionDetail = new PromotionDetails
                    {
                        PromotionDetailsID = Guid.NewGuid(),
                        PromotionID = model.PromotionID,
                        ProductID = productId,
                    };
                    _context.PromotionDetails.Add(promotionDetail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Promotion '{model.Name}' đã được tạo và đang chờ phê duyệt từ Master!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";

                await ReloadProductsForView();
                return View(model);
            }
        }

        // GET: Marketing/Promotion/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionDetails)
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promotion == null) return NotFound();

            // ✅ Chỉ cho phép edit promotion có status "Pending"
            if (promotion.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể chỉnh sửa promotion đang chờ duyệt! Promotion đã được phê duyệt hoặc từ chối không thể chỉnh sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Load products
            await ReloadProductsForView();

            // Set selected products
            ViewBag.SelectedProductIds = promotion.PromotionDetails?.Select(pd => pd.ProductID).ToList() ?? new List<Guid>();

            return View(promotion);
        }

        // POST: Marketing/Promotion/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Promotion model, List<Guid> selectedProductIds)
        {
            if (id != model.PromotionID) return NotFound();

            // Validation
            if (model.StartDate >= model.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            if (model.discount <= 0 || model.discount > 100)
            {
                ModelState.AddModelError("discount", "Phần trăm giảm giá phải từ 1% đến 100%");
            }

            if (selectedProductIds == null || !selectedProductIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sản phẩm");
            }

            // ✅ Loại bỏ validation cho các field Marketing không được quyền set
            ModelState.Remove("Status");

            if (!ModelState.IsValid)
            {
                await ReloadProductsForView();
                ViewBag.SelectedProductIds = selectedProductIds ?? new List<Guid>();
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingPromotion = await _context.Promotions
                    .Include(p => p.PromotionDetails)
                    .FirstOrDefaultAsync(p => p.PromotionID == id);

                if (existingPromotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy promotion!";
                    return RedirectToAction(nameof(Index));
                }

                // ✅ Chỉ cho phép edit promotion có status "Pending"
                if (existingPromotion.Status != "Pending")
                {
                    TempData["ErrorMessage"] = "Chỉ có thể chỉnh sửa promotion đang chờ duyệt!";
                    return RedirectToAction(nameof(Index));
                }

                // Update promotion info (không bao gồm Status)
                existingPromotion.Name = model.Name;
                existingPromotion.discount = model.discount;
                existingPromotion.StartDate = model.StartDate;
                existingPromotion.EndDate = model.EndDate;
                // ✅ Status vẫn giữ nguyên "Pending"

                // Update promotion details
                _context.PromotionDetails.RemoveRange(existingPromotion.PromotionDetails);

                foreach (var productId in selectedProductIds)
                {
                    var promotionDetail = new PromotionDetails
                    {
                        PromotionDetailsID = Guid.NewGuid(),
                        PromotionID = id,
                        ProductID = productId,
                    };
                    _context.PromotionDetails.Add(promotionDetail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Promotion đã được cập nhật và đang chờ phê duyệt lại!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        // GET: Marketing/Promotion/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionDetails)
                    .ThenInclude(pd => pd.Product)
                        .ThenInclude(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promotion == null)
            {
                return NotFound();
            }

            return View(promotion);
        }

        // POST: Marketing/Promotion/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionDetails)
                .FirstOrDefaultAsync(p => p.PromotionID == id);

            if (promotion == null) return NotFound();


            if (promotion.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể xóa promotion đang chờ duyệt! Promotion đã được phê duyệt không thể xóa.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Xóa PromotionDetails trước
                if (promotion.PromotionDetails != null && promotion.PromotionDetails.Any())
                {
                    _context.PromotionDetails.RemoveRange(promotion.PromotionDetails);
                }

                // Xóa mềm promotion
                promotion.IsActive = false;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Promotion '{promotion.Name}' đã được xóa!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ Helper method
        private async Task ReloadProductsForView()
        {
            // Chỉ loại trừ sản phẩm có promotion đã được approve và đang trong thời gian hiệu lực
            var activePromotionProductIds = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => pd.Promotion.IsActive &&
                           pd.Promotion.Status == "Approved" && // Chỉ tính promotion đã approve
                           pd.Promotion.StartDate <= DateTime.Now &&
                           pd.Promotion.EndDate >= DateTime.Now)
                .Select(pd => pd.ProductID)
                .ToListAsync();

            var products = await _context.Products
                .Where(p => p.Status == "Active" && !activePromotionProductIds.Contains(p.ProductID))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.Products = products;
            ViewBag.AllProducts = await _context.Products
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }
    }
}

// Areas/Manager/Controllers/ProductController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Services.Interfaces;

namespace POS_Shoes.Areas.Manager.Controllers
{
    public class ProductController : BaseManagerController
    {
        private readonly ICloudinaryService _cloudinaryService;

        public ProductController(ApplicationDbContext context, ICloudinaryService cloudinaryService)
            : base(context)
        {
            _cloudinaryService = cloudinaryService;
        }

        // GET: Manager/Product/Create - CẬP NHẬT
        public async Task<IActionResult> Create()
        {
            try
            {
                // Lấy danh sách promotions an toàn
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive &&
                               p.StartDate <= DateTime.Now &&
                               p.EndDate >= DateTime.Now)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                ViewData["PromotionID"] = new SelectList(
                    promotions ?? new List<Promotion>(),
                    "PromotionID",
                    "Name"
                );

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading promotions: {ex.Message}");
                ViewData["PromotionID"] = new SelectList(new List<Promotion>(), "PromotionID", "Name");
                TempData["Warning"] = "Không thể tải danh sách khuyến mãi. Vui lòng thử lại.";
                return View();
            }
        }

        // POST: Manager/Product/Create - CẬP NHẬT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Price,Barcode,Supplier,Status")] Product product,
            IFormFile? imageFile, List<string> sizes, List<int> quantities, Guid? promotionId)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    product.ProductID = Guid.NewGuid();

                    // Handle image upload to Cloudinary
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                            product.Image = uploadResult.SecureUrl.ToString();
                            product.ImagePublicId = uploadResult.PublicId;
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Lỗi upload hình ảnh: {ex.Message}";
                            await LoadPromotionsForView(promotionId);
                            return View(product);
                        }
                    }

                    // Add product
                    _context.Add(product);

                    // Add product sizes
                    for (int i = 0; i < sizes.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(sizes[i]) && quantities[i] > 0)
                        {
                            var productSize = new ProductSize
                            {
                                SizeID = Guid.NewGuid(),
                                ProductID = product.ProductID,
                                Size = sizes[i],
                                Quantity = quantities[i]
                            };
                            _context.ProductSizes.Add(productSize);
                        }
                    }

                    // ✅ Xử lý promotion thông qua PromotionDetails
                    if (promotionId.HasValue && promotionId.Value != Guid.Empty)
                    {
                        var promotionDetail = new PromotionDetails
                        {
                            PromotionDetailsID = Guid.NewGuid(),
                            PromotionID = promotionId.Value,
                            ProductID = product.ProductID,
                        };
                        _context.PromotionDetails.Add(promotionDetail);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Tạo sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Lỗi tạo sản phẩm: {ex.Message}";
                    Console.WriteLine($"Create Error: {ex}");
                }
            }

            await LoadPromotionsForView(promotionId);
            return View(product);
        }

        // GET: Manager/Product/Edit/5 - CẬP NHẬT
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            // ✅ Lấy promotion hiện tại từ PromotionDetails
            var currentPromotion = await _context.PromotionDetails
                .Where(pd => pd.ProductID == id.Value)
                .Select(pd => pd.PromotionID)
                .FirstOrDefaultAsync();

            await LoadPromotionsForView(currentPromotion == Guid.Empty ? null : currentPromotion);
            ViewBag.CurrentPromotionId = currentPromotion;

            return View(product);
        }

        // POST: Manager/Product/Edit/5 - CẬP NHẬT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id,
            [Bind("ProductID,ProductName,Price,Barcode,Supplier,Status,Image,ImagePublicId")]
            Product product, IFormFile? imageFile,
            List<Guid>? existingSizeIds, List<string>? existingSizes, List<int>? existingQuantities,
            List<string>? newSizes, List<int>? newQuantities,
            List<Guid>? deletedSizeIds, Guid? promotionId)
        {
            if (id != product.ProductID) return NotFound();
            CleanupModelStateForSizes();

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingProduct = await _context.Products
                        .Include(p => p.ProductSizes)
                        .FirstOrDefaultAsync(p => p.ProductID == id);

                    if (existingProduct == null) return NotFound();

                    var oldImagePublicId = existingProduct.ImagePublicId;

                    // Update basic product info
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Price = product.Price;
                    existingProduct.Barcode = product.Barcode;
                    existingProduct.Supplier = product.Supplier;
                    existingProduct.Status = product.Status;

                    // Handle image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                            existingProduct.Image = uploadResult.SecureUrl.ToString();
                            existingProduct.ImagePublicId = uploadResult.PublicId;

                            // Delete old image from Cloudinary
                            if (!string.IsNullOrEmpty(oldImagePublicId))
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _cloudinaryService.DeleteImageAsync(oldImagePublicId);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to delete old image: {ex.Message}");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            TempData["Error"] = $"Lỗi upload hình ảnh: {ex.Message}";
                            await transaction.RollbackAsync();
                            await LoadPromotionsForView(promotionId);
                            return View(existingProduct);
                        }
                    }

                    // ✅ Xử lý promotion thông qua PromotionDetails
                    await HandlePromotionUpdate(id, promotionId);

                    // Handle Size Updates
                    await HandleSizeUpdates(existingProduct, deletedSizeIds, existingSizeIds,
                        existingSizes, existingQuantities, newSizes, newQuantities);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Lỗi cập nhật: {ex.Message}";
                    Console.WriteLine($"Edit Error: {ex}");
                }
            }

            var productWithSizes = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            await LoadPromotionsForView(promotionId);
            return View(productWithSizes ?? product);
        }

        // GET: Manager/Product/Index - CẬP NHẬT
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .ToListAsync();

            // ✅ Load promotion data cho mỗi product
            var productIds = products.Select(p => p.ProductID).ToList();
            var promotionDetails = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => productIds.Contains(pd.ProductID))
                .ToListAsync();

            ViewBag.PromotionDetails = promotionDetails;

            return View(products);
        }

        // GET: Manager/Product/Details/5 - CẬP NHẬT
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null) return NotFound();

            // ✅ Load promotion data
            var promotionDetail = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .FirstOrDefaultAsync(pd => pd.ProductID == id);

            ViewBag.PromotionDetail = promotionDetail;

            return View(product);
        }

        // GET: Manager/Product/Delete/5 - CẬP NHẬT
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null) return NotFound();

            // ✅ Load promotion data
            var promotionDetail = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .FirstOrDefaultAsync(pd => pd.ProductID == id);

            ViewBag.PromotionDetail = promotionDetail;

            return View(product);
        }

        // DELETE: Manager/Product/Delete/5 - CẬP NHẬT
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductSizes)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm cần xóa!";
                    return NotFound();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // ✅ Remove PromotionDetails first
                    var promotionDetails = await _context.PromotionDetails
                        .Where(pd => pd.ProductID == id)
                        .ToListAsync();

                    if (promotionDetails.Any())
                    {
                        _context.PromotionDetails.RemoveRange(promotionDetails);
                    }

                    // Remove associated product sizes
                    if (product.ProductSizes != null && product.ProductSizes.Any())
                    {
                        _context.ProductSizes.RemoveRange(product.ProductSizes);
                    }

                    // Delete image from Cloudinary
                    if (!string.IsNullOrEmpty(product.ImagePublicId))
                    {
                        try
                        {
                            await _cloudinaryService.DeleteImageAsync(product.ImagePublicId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete image from Cloudinary: {ex.Message}");
                        }
                    }

                    // Remove the product
                    _context.Products.Remove(product);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = $"Đã xóa sản phẩm '{product.ProductName}' thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ Helper method mới để xử lý promotion
        private async Task HandlePromotionUpdate(Guid productId, Guid? promotionId)
        {
            // Remove existing promotion details
            var existingPromotionDetails = await _context.PromotionDetails
                .Where(pd => pd.ProductID == productId)
                .ToListAsync();

            if (existingPromotionDetails.Any())
            {
                _context.PromotionDetails.RemoveRange(existingPromotionDetails);
            }

            // Add new promotion detail if promotionId is provided
            if (promotionId.HasValue && promotionId.Value != Guid.Empty)
            {
                var newPromotionDetail = new PromotionDetails
                {
                    PromotionDetailsID = Guid.NewGuid(),
                    PromotionID = promotionId.Value,
                    ProductID = productId,
                };
                _context.PromotionDetails.Add(newPromotionDetail);
            }
        }

        // Helper method để load promotions - CẬP NHẬT
        private async Task LoadPromotionsForView(Guid? selectedPromotionId = null)
        {
            try
            {
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive &&
                               p.StartDate <= DateTime.Now &&
                               p.EndDate >= DateTime.Now)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                ViewData["PromotionID"] = new SelectList(
                    promotions ?? new List<Promotion>(),
                    "PromotionID",
                    "Name",
                    selectedPromotionId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading promotions: {ex.Message}");
                ViewData["PromotionID"] = new SelectList(new List<Promotion>(), "PromotionID", "Name");
            }
        }

        // Helper methods (unchanged)
        private async Task HandleSizeUpdates(Product existingProduct,
            List<Guid>? deletedSizeIds, List<Guid>? existingSizeIds,
            List<string>? existingSizes, List<int>? existingQuantities,
            List<string>? newSizes, List<int>? newQuantities)
        {
            // Handle Size Deletions
            if (deletedSizeIds != null && deletedSizeIds.Any())
            {
                var sizesToDelete = existingProduct.ProductSizes
                    .Where(ps => deletedSizeIds.Contains(ps.SizeID))
                    .ToList();
                _context.ProductSizes.RemoveRange(sizesToDelete);
            }

            // Update existing sizes
            if (existingSizeIds != null && existingSizes != null && existingQuantities != null)
            {
                for (int i = 0; i < existingSizeIds.Count; i++)
                {
                    if (i < existingSizes.Count && i < existingQuantities.Count)
                    {
                        var sizeToUpdate = existingProduct.ProductSizes
                            .FirstOrDefault(ps => ps.SizeID == existingSizeIds[i]);

                        if (sizeToUpdate != null && !string.IsNullOrEmpty(existingSizes[i]))
                        {
                            sizeToUpdate.Size = existingSizes[i];
                            sizeToUpdate.Quantity = existingQuantities[i];
                        }
                    }
                }
            }

            // Add new sizes
            if (newSizes != null && newQuantities != null)
            {
                for (int i = 0; i < newSizes.Count; i++)
                {
                    if (i < newQuantities.Count &&
                        !string.IsNullOrEmpty(newSizes[i]) &&
                        newQuantities[i] > 0)
                    {
                        var newSize = new ProductSize
                        {
                            SizeID = Guid.NewGuid(),
                            ProductID = existingProduct.ProductID,
                            Size = newSizes[i],
                            Quantity = newQuantities[i]
                        };
                        _context.ProductSizes.Add(newSize);
                    }
                }
            }
        }

        private void CleanupModelStateForSizes()
        {
            var newQuantitiesKeys = ModelState.Keys.Where(k => k.StartsWith("newQuantities")).ToList();
            var newSizesKeys = ModelState.Keys.Where(k => k.StartsWith("newSizes")).ToList();

            foreach (var key in newQuantitiesKeys.Concat(newSizesKeys))
            {
                var values = Request.Form[key].Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                if (!values.Any())
                {
                    ModelState.Remove(key);
                }
            }
        }

        private bool ProductExists(Guid id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}

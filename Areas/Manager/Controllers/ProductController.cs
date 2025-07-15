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

        // GET: Manager/Product/Create - SỬA LỖI
        public async Task<IActionResult> Create()
        {
            try
            {
                // Lấy danh sách promotions an toàn
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive) // Chỉ lấy promotion đang active
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                // Tạo SelectList với tên trường đúng và xử lý trường hợp rỗng
                ViewData["PromotionID"] = new SelectList(
                    promotions ?? new List<Promotion>(),
                    "PromotionID",
                    "Name" // SỬA: từ "PromotionName" thành "Name"
                );

                return View();
            }
            catch (Exception ex)
            {
                // Log error và tạo SelectList rỗng
                Console.WriteLine($"Error loading promotions: {ex.Message}");
                ViewData["PromotionID"] = new SelectList(new List<Promotion>(), "PromotionID", "Name");
                TempData["Warning"] = "Không thể tải danh sách khuyến mãi. Vui lòng thử lại.";
                return View();
            }
        }

        // POST: Manager/Product/Create - SỬA LỖI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductName,Price,Barcode,Supplier,Status,PromotionID")] Product product,
            IFormFile? imageFile, List<string> sizes, List<int> quantities)
        {
            if (ModelState.IsValid)
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
                        await LoadPromotionsForView(product.PromotionID); // SỬA: Dùng method helper
                        return View(product);
                    }
                }

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

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // SỬA: Load lại promotions khi validation fail
            await LoadPromotionsForView(product.PromotionID);
            return View(product);
        }

        // GET: Manager/Product/Edit/5 - SỬA LỖI
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null) return NotFound();

            await LoadPromotionsForView(product.PromotionID); // SỬA: Dùng method helper
            return View(product);
        }

        // PUT: Manager/Product/Edit/5 - SỬA LỖI
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id,
            [Bind("ProductID,ProductName,Price,Barcode,Supplier,Status,PromotionID,Image,ImagePublicId")]
            Product product, IFormFile? imageFile,
            List<Guid>? existingSizeIds, List<string>? existingSizes, List<int>? existingQuantities,
            List<string>? newSizes, List<int>? newQuantities,
            List<Guid>? deletedSizeIds)
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

                    // Store old image info for potential cleanup
                    var oldImagePublicId = existingProduct.ImagePublicId;

                    // Update basic product info
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Price = product.Price;
                    existingProduct.Barcode = product.Barcode;
                    existingProduct.Supplier = product.Supplier;
                    existingProduct.Status = product.Status;
                    existingProduct.PromotionID = product.PromotionID;

                    // Handle image upload to Cloudinary
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                            existingProduct.Image = uploadResult.SecureUrl.ToString();
                            existingProduct.ImagePublicId = uploadResult.PublicId;

                            // Delete old image from Cloudinary (background task)
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

                            await LoadPromotionsForView(product.PromotionID); // SỬA: Dùng method helper
                            return View(existingProduct);
                        }
                    }

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

            await LoadPromotionsForView(product.PromotionID); // SỬA: Dùng method helper
            return View(productWithSizes ?? product);
        }

        // THÊM: Helper method để load promotions an toàn
        private async Task LoadPromotionsForView(Guid? selectedPromotionId = null)
        {
            try
            {
                var promotions = await _context.Promotions
                    .Where(p => p.IsActive)
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

        // Các method khác giữ nguyên...

        // GET: Manager/Product
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion)
                .ToListAsync();
            return View(products);
        }

        // GET: Manager/Product/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Manager/Product/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion)
                .FirstOrDefaultAsync(m => m.ProductID == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // DELETE: Manager/Product/Delete/5
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
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
                            // Log error but continue with product deletion
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

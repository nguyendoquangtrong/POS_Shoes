using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // GET: Manager/Product/Index
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .ToListAsync();
            return View(products);
        }

        // GET: Manager/Product/Create
        public IActionResult Create() => View();

        // POST: Manager/Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ProductName,Price,Barcode,Supplier,Status")] Product product,
            IFormFile? imageFile, List<string> sizes, List<int> quantities)
        {
            if (!ModelState.IsValid) return View(product);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                product.ProductID = Guid.NewGuid();

                // Upload image
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
                        return View(product);
                    }
                }

                _context.Add(product);

                for (int i = 0; i < sizes.Count; i++)
                {
                    if (!string.IsNullOrEmpty(sizes[i]) && quantities[i] > 0)
                    {
                        _context.ProductSizes.Add(new ProductSize
                        {
                            SizeID = Guid.NewGuid(),
                            ProductID = product.ProductID,
                            Size = sizes[i],
                            Quantity = quantities[i]
                        });
                    }
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
            }
            return View(product);
        }

        // GET: Manager/Product/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            return product == null ? NotFound() : View(product);
        }

        // POST: Manager/Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            [Bind("ProductID,ProductName,Price,Barcode,Supplier,Status,Image,ImagePublicId")] Product product,
            IFormFile? imageFile,
            List<Guid>? existingSizeIds, List<string>? existingSizes, List<int>? existingQuantities,
            List<string>? newSizes, List<int>? newQuantities, List<Guid>? deletedSizeIds)
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

                    // Update basic info
                    existingProduct.ProductName = product.ProductName;
                    existingProduct.Price = product.Price;
                    existingProduct.Barcode = product.Barcode;
                    existingProduct.Supplier = product.Supplier;
                    existingProduct.Status = product.Status;

                    // Image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        try
                        {
                            var uploadResult = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                            existingProduct.Image = uploadResult.SecureUrl.ToString();
                            existingProduct.ImagePublicId = uploadResult.PublicId;

                            if (!string.IsNullOrEmpty(oldImagePublicId))
                            {
                                _ = Task.Run(async () =>
                                {
                                    try { await _cloudinaryService.DeleteImageAsync(oldImagePublicId); }
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
                            return View(existingProduct);
                        }
                    }

                    await HandleSizeUpdates(
                        existingProduct,
                        deletedSizeIds, existingSizeIds,
                        existingSizes, existingQuantities,
                        newSizes, newQuantities
                    );

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = $"Lỗi cập nhật: {ex.Message}";
                }
            }

            var productWithSizes = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            return View(productWithSizes ?? product);
        }

        // GET: Manager/Product/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            return product == null ? NotFound() : View(product);
        }

        // GET: Manager/Product/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            return product == null ? NotFound() : View(product);
        }

        // POST: Manager/Product/Delete/5
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
                    if (product.ProductSizes != null && product.ProductSizes.Any())
                        _context.ProductSizes.RemoveRange(product.ProductSizes);

                    if (!string.IsNullOrEmpty(product.ImagePublicId))
                    {
                        try { await _cloudinaryService.DeleteImageAsync(product.ImagePublicId); }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete image from Cloudinary: {ex.Message}");
                        }
                    }

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

        // --- Helper Methods ---

        private async Task HandleSizeUpdates(
            Product existingProduct,
            List<Guid>? deletedSizeIds,
            List<Guid>? existingSizeIds, List<string>? existingSizes, List<int>? existingQuantities,
            List<string>? newSizes, List<int>? newQuantities)
        {
            // Delete sizes
            if (deletedSizeIds != null && deletedSizeIds.Any())
            {
                var toDelete = existingProduct.ProductSizes.Where(ps => deletedSizeIds.Contains(ps.SizeID)).ToList();
                _context.ProductSizes.RemoveRange(toDelete);
            }

            // Update existing sizes
            if (existingSizeIds != null && existingSizes != null && existingQuantities != null)
            {
                for (int i = 0; i < existingSizeIds.Count; i++)
                {
                    if (i < existingSizes.Count && i < existingQuantities.Count)
                    {
                        var sizeToUpdate = existingProduct.ProductSizes.FirstOrDefault(ps => ps.SizeID == existingSizeIds[i]);
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
                    if (i < newQuantities.Count && !string.IsNullOrEmpty(newSizes[i]) && newQuantities[i] > 0)
                    {
                        _context.ProductSizes.Add(new ProductSize
                        {
                            SizeID = Guid.NewGuid(),
                            ProductID = existingProduct.ProductID,
                            Size = newSizes[i],
                            Quantity = newQuantities[i]
                        });
                    }
                }
            }
        }

        private void CleanupModelStateForSizes()
        {
            var keys = ModelState.Keys
                .Where(k => k.StartsWith("newQuantities") || k.StartsWith("newSizes"))
                .ToList();

            foreach (var key in keys)
            {
                var values = Request.Form[key].Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                if (!values.Any())
                    ModelState.Remove(key);
            }
        }
    }
}

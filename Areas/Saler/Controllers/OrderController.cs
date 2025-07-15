using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class OrderController : SalerBaseController
    {
        public OrderController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Saler/Order/Create
        public async Task<IActionResult> Create()
        {
            var model = new CreateOrderViewModel();

            await LoadCreateViewData();
            return View(model);
        }

        // POST: Saler/Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (ModelState.IsValid && model.OrderItems.Any())
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Validate product availability before creating order
                    var validationResult = await ValidateOrderItems(model.OrderItems);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("", validationResult.ErrorMessage);
                        await LoadCreateViewData();
                        return View(model);
                    }

                    // Create new order
                    var order = new Order
                    {
                        OrderID = Guid.NewGuid(),
                        OrderDate = DateTime.Now,
                        TotalPrice = (double)model.TotalAmount,
                        Status = "Completed",
                        CustomerID = model.CustomerID,
                        UserID = GetCurrentUserGuid(),
                        OrderDetails = new List<OrderDetail>()
                    };

                    // Add order details and update inventory
                    foreach (var item in model.OrderItems)
                    {
                        var orderDetail = new OrderDetail
                        {
                            OrderDetailID = Guid.NewGuid(),
                            OrderID = order.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Size = item.Size,
                        };
                        order.OrderDetails.Add(orderDetail);

                        // Update product quantity
                        await UpdateProductQuantity(item.ProductID, item.Size, item.Quantity);
                    }

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đơn hàng đã được tạo thành công!";
                    return RedirectToAction("Receipt", new { id = order.OrderID });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo đơn hàng: " + ex.Message);
                }
            }

            // Reload data if validation fails
            await LoadCreateViewData();
            return View(model);
        }

        // GET: Saler/Order/Receipt/5
        public async Task<IActionResult> Receipt(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            // Load promotion details for receipt display
            var productIds = order.OrderDetails.Select(od => od.ProductID).ToList();
            var promotionDetails = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => productIds.Contains(pd.ProductID))
                .ToListAsync();

            ViewBag.PromotionDetails = promotionDetails;

            return View(order);
        }

        // ✅ API endpoint với thông tin promotion từ PromotionDetails - CẬP NHẬT
        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return BadRequest("Mã vạch không hợp lệ");
            }

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.Status == "Active");

            if (product == null)
            {
                return NotFound();
            }

            // Lấy promotion information từ PromotionDetails
            var promotionDetail = await GetActivePromotionForProduct(product.ProductID);
            var discountPercent = promotionDetail?.Promotion?.discount ?? 0;
            var discountedPrice = CalculateDiscountedPrice(product.Price, discountPercent);

            return Json(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                price = product.Price,
                discountedPrice = discountedPrice,
                originalPrice = product.Price,
                // ✅ Thông tin promotion từ PromotionDetails
                discount = discountPercent,
                promotionId = promotionDetail?.Promotion?.PromotionID,
                promotionName = promotionDetail?.Promotion?.Name,
                hasPromotion = discountPercent > 0,
                sizes = product.ProductSizes.Where(ps => ps.Quantity > 0).Select(ps => new
                {
                    size = ps.Size,
                    quantity = ps.Quantity
                })
            });
        }

        // GET: Saler/Order/Index
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Where(o => o.UserID == GetCurrentUserGuid())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ✅ API: Get product details for POS - CẬP NHẬT
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(Guid productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.Status == "Active");

            if (product == null)
            {
                return NotFound();
            }

            var promotionDetail = await GetActivePromotionForProduct(productId);
            var discountPercent = promotionDetail?.Promotion?.discount ?? 0;
            var discountedPrice = CalculateDiscountedPrice(product.Price, discountPercent);

            return Json(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                price = product.Price,
                discountedPrice = discountedPrice,
                originalPrice = product.Price,
                image = product.Image,
                discount = discountPercent,
                promotionId = promotionDetail?.Promotion?.PromotionID,
                promotionName = promotionDetail?.Promotion?.Name,
                hasPromotion = discountPercent > 0,
                sizes = product.ProductSizes.Where(ps => ps.Quantity > 0).Select(ps => new
                {
                    size = ps.Size,
                    quantity = ps.Quantity
                }).ToList()
            });
        }

        private async Task LoadCreateViewData()
        {
            // Load products
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            // Load active promotion details
            var promotionDetails = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => pd.Promotion.IsActive &&
                           pd.Promotion.Status == "Approved").ToListAsync();

            var productsForJs = products.Select(p =>
            {
                var promotion = promotionDetails.FirstOrDefault(pd => pd.ProductID == p.ProductID)?.Promotion;
                var discountPercent = promotion?.discount ?? 0;
                var discountedPrice = CalculateDiscountedPrice(p.Price, discountPercent);

                return new
                {
                    productID = p.ProductID,
                    productName = p.ProductName,
                    price = p.Price,
                    discountedPrice = discountedPrice,
                    originalPrice = p.Price,
                    image = p.Image,
                    quantity = p.Quantity,
                    barcode = p.Barcode,
                    // ✅ Thông tin promotion từ PromotionDetails
                    promotion = promotion != null ? new
                    {
                        promotionID = promotion.PromotionID,
                        name = promotion.Name,
                        discount = promotion.discount,
                        isActive = promotion.IsActive,
                        startDate = promotion.StartDate,
                        endDate = promotion.EndDate
                    } : null,
                    // ✅ Thông tin giá giảm
                    hasPromotion = discountPercent > 0,
                    discountPercent = discountPercent,
                    savedAmount = p.Price - discountedPrice,
                    productSizes = p.ProductSizes.Where(ps => ps.Quantity > 0).Select(ps => new
                    {
                        size = ps.Size,
                        quantity = ps.Quantity
                    }).ToList()
                };
            }).ToList();

            // Load customers
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Set ViewBag data
            ViewBag.Products = products;
            ViewBag.ProductsForJs = productsForJs;
            ViewBag.PromotionDetails = promotionDetails;
            ViewBag.Customers = customers;
        }

        // ✅ Helper method để tính giá sau khi giảm - MỚI
        private decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountPercent)
        {
            if (discountPercent <= 0) return originalPrice;

            var discountAmount = originalPrice * (discountPercent / 100);
            var discountedPrice = originalPrice - discountAmount;

            return Math.Round(discountedPrice, 0); // Làm tròn về số nguyên
        }

        // ✅ Helper method để tính số tiền tiết kiệm - MỚI
        private decimal CalculateSavedAmount(decimal originalPrice, decimal discountPercent)
        {
            if (discountPercent <= 0) return 0;

            var savedAmount = originalPrice * (discountPercent / 100);
            return Math.Round(savedAmount, 0);
        }

        // Helper method để lấy promotion đang active cho sản phẩm
        private async Task<PromotionDetails> GetActivePromotionForProduct(Guid productId)
        {
            return await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .FirstOrDefaultAsync(pd => pd.ProductID == productId &&
                                         pd.Promotion.IsActive &&
                                         pd.Promotion.Status == "Approved" &&
                                         pd.Promotion.StartDate <= DateTime.Now &&
                                         pd.Promotion.EndDate >= DateTime.Now);
        }

        // ✅ Helper method để validate order items - CẬP NHẬT
        private async Task<(bool IsValid, string ErrorMessage)> ValidateOrderItems(List<OrderItemViewModel> orderItems)
        {
            foreach (var item in orderItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductID == item.ProductID && ps.Size == item.Size);

                if (productSize == null)
                {
                    return (false, $"Không tìm thấy sản phẩm {item.ProductName} size {item.Size}");
                }

                if (productSize.Quantity < item.Quantity)
                {
                    return (false, $"Sản phẩm {item.ProductName} size {item.Size} chỉ còn {productSize.Quantity} trong kho");
                }
            }

            return (true, string.Empty);
        }

        // Helper method để update product quantity
        private async Task UpdateProductQuantity(Guid productId, string size, int quantity)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductID == productId && ps.Size == size);

            if (productSize != null)
            {
                productSize.Quantity -= quantity;

                // Ensure quantity doesn't go below 0
                if (productSize.Quantity < 0)
                {
                    productSize.Quantity = 0;
                }
            }
        }

        // ✅ API: Get promotion summary - MỚI
        [HttpGet]
        public async Task<IActionResult> GetPromotionSummary()
        {
            var activePromotions = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Include(pd => pd.Product)
                .Where(pd => pd.Promotion.IsActive &&
                           pd.Promotion.Status == "Approved" &&
                           pd.Promotion.StartDate <= DateTime.Now &&
                           pd.Promotion.EndDate >= DateTime.Now)
                .GroupBy(pd => pd.Promotion)
                .Select(g => new
                {
                    promotionId = g.Key.PromotionID,
                    name = g.Key.Name,
                    discount = g.Key.discount,
                    startDate = g.Key.StartDate,
                    endDate = g.Key.EndDate,
                    productCount = g.Count(),
                    products = g.Select(pd => new
                    {
                        productId = pd.ProductID,
                        productName = pd.Product.ProductName,
                        originalPrice = pd.Product.Price,
                        discountedPrice = pd.Product.Price * (1 - g.Key.discount / 100)
                    }).ToList()
                })
                .ToListAsync();

            return Json(activePromotions);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class OrderController : SalerBaseController
    {
        public OrderController(ApplicationDbContext context) : base(context) { }

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
                    var validationResult = await ValidateOrderItems(model.OrderItems);
                    if (!validationResult.IsValid)
                    {
                        ModelState.AddModelError("", validationResult.ErrorMessage);
                        await LoadCreateViewData();
                        return View(model);
                    }

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

                    foreach (var item in model.OrderItems)
                    {
                        order.OrderDetails.Add(new OrderDetail
                        {
                            OrderDetailID = Guid.NewGuid(),
                            OrderID = order.OrderID,
                            ProductID = item.ProductID,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Size = item.Size,
                        });

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

            await LoadCreateViewData();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> FindCustomerByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest("Vui lòng nhập số điện thoại");

            // Có thể tìm chính xác hoặc like (đổi .Where nếu bạn muốn)
            var customer = await _context.Customers
                .Where(c => c.Phone == phone)
                .Select(c => new
                {
                    customerID = c.CustomerID,
                    name = c.Name,
                    phone = c.Phone,
                    email = c.Email
                })
                .FirstOrDefaultAsync();

            if (customer == null)
                return NotFound("Không tìm thấy khách hàng");

            return Json(customer);
        }


        // GET: Saler/Order/Receipt/5
        public async Task<IActionResult> Receipt(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
                return NotFound();

            var productIds = order.OrderDetails.Select(od => od.ProductID).ToList();
            var promotionDetails = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => productIds.Contains(pd.ProductID))
                .ToListAsync();

            ViewBag.PromotionDetails = promotionDetails;
            return View(order);
        }

        #region API Endpoints

        // API: Lấy sản phẩm theo barcode (có promotion)
        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return BadRequest("Mã vạch không hợp lệ");

            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.Status == "Active");

            if (product == null)
                return NotFound();

            var promotionDetail = await GetActivePromotionForProduct(product.ProductID);
            var discountPercent = promotionDetail?.Promotion?.discount ?? 0;
            var discountedPrice = CalculateDiscountedPrice(product.Price, discountPercent);

            return Json(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                price = product.Price,
                discountedPrice,
                originalPrice = product.Price,
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

        // API: Lấy chi tiết sản phẩm cho POS
        [HttpGet]
        public async Task<IActionResult> GetProductDetails(Guid productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .FirstOrDefaultAsync(p => p.ProductID == productId && p.Status == "Active");

            if (product == null)
                return NotFound();

            var promotionDetail = await GetActivePromotionForProduct(productId);
            var discountPercent = promotionDetail?.Promotion?.discount ?? 0;
            var discountedPrice = CalculateDiscountedPrice(product.Price, discountPercent);

            return Json(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                price = product.Price,
                discountedPrice,
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

        // API: Get promotion summary
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

        #endregion

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

        #region Helpers

        private async Task LoadCreateViewData()
        {
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .Where(p => p.Status == "Active")
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var promotionDetails = await _context.PromotionDetails
                .Include(pd => pd.Promotion)
                .Where(pd => pd.Promotion.IsActive && pd.Promotion.Status == "Approved")
                .ToListAsync();

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
                    discountedPrice,
                    originalPrice = p.Price,
                    image = p.Image,
                    quantity = p.Quantity,
                    barcode = p.Barcode,
                    promotion = promotion != null ? new
                    {
                        promotionID = promotion.PromotionID,
                        name = promotion.Name,
                        discount = promotion.discount,
                        isActive = promotion.IsActive,
                        startDate = promotion.StartDate,
                        endDate = promotion.EndDate
                    } : null,
                    hasPromotion = discountPercent > 0,
                    discountPercent,
                    savedAmount = p.Price - discountedPrice,
                    productSizes = p.ProductSizes.Where(ps => ps.Quantity > 0)
                        .Select(ps => new { size = ps.Size, quantity = ps.Quantity }).ToList()
                };
            }).ToList();

            var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();

            ViewBag.Products = products;
            ViewBag.ProductsForJs = productsForJs;
            ViewBag.PromotionDetails = promotionDetails;
            ViewBag.Customers = customers;
        }

        // Return discounted price rounded to integer
        private decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountPercent)
        {
            if (discountPercent <= 0) return originalPrice;
            var discountAmount = originalPrice * (discountPercent / 100);
            return Math.Round(originalPrice - discountAmount, 0);
        }

        // Return saved amount (rounded)
        private decimal CalculateSavedAmount(decimal originalPrice, decimal discountPercent)
        {
            if (discountPercent <= 0) return 0;
            return Math.Round(originalPrice * (discountPercent / 100), 0);
        }

        // Get active promotion for a product
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

        // Validate stock before order creation
        private async Task<(bool IsValid, string ErrorMessage)> ValidateOrderItems(List<OrderItemViewModel> orderItems)
        {
            foreach (var item in orderItems)
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductID == item.ProductID && ps.Size == item.Size);
                if (productSize == null)
                    return (false, $"Không tìm thấy sản phẩm {item.ProductName} size {item.Size}");

                if (productSize.Quantity < item.Quantity)
                    return (false, $"Sản phẩm {item.ProductName} size {item.Size} chỉ còn {productSize.Quantity} trong kho");
            }
            return (true, string.Empty);
        }

        // Update inventory after order
        private async Task UpdateProductQuantity(Guid productId, string size, int quantity)
        {
            var productSize = await _context.ProductSizes.FirstOrDefaultAsync(ps => ps.ProductID == productId && ps.Size == size);
            if (productSize != null)
            {
                productSize.Quantity -= quantity;
                if (productSize.Quantity < 0)
                    productSize.Quantity = 0;
            }
        }

        #endregion
    }
}

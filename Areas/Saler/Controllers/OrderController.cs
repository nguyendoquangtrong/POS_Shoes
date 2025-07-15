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

        // Areas/Saler/Controllers/OrderController.cs
        public async Task<IActionResult> Create()
        {
            var model = new CreateOrderViewModel();

            // ✅ Load products với Promotion
            var products = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion) // ⭐ Thêm dòng này
                .Where(p => p.Status == "Active")
                .ToListAsync();

            // ✅ Create DTO với thông tin promotion
            var productsForJs = products.Select(p => new
            {
                productID = p.ProductID,
                productName = p.ProductName,
                price = p.Price,
                image = p.Image,
                quantity = p.Quantity,
                // ⭐ Thêm thông tin promotion
                promotion = p.Promotion != null ? new
                {
                    promotionID = p.Promotion.PromotionID,
                    discount = p.Promotion.discount,
                    isActive = p.Promotion.IsActive,
                    startDate = p.Promotion.StartDate,
                    endDate = p.Promotion.EndDate
                } : null,
                productSizes = p.ProductSizes.Select(ps => new
                {
                    size = ps.Size,
                    quantity = ps.Quantity
                }).ToList()
            }).ToList();

            ViewBag.Products = products; // For Razor view
            ViewBag.ProductsForJs = productsForJs; // For JavaScript

            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

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

                    // Add order details
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
                        var productSize = await _context.ProductSizes
                            .FirstOrDefaultAsync(ps => ps.ProductID == item.ProductID && ps.Size == item.Size);

                        if (productSize != null)
                        {
                            productSize.Quantity -= item.Quantity;
                        }
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

            // ✅ Reload data với Promotion nếu validation fails
            ViewBag.Products = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion) // ⭐ Thêm dòng này
                .Where(p => p.Status == "Active")
                .ToListAsync();

            ViewBag.Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

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

            return View(order);
        }

        // ✅ API endpoint với thông tin promotion
        [HttpGet]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            var product = await _context.Products
                .Include(p => p.ProductSizes)
                .Include(p => p.Promotion) // ⭐ Thêm dòng này
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.Status == "Active");

            if (product == null)
            {
                return NotFound();
            }

            return Json(new
            {
                productID = product.ProductID,
                productName = product.ProductName,
                price = product.Price,
                // ⭐ Thêm thông tin promotion
                discount = product.Promotion?.discount ?? 0,
                promotionId = product.Promotion?.PromotionID,
                sizes = product.ProductSizes.Select(ps => new
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
    }
}

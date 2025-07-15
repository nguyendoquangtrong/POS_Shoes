using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class ReturnReceiptController : SalerBaseController
    {
        public ReturnReceiptController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Saler/ReturnReceipt
        public async Task<IActionResult> Index()
        {
            var returnReceipts = await _context.ReturnReceipts
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.User)
                .Include(r => r.ReturnReceiptDetails)
                    .ThenInclude(rd => rd.Product)
                .Where(r => r.UserID == GetCurrentUserGuid())
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            return View(returnReceipts);
        }

        // GET: Saler/ReturnReceipt/SelectOrder
        public async Task<IActionResult> SelectOrder()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.UserID == GetCurrentUserGuid() && o.Status == "Completed")
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Saler/ReturnReceipt/Create/5
        public async Task<IActionResult> Create(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var model = new CreateReturnReceiptViewModel
            {
                OrderID = order.OrderID,
                OrderCode = order.OrderID.ToString().Substring(0, 8).ToUpper(),
                OrderDate = order.OrderDate,
                CustomerName = order.Customer?.Name ?? "Khách lẻ",
                CustomerPhone = order.Customer?.Phone ?? "",
                OrderItems = order.OrderDetails.Select(od => new OrderItemViewModel
                {
                    ProductID = od.ProductID,
                    ProductName = od.Product.ProductName,
                    Size = GetProductSize(od.ProductID), // You'll need to implement this
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                }).ToList()
            };

            return View(model);
        }

        // POST: Saler/ReturnReceipt/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReturnReceiptViewModel model)
        {
            ModelState.Remove("OrderCode");
            ModelState.Remove("OrderDate");
            ModelState.Remove("CustomerName");
            ModelState.Remove("CustomerPhone");
            ModelState.Remove("OrderItems");
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create return receipt
                    var returnReceipt = new ReturnReceipt
                    {
                        ReturnReceiptID = Guid.NewGuid(),
                        Date = DateTime.Now,
                        Reason = model.Reason,
                        Status = "Progressing",
                        OrderID = model.OrderID,
                        UserID = GetCurrentUserGuid(),
                        TotalRefundAmount = model.TotalRefundAmount,
                        ReturnReceiptDetails = new List<ReturnReceiptDetail>()
                    };

                    // Add return receipt details
                    foreach (var item in model.ReturnItems.Where(r => r.ReturnQuantity > 0))
                    {
                        var detail = new ReturnReceiptDetail
                        {
                            ReturnReceiptDetailID = Guid.NewGuid(),
                            ReturnReceiptID = returnReceipt.ReturnReceiptID,
                            ProductID = item.ProductID,
                            Size = item.Size,
                            Quantity = item.ReturnQuantity,
                            UnitPrice = item.UnitPrice,
                            RefundAmount = item.RefundAmount,
                            Note = item.Note
                        };
                        returnReceipt.ReturnReceiptDetails.Add(detail);

                        // Update product stock (return to inventory)
                        var productSize = await _context.ProductSizes
                            .FirstOrDefaultAsync(ps => ps.ProductID == item.ProductID && ps.Size == item.Size);

                        if (productSize != null)
                        {
                            productSize.Quantity += item.ReturnQuantity;
                        }
                    }

                    _context.ReturnReceipts.Add(returnReceipt);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Phiếu trả hàng đã được tạo thành công!";
                    return RedirectToAction("Details", new { id = returnReceipt.ReturnReceiptID });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo phiếu trả hàng: " + ex.Message);
                }
            }

            // Reload order data if validation fails
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == model.OrderID);

            if (order != null)
            {
                model.OrderCode = order.OrderID.ToString().Substring(0, 8).ToUpper();
                model.OrderDate = order.OrderDate;
                model.CustomerName = order.Customer?.Name ?? "Khách lẻ";
                model.CustomerPhone = order.Customer?.Phone ?? "";
                model.OrderItems = order.OrderDetails.Select(od => new OrderItemViewModel
                {
                    ProductID = od.ProductID,
                    ProductName = od.Product.ProductName,
                    Size = GetProductSize(od.ProductID),
                    Quantity = od.Quantity,
                    UnitPrice = od.UnitPrice,
                }).ToList();
            }

            return View(model);
        }

        // GET: Saler/ReturnReceipt/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var returnReceipt = await _context.ReturnReceipts
                .Include(r => r.Order)
                    .ThenInclude(o => o.Customer)
                .Include(r => r.User)
                .Include(r => r.ReturnReceiptDetails)
                    .ThenInclude(rd => rd.Product)
                .FirstOrDefaultAsync(r => r.ReturnReceiptID == id);

            if (returnReceipt == null)
            {
                return NotFound();
            }

            return View(returnReceipt);
        }

        // Helper method to get product size (you'll need to implement this based on your business logic)
        private string GetProductSize(Guid productId)
        {
            // This is a placeholder - you'll need to implement based on your OrderDetail structure
            // If you store size information in OrderDetail, modify the OrderDetail model
            return "42"; // Default size
        }

        // API: Search orders by customer phone or order code
        [HttpGet]
        public async Task<IActionResult> SearchOrders(string searchTerm)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.UserID == GetCurrentUserGuid() &&
                           o.Status == "Completed" &&
                           (o.Customer != null && o.Customer.Phone.Contains(searchTerm) ||
                            o.OrderID.ToString().Contains(searchTerm)))
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    orderID = o.OrderID,
                    orderCode = o.OrderID.ToString().Substring(0, 8).ToUpper(),
                    orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    customerName = o.Customer != null ? o.Customer.Name : "Khách lẻ",
                    customerPhone = o.Customer != null ? o.Customer.Phone : "",
                    totalPrice = o.TotalPrice
                })
                .ToListAsync();

            return Json(orders);
        }
    }
}

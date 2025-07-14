// Areas/Saler/Controllers/CustomerController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class CustomerController : SalerBaseController
    {
        public CustomerController(ApplicationDbContext context) : base(context)
        {
        }
        // API endpoint to search customer by phone
        [HttpGet]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return Json(new { success = false, message = "Vui lòng nhập số điện thoại" });
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy khách hàng" });
            }

            return Json(new
            {
                success = true,
                customer = new
                {
                    id = customer.CustomerID,
                    name = customer.Name,
                    phone = customer.Phone,
                    email = customer.Email
                }
            });
        }


        // GET: Saler/Customer/Create
        public IActionResult Create()
        {
            return View(new CreateCustomerViewModel());
        }

        // POST: Saler/Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCustomerViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if phone already exists
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == model.Phone);

                if (existingCustomer != null)
                {
                    // If it's an AJAX request, return JSON
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Số điện thoại này đã được sử dụng" });
                    }

                    ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng");
                    return View(model);
                }

                var customer = new Customer
                {
                    CustomerID = Guid.NewGuid(),
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // If it's an AJAX request, return JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        customerID = customer.CustomerID,
                        name = customer.Name,
                        phone = customer.Phone
                    });
                }

                TempData["SuccessMessage"] = "Khách hàng đã được tạo thành công!";
                return RedirectToAction("Create");
            }

            // If it's an AJAX request, return validation errors
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            return View(model);
        }


        // GET: Saler/Customer/Index
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(customers);
        }

        // API endpoint to search customers
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var customers = await _context.Customers
                .Where(c => c.Name.Contains(term) || c.Phone.Contains(term))
                .Take(10)
                .Select(c => new
                {
                    id = c.CustomerID,
                    name = c.Name,
                    phone = c.Phone
                })
                .ToListAsync();

            return Json(customers);
        }
    }
}

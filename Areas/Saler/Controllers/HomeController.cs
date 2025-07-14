// Areas/Saler/Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Saler.Controllers
{
    public class HomeController : SalerBaseController
    {
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            var userGuid = GetCurrentUserGuid();

            ViewBag.TodayOrders = await _context.Orders
                .Where(o => o.UserID == userGuid && o.OrderDate.Date == DateTime.Today)
                .CountAsync();

            ViewBag.TodayRevenue = await _context.Orders
                .Where(o => o.UserID == userGuid && o.OrderDate.Date == DateTime.Today)
                .SumAsync(o => o.TotalPrice);

            ViewBag.TotalCustomers = await _context.Customers.CountAsync();

            return View();
        }
    }
}

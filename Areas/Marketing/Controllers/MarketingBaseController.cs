// Areas/Marketing/Controllers/MarketingBaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Marketing.Controllers
{
    [Area("Marketing")]
    [Authorize(Roles = "Marketing")]
    public abstract class MarketingBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected MarketingBaseController(ApplicationDbContext ctx) => _context = ctx;

        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            ViewData["CurrentArea"] = "Marketing";
            ViewData["UserRole"] = "Nhân viên Marketing";
            ViewData["WelcomeMessage"] = $"Chào mừng, {User.Identity?.Name}!";
            base.OnActionExecuting(ctx);
        }
    }
}

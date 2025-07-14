// Areas/Accountant/Controllers/AccountantBaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Accountant.Controllers
{
    [Area("Accountant")]
    [Authorize(Roles = "Accountant")]
    public class AccountantBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public AccountantBaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewData["CurrentArea"] = "Accountant";
            ViewData["UserRole"] = "Kế toán";
            ViewData["WelcomeMessage"] = $"Chào mừng, {User.Identity.Name}!";

            base.OnActionExecuting(context);
        }

        protected Guid GetCurrentUserGuid()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userGuid))
            {
                return userGuid;
            }
            return Guid.Empty;
        }

        protected IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }
    }
}

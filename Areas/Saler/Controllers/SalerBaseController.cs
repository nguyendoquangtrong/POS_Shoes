// Areas/Saler/Controllers/SalerBaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Saler.Controllers
{
    [Area("Saler")]
    [Authorize(Roles = "Saler")]
    public class SalerBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public SalerBaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Logic chung cho Saler area
            ViewData["CurrentArea"] = "Saler";
            ViewData["UserRole"] = "Nhân viên bán hàng";
            ViewData["WelcomeMessage"] = $"Chào mừng, {User.Identity.Name}!";

            base.OnActionExecuting(context);
        }
        protected Guid GetCurrentUserGuid()
        {
            // Example: if user id is stored as a claim
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userGuid))
            {
                return userGuid;
            }
            // Return Guid.Empty or throw if not found
            return Guid.Empty;
        }


        protected IActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }

    }
}

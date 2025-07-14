// Areas/Master/Controllers/MasterBaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using POS_Shoes.Models.Data;
using System.Security.Claims;

namespace POS_Shoes.Areas.Master.Controllers
{
    [Area("Master")]
    [Authorize(Roles = "Master")]
    public class MasterBaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public MasterBaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewData["CurrentArea"] = "Master";
            ViewData["UserRole"] = "Master Admin";
            ViewData["WelcomeMessage"] = $"Chào mừng Master, {User.Identity.Name}!";

            base.OnActionExecuting(context);
        }

        protected Guid GetCurrentUserGuid()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserID" || c.Type == ClaimTypes.NameIdentifier);
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

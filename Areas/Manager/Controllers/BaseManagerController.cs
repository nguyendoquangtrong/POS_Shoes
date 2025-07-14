// Areas/Manager/Controllers/BaseManagerController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_Shoes.Models.Data;

namespace POS_Shoes.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public abstract class BaseManagerController : Controller
    {
        protected readonly ApplicationDbContext _context;

        protected BaseManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        protected string GetCurrentUserId()
        {
            return User.Identity.Name ?? string.Empty;
        }
    }
}

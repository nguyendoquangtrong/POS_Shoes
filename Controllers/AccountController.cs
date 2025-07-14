// Controllers/AccountController.cs
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.ViewModels;
using System.Security.Claims;

namespace POS_Shoes.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu user đã đăng nhập, redirect về trang chính
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToRoleDashboard(User.FindFirst(ClaimTypes.Role)?.Value);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Tìm user trong database
                var user = _context.Users.FirstOrDefault(u => u.Username == model.Username);

                if (user != null && VerifyPassword(model.Password, user.Password))
                {
                    // Tạo claims cho user
                    var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.Username),
                            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                            new Claim(ClaimTypes.Role, user.Role),
                            new Claim("UserId", user.UserID.ToString()),
                            new Claim("UserRole", user.Role)
                        };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    _logger.LogInformation("User {Username} logged in successfully", model.Username);

                    // Redirect dựa trên returnUrl hoặc role
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToRoleDashboard(user.Role);
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác");
                    _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out");
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToRoleDashboard(string role)
        {
            return role switch
            {
                "Manager" => RedirectToAction("Index", "Dashboard", new { area = "Manager" }),
                "Saler" => RedirectToAction("Index", "Dashboard", new { area = "Saler" }),
                "Accountant" => RedirectToAction("Index", "Dashboard", new { area = "Accountant" }),
                "Marketing" => RedirectToAction("Index", "Dashboard", new { area = "Marketing" }),
                _ => RedirectToAction("Index", "Home")
            };
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            // TODO: Implement proper password hashing (BCrypt, Argon2, etc.)
            // Hiện tại đang so sánh trực tiếp (KHÔNG AN TOÀN cho production)
            return inputPassword == storedPassword;
        }
    }
}

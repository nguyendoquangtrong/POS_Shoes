// Areas/Marketing/Controllers/PromotionController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Areas.Marketing.Controllers
{
    public class PromotionController : MarketingBaseController
    {
        public PromotionController(ApplicationDbContext ctx) : base(ctx) { }

        // Danh sách Promotion (chỉ của Marketing – mọi trạng thái)
        public async Task<IActionResult> Index()
        {
            var list = await _context.Promotions
                                     .OrderByDescending(p => p.CreatedAt)
                                     .ToListAsync();
            return View(list);
        }

        // GET  : Marketing/Promotion/Create
        public IActionResult Create() => View(new Promotion());

        // POST : Marketing/Promotion/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion model)
        {
            if (!ModelState.IsValid) return View(model);

            model.PromotionID = Guid.NewGuid();
            model.Status = "Pending";          // chờ ông chủ phê duyệt
            model.CreatedAt = DateTime.Now;       // => thêm property CreatedAt (không cần navigation)
            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Promotion đã được tạo (chờ phê duyệt)!";
            return RedirectToAction(nameof(Index));
        }

        // POST : Marketing/Promotion/Delete/5  (xoá cứng hoặc chuyển IsActive=false)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            // xoá mềm
            promo.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Promotion đã được xoá!";
            return RedirectToAction(nameof(Index));
        }
    }
}

// Areas/Manager/Controllers/AssignmentController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;

namespace POS_Shoes.Areas.Manager.Controllers
{
    public class AssignmentController : BaseManagerController
    {
        public AssignmentController(ApplicationDbContext context) : base(context) { }

        // GET: Manager/Assignment
        public async Task<IActionResult> Index()
        {
            var assignments = await _context.Assignments
                .Include(a => a.User)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(assignments);
        }

        // GET: Manager/Assignment/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AssignmentID == id);

            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // GET: Manager/Assignment/Create
        public IActionResult Create()
        {
            ViewData["UserID"] = new SelectList(_context.Users
                .Where(u => u.Role != "Manager"), "UserID", "Username");
            return View();
        }

        // POST: Manager/Assignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Shift,Date,StartTime,EndTime,UserID")] Assignment assignment)
        {
            if (ModelState.IsValid)
            {
                assignment.AssignmentID = Guid.NewGuid();
                _context.Add(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo lịch làm việc thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserID"] = new SelectList(_context.Users
                .Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
            return View(assignment);
        }

        // GET: Manager/Assignment/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null) return NotFound();

            ViewData["UserID"] = new SelectList(_context.Users
                .Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
            return View(assignment);
        }

        // PUT: Manager/Assignment/Edit/5
        [HttpPut]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("AssignmentID,Description,Shift,Date,StartTime,EndTime,UserID")] Assignment assignment)
        {
            if (id != assignment.AssignmentID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật lịch làm việc thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssignmentExists(assignment.AssignmentID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserID"] = new SelectList(_context.Users
                .Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
            return View(assignment);
        }

        // GET: Manager/Assignment/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.AssignmentID == id);
            if (assignment == null) return NotFound();

            return View(assignment);
        }

        // DELETE: Manager/Assignment/Delete/5
        [HttpDelete]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment != null)
            {
                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa lịch làm việc thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AssignmentExists(Guid id)
        {
            return _context.Assignments.Any(e => e.AssignmentID == id);
        }
    }
}

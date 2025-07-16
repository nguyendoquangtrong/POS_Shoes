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
            // Không cho StartTime == EndTime hoặc ca không hợp lệ: StartTime > EndTime mà KHÔNG phải ca đêm (vd: 14:00-11:00 bị cấm)
            // Cho phép StartTime > EndTime nếu là ca đêm (chỉ hợp lệ với các ca thật sự ca đêm, ví dụ Shift == "Tối")
            if (assignment.StartTime == assignment.EndTime)
            {
                ModelState.AddModelError("", "Giờ bắt đầu và kết thúc không được giống nhau!");
            }
            if (assignment.StartTime > assignment.EndTime)
            {
                ModelState.AddModelError("", "Giờ bắt đầu phải nhỏ hơn giờ kết thúc ");
            }
            if (!ModelState.IsValid)
            {
                ViewData["UserID"] = new SelectList(_context.Users.Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
                return View(assignment);
            }

            // Tiếp tục kiểm tra trùng ca
            if (await IsShiftConflictAsync(assignment))
            {
                ModelState.AddModelError("", "Nhân viên đã có lịch làm trùng giờ!");
                ViewData["UserID"] = new SelectList(_context.Users.Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
                return View(assignment);
            }
            assignment.AssignmentID = Guid.NewGuid();
            _context.Add(assignment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo lịch làm việc thành công!";
            return RedirectToAction(nameof(Index));
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("AssignmentID,Description,Shift,Date,StartTime,EndTime,UserID")] Assignment assignment)
        {
            if (id != assignment.AssignmentID) return NotFound();

            if (assignment.StartTime == assignment.EndTime)
            {
                ModelState.AddModelError("", "Giờ bắt đầu và kết thúc không được giống nhau!");
            }
            if (assignment.StartTime > assignment.EndTime)
            {
                ModelState.AddModelError("", "Giờ bắt đầu phải nhỏ hơn giờ kết thúc");
            }
            if (!ModelState.IsValid)
            {
                ViewData["UserID"] = new SelectList(_context.Users.Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
                return View(assignment);
            }

            if (await IsShiftConflictAsync(assignment))
            {
                ModelState.AddModelError("", "Nhân viên đã có lịch làm trùng giờ!");
                ViewData["UserID"] = new SelectList(_context.Users.Where(u => u.Role != "Manager"), "UserID", "Username", assignment.UserID);
                return View(assignment);
            }
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


        private async Task<bool> IsShiftConflictAsync(Assignment assignment)
        {
            // Ngày hiện tại và (nếu ca đêm qua ngày) thì cả ngày hôm sau
            var relates = new List<DateOnly> { assignment.Date };
            if (assignment.EndTime < assignment.StartTime)
                relates.Add(assignment.Date.AddDays(1));

            var relevantShifts = await _context.Assignments
                .Where(a =>
                    a.UserID == assignment.UserID &&
                    relates.Contains(a.Date) &&
                    a.AssignmentID != assignment.AssignmentID // bỏ qua bản ghi đang sửa
                )
                .ToListAsync();

            foreach (var other in relevantShifts)
            {
                if (IsShiftsOverlap(assignment.Date, assignment.StartTime, assignment.EndTime,
                                    other.Date, other.StartTime, other.EndTime))
                {
                    return true;
                }
            }
            return false;
        }

        // Hàm này trả về true nếu 2 ca làm overlap nhau, kể cả ca tối (là ca qua nửa đêm)
        private bool IsShiftsOverlap(
            DateOnly d1, TimeOnly s1, TimeOnly e1,
            DateOnly d2, TimeOnly s2, TimeOnly e2)
        {
            // Tạo hai dải thời gian tuyệt đối
            (DateTime, DateTime) range1 = ConvertToDateTimeRange(d1, s1, e1);
            (DateTime, DateTime) range2 = ConvertToDateTimeRange(d2, s2, e2);

            // Check overlap
            return range1.Item1 < range2.Item2 && range2.Item1 < range1.Item2;
        }

        // Chuyển lịch làm từng ngày + giờ thành datetime tuyệt đối (giải quyết ca đêm)
        private (DateTime, DateTime) ConvertToDateTimeRange(DateOnly d, TimeOnly s, TimeOnly e)
        {
            var from = d.ToDateTime(s);
            var to = (e < s) ?
                d.AddDays(1).ToDateTime(e) : // Ca đêm (kết thúc sau nửa đêm)
                d.ToDateTime(e);
            return (from, to);
        }

        private bool AssignmentExists(Guid id)
        {
            return _context.Assignments.Any(e => e.AssignmentID == id);
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

    }
}

// Areas/Accountant/Controllers/PaySlipController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POS_Shoes.Models.Data;
using POS_Shoes.Models.Entities;
using POS_Shoes.Models.ViewModels;

namespace POS_Shoes.Areas.Accountant.Controllers
{
    public class PaySlipController : AccountantBaseController
    {
        public PaySlipController(ApplicationDbContext context) : base(context)
        {
        }

        // GET: Accountant/PaySlip/Index
        public async Task<IActionResult> Index(int month = 0, int year = 0, Guid? userID = null, string status = "")
        {
            if (month == 0) month = DateTime.Now.Month;
            if (year == 0) year = DateTime.Now.Year;

            var model = new PaySlipListViewModel
            {
                Month = month,
                Year = year,
                UserID = userID,
                Status = status
            };

            var query = _context.PaySlips
                .Include(p => p.User)
                .AsQueryable();

            // Filter by month/year
            query = query.Where(p => p.PayPeriodStart.Month == month && p.PayPeriodStart.Year == year);

            // Filter by user
            if (userID.HasValue)
            {
                query = query.Where(p => p.UserID == userID.Value);
            }

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            model.PaySlips = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && u.Role != "Accountant")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(model);
        }

        // GET: Accountant/PaySlip/Create
        public async Task<IActionResult> Create()
        {
            var model = new CreatePaySlipViewModel
            {
                PayPeriodStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                PayPeriodEnd = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
            };

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && u.Role != "Accountant")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(model);
        }

        // POST: Accountant/PaySlip/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePaySlipViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if payslip already exists for this period
                var existingPaySlip = await _context.PaySlips
                    .FirstOrDefaultAsync(p => p.UserID == model.UserID &&
                                            p.PayPeriodStart == model.PayPeriodStart &&
                                            p.PayPeriodEnd == model.PayPeriodEnd);

                if (existingPaySlip != null)
                {
                    ModelState.AddModelError("", "Phiếu lương cho nhân viên này trong kỳ này đã tồn tại");
                    ViewBag.Users = await _context.Users
                        .Where(u => u.IsActive && u.Role != "Accountant")
                        .OrderBy(u => u.FullName)
                        .ToListAsync();
                    return View(model);
                }

                // Calculate salary
                var calculatedData = await CalculatePaySlip(model.UserID, model.PayPeriodStart, model.PayPeriodEnd);

                var paySlip = new PaySlip
                {
                    PaySlipID = Guid.NewGuid(),
                    UserID = model.UserID,
                    PayPeriodStart = model.PayPeriodStart,
                    PayPeriodEnd = model.PayPeriodEnd,
                    HourlyRate = calculatedData.HourlyRate,
                    TotalHours = calculatedData.TotalHours,
                    BasicSalary = calculatedData.BasicSalary,
                    Bonus = model.Bonus,
                    Deduction = model.Deduction,
                    NetSalary = calculatedData.BasicSalary + model.Bonus - model.Deduction,
                    Note = model.Note,
                    Status = "Generated", // Chỉ tạo, không thể tự duyệt
                    CreatedByUserID = GetCurrentUserGuid(),
                    CreatedAt = DateTime.Now
                };

                _context.PaySlips.Add(paySlip);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Phiếu lương đã được tạo thành công! Đang chờ Manager phê duyệt.";
                return RedirectToAction("Details", new { id = paySlip.PaySlipID });
            }

            ViewBag.Users = await _context.Users
                .Where(u => u.IsActive && u.Role != "Accountant")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(model);
        }

        // GET: Accountant/PaySlip/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var paySlip = await _context.PaySlips
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaySlipID == id);

            if (paySlip == null)
            {
                return NotFound();
            }

            // Get assignments for this pay period
            var assignments = await _context.Assignments
                .Where(a => a.UserID == paySlip.UserID &&
                           a.Date >= DateOnly.FromDateTime(paySlip.PayPeriodStart) &&
                           a.Date <= DateOnly.FromDateTime(paySlip.PayPeriodEnd))
                .OrderBy(a => a.Date)
                .ToListAsync();

            ViewBag.Assignments = assignments.Select(a => new AssignmentSummary
            {
                Date = a.Date,
                Shift = a.Shift,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Hours = CalculateHours(a.StartTime, a.EndTime),
                Description = a.Description
            }).ToList();

            return View(paySlip);
        }

        // API: Calculate pay slip preview
        [HttpPost]
        public async Task<IActionResult> CalculatePreview([FromBody] CreatePaySlipViewModel model)
        {
            if (model.UserID == Guid.Empty)
            {
                return BadRequest("Vui lòng chọn nhân viên");
            }

            var calculatedData = await CalculatePaySlip(model.UserID, model.PayPeriodStart, model.PayPeriodEnd);

            return Json(new
            {
                hourlyRate = calculatedData.HourlyRate,
                totalHours = calculatedData.TotalHours,
                basicSalary = calculatedData.BasicSalary,
                bonus = model.Bonus,
                deduction = model.Deduction,
                netSalary = calculatedData.BasicSalary + model.Bonus - model.Deduction,
                assignments = calculatedData.Assignments
            });
        }

        // Private method to calculate pay slip
        private async Task<CreatePaySlipViewModel> CalculatePaySlip(Guid userID, DateTime startDate, DateTime endDate)
        {
            var user = await _context.Users.FindAsync(userID);
            if (user == null)
            {
                throw new ArgumentException("Nhân viên không tồn tại");
            }

            var assignments = await _context.Assignments
                .Where(a => a.UserID == userID &&
                           a.Date >= DateOnly.FromDateTime(startDate) &&
                           a.Date <= DateOnly.FromDateTime(endDate))
                .OrderBy(a => a.Date)
                .ToListAsync();

            var assignmentSummaries = assignments.Select(a => new AssignmentSummary
            {
                Date = a.Date,
                Shift = a.Shift,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Hours = CalculateHours(a.StartTime, a.EndTime),
                Description = a.Description
            }).ToList();

            var totalHours = assignmentSummaries.Sum(a => a.Hours);
            var basicSalary = totalHours * user.HourlyRate;

            return new CreatePaySlipViewModel
            {
                UserID = userID,
                PayPeriodStart = startDate,
                PayPeriodEnd = endDate,
                HourlyRate = user.HourlyRate,
                TotalHours = totalHours,
                BasicSalary = basicSalary,
                Assignments = assignmentSummaries
            };
        }

        // Helper method to calculate hours
        private decimal CalculateHours(TimeOnly startTime, TimeOnly endTime)
        {
            var duration = endTime - startTime;
            return (decimal)duration.TotalHours;
        }

        // ❌ XÓA METHOD UpdateStatus - Không cho phép Accountant tự duyệt
        // Chỉ Manager mới có quyền phê duyệt phiếu lương
    }
}

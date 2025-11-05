using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TasksController : Controller
    {
        private readonly ContextDataBase _context;

        public TasksController(ContextDataBase context)
        {
            _context = context;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(
            string searchString,
            int? projectId,
            int? employeeId,
            bool? status,
            string sortOrder,
            int? month,
            int? year)
        {
            // Параметри сортування
            ViewData["NameSort"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["StatusSort"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["EmployeeSort"] = sortOrder == "employee" ? "employee_desc" : "employee";
            ViewData["ProjectSort"] = sortOrder == "project" ? "project_desc" : "project";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentProjectId"] = projectId;
            ViewData["CurrentEmployeeId"] = employeeId;
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentMonth"] = month;
            ViewData["CurrentYear"] = year;

            // Основний запит
            var tasks = _context.Tasks
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .AsQueryable();

            // Пошук
            if (!string.IsNullOrEmpty(searchString))
            {
                tasks = tasks.Where(t => t.Name.Contains(searchString));
            }

            // Фільтри
            if (projectId.HasValue)
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);

            if (employeeId.HasValue)
                tasks = tasks.Where(t => t.EmployeeId == employeeId.Value);

            if (status.HasValue)
                tasks = tasks.Where(t => t.Status == status.Value);

            // Фільтр по місяцю/року (за StartDate)
            if (month.HasValue && year.HasValue)
            {
                tasks = tasks.Where(t =>
                    t.StartDate.HasValue &&
                    t.StartDate.Value.Month == month.Value &&
                    t.StartDate.Value.Year == year.Value);
            }
            else if (month.HasValue)
            {
                tasks = tasks.Where(t =>
                    t.StartDate.HasValue &&
                    t.StartDate.Value.Month == month.Value);
            }

            // Сортування
            tasks = sortOrder switch
            {
                "name_desc" => tasks.OrderByDescending(t => t.Name),
                "status" => tasks.OrderBy(t => t.Status),
                "status_desc" => tasks.OrderByDescending(t => t.Status),
                "employee" => tasks.OrderBy(t => t.Employee != null ? t.Employee.Name : ""),
                "employee_desc" => tasks.OrderByDescending(t => t.Employee != null ? t.Employee.Name : ""),
                "project" => tasks.OrderBy(t => t.Project.Name),
                "project_desc" => tasks.OrderByDescending(t => t.Project.Name),
                _ => tasks.OrderBy(t => t.Name),
            };

            // Дані для фільтрів
            ViewData["Projects"] = new SelectList(_context.Projects, "Id", "Name", projectId);
            ViewData["Employees"] = new SelectList(_context.Employees, "Id", "Name", employeeId);

            // Місяці та роки для випадаючих списків
            ViewData["Months"] = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                });

            ViewData["Years"] = Enumerable.Range(DateTime.Today.Year - 5, 6)
                .Select(y => new SelectListItem
                {
                    Value = y.ToString(),
                    Text = y.ToString()
                });

            return View(await tasks.ToListAsync());
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name");
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name");
            return View(new Models.Task { StartDate = DateTime.Today });
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,Status,EmployeeId,ProjectId,StartDate,EndDate")] Models.Task task)
        {
            _context.Add(task);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Завдання створено!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks.FindAsync(id);
            if (task == null) return NotFound();

            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Name", task.EmployeeId);
            ViewData["ProjectId"] = new SelectList(_context.Projects, "Id", "Name", task.ProjectId);
            return View(task);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Name,Description,Status,EmployeeId,ProjectId,StartDate,EndDate")] Models.Task task)
        {
            if (id != task.Id) return NotFound();

            try
            {
                _context.Update(task);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tasks.Any(e => e.Id == task.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.Employee)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return NotFound();

            return View(task);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
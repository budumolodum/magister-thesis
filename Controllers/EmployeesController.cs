using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly ContextDataBase _context;

        public EmployeesController(ContextDataBase context)
        {
            _context = context;
        }

        // GET: Employees
        public async Task<IActionResult> Index(
            string searchString,
            string sortOrder)
        {
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["PhoneSort"] = sortOrder == "phone" ? "phone_desc" : "phone";
            ViewData["EmailSort"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;

            var employees = _context.Employees
                .Include(e => e.Tasks)
                .ThenInclude(t => t.Project)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(e => e.Name.Contains(searchString) ||
                                                e.Phone.Contains(searchString) ||
                                                e.Email.Contains(searchString));
            }

            employees = sortOrder switch
            {
                "name" => employees.OrderBy(e => e.Name),
                "name_desc" => employees.OrderByDescending(e => e.Name),
                "phone" => employees.OrderBy(e => e.Phone),
                "phone_desc" => employees.OrderByDescending(e => e.Phone),
                "email" => employees.OrderBy(e => e.Email),
                "email_desc" => employees.OrderByDescending(e => e.Email),
                _ => employees.OrderBy(e => e.Name),
            };

            return View(await employees.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Tasks)
                .ThenInclude(t => t.Project)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View(new Employee());
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email,Skills,Description")] Employee employee)
        {
            _context.Add(employee);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Працівник створений!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email,Skills,Description")] Employee employee)
        {
            if (id != employee.Id) return NotFound();

            try
            {
                _context.Update(employee);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.Id == employee.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null) return NotFound();

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
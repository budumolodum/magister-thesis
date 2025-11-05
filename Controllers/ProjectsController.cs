using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly ContextDataBase _context;

        public ProjectsController(ContextDataBase context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index(
            string searchString,
            int? customerId,
            string sortOrder)
        {
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["TypeSort"] = sortOrder == "type" ? "type_desc" : "type";
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["CustomerSort"] = sortOrder == "customer" ? "customer_desc" : "customer";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCustomerId"] = customerId;

            var projects = _context.Projects
                .Include(p => p.Customer)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Employee)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                projects = projects.Where(p =>
                    p.Name.Contains(searchString) ||
                    p.Type.Contains(searchString));
            }

            if (customerId.HasValue)
            {
                projects = projects.Where(p => p.Customer.Id == customerId);
            }

            projects = sortOrder switch
            {
                "name" => projects.OrderBy(p => p.Name),
                "name_desc" => projects.OrderByDescending(p => p.Name),
                "type" => projects.OrderBy(p => p.Type),
                "type_desc" => projects.OrderByDescending(p => p.Type),
                "price" => projects.OrderBy(p => p.Price),
                "price_desc" => projects.OrderByDescending(p => p.Price),
                "customer" => projects.OrderBy(p => p.Customer.Name),
                "customer_desc" => projects.OrderByDescending(p => p.Customer.Name),
                _ => projects.OrderBy(p => p.Name),
            };

            ViewData["Customers"] = new SelectList(_context.Customers, "Id", "Name", customerId);

            return View(await projects.ToListAsync());
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Customer)
                .Include(p => p.Tasks)
                .ThenInclude(t => t.Employee)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            return View(new Project());
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Type,Price,CustomerId")] Project project)
        {
            _context.Add(project);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Проєкт створений!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", project.Customer.Id);
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Type,Price,CustomerId")] Project project)
        {
            if (id != project.Id) return NotFound();

            try
            {
                _context.Update(project);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Projects.Any(e => e.Id == project.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects
                .Include(p => p.Customer)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project != null)
            {
                _context.Tasks.RemoveRange(project.Tasks);
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly ContextDataBase _context;

        public CustomersController(ContextDataBase context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index(
            string searchString,
            string sortOrder)
        {
            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["PhoneSort"] = sortOrder == "phone" ? "phone_desc" : "phone";
            ViewData["EmailSort"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["CurrentSort"] = sortOrder;
            ViewData["CurrentSearch"] = searchString;

            var customers = _context.Customers
                .Include(c => c.Projects)
                .ThenInclude(p => p.Tasks)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                customers = customers.Where(c =>
                    c.Name.Contains(searchString) ||
                    (c.Phone != null && c.Phone.Contains(searchString)) ||
                    (c.Email != null && c.Email.Contains(searchString)));
            }

            customers = sortOrder switch
            {
                "name" => customers.OrderBy(c => c.Name),
                "name_desc" => customers.OrderByDescending(c => c.Name),
                "phone" => customers.OrderBy(c => c.Phone),
                "phone_desc" => customers.OrderByDescending(c => c.Phone),
                "email" => customers.OrderBy(c => c.Email),
                "email_desc" => customers.OrderByDescending(c => c.Email),
                _ => customers.OrderBy(c => c.Name),
            };

            return View(await customers.ToListAsync());
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Projects)
                .ThenInclude(p => p.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null) return NotFound();

            return View(customer);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email")] Customer customer)
        {
            _context.Add(customer);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Замовник створений!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email")] Customer customer)
        {
            if (id != customer.Id) return NotFound();

            try
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Customers.Any(e => e.Id == customer.Id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var customer = await _context.Customers
                .Include(c => c.Projects)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (customer == null) return NotFound();

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Projects)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer != null)
            {
                // Видаляємо проєкти (і їхні завдання) разом із замовником
                _context.Projects.RemoveRange(customer.Projects);
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
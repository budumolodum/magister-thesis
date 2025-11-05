using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class RoiByTypeViewModel
    {
        public string Type { get; set; } = null!;
        public int TotalRevenue { get; set; }
        public double TotalDays { get; set; }
        public double RevenuePerDay { get; set; }
    }
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly ContextDataBase _context;

        public AnalyticsController(ContextDataBase context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult ROIByType()
        {
            var result = _context.Projects
                .Include(p => p.Tasks)
                .AsEnumerable()
                .GroupBy(p => p.Type)
                .Select(g => new RoiByTypeViewModel
                {
                    Type = g.Key,
                    TotalRevenue = g.Sum(p => p.Price),
                    TotalDays = g.SelectMany(p => p.Tasks)
                                 .Where(t => t.StartDate.HasValue &&
                                           t.EndDate.HasValue &&
                                           t.Status) // ← ТІЛЬКИ ВИКОНАНІ
                                 .Sum(t => (t.EndDate.Value - t.StartDate.Value).TotalDays)
                })
                .Select(x => new RoiByTypeViewModel
                {
                    Type = x.Type,
                    TotalRevenue = x.TotalRevenue,
                    TotalDays = x.TotalDays,
                    RevenuePerDay = x.TotalDays > 0 ? Math.Round(x.TotalRevenue / x.TotalDays, 2) : 0
                })
                .OrderByDescending(x => x.RevenuePerDay)
                .ToList();

            ViewBag.Title = "Прибутковість за типами проєктів (грн/день) (Done Projects)";
            return View("Table", result);
        }

        // 2. Клієнтська лояльність
        public IActionResult CustomerValue()
        {
            var data = _context.Customers
                .Select(c => new
                {
                    c.Name,
                    ProjectsCount = c.Projects.Count,
                    TotalSpent = c.Projects.Sum(p => p.Price),
                    AvgProjectPrice = c.Projects.Count > 0 ? Math.Round(c.Projects.Average(p => p.Price), 0) : 0
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList();

            ViewBag.Title = "Топ-10 клієнтів за доходом";
            return View("Table", data);
        }

        // 3. Продуктивність команди (Velocity)
        public IActionResult TeamVelocity()
        {
            // 1. Завантажуємо завершені завдання
            var tasks = _context.Tasks
                .Where(t => t.Status && t.EndDate.HasValue)
                .ToList();

            // 2. Якщо немає — створюємо "порожній" графік з нулями
            if (!tasks.Any())
            {
                var emptyData = Enumerable.Range(0, 12)
                    .Select(i => new
                    {
                        Week = $"{DateTime.Today.AddDays(-i * 7):yyyy}-W{GetWeekOfYear(DateTime.Today.AddDays(-i * 7)):D2}",
                        Completed = 0
                    })
                    .OrderBy(x => x.Week)
                    .ToList();

                ViewBag.Labels = emptyData.Select(x => x.Week).ToArray();
                ViewBag.Data = emptyData.Select(x => 0.0).ToArray();
                ViewBag.Title = "Завдання на тиждень (немає даних)";
                ViewBag.ChartType = "line";
                return View("Chart", emptyData);
            }

            // 3. Нормальний розрахунок
            var data = tasks
                .GroupBy(t => new
                {
                    Year = t.EndDate.Value.Year,
                    Week = GetWeekOfYear(t.EndDate.Value)
                })
                .Select(g => new
                {
                    Week = $"{g.Key.Year}-W{g.Key.Week:D2}",
                    Completed = g.Count()
                })
                .OrderByDescending(x => x.Week)
                .Take(12)
                .OrderBy(x => x.Week)  // для графіка — по порядку
                .ToList();

            ViewBag.Labels = data.Select(x => x.Week).ToArray();
            ViewBag.Data = data.Select(x => (double)x.Completed).ToArray();
            ViewBag.Title = "Завдання на тиждень (останні 12)";
            ViewBag.ChartType = "line";
            ViewBag.Label = " ";

            return View("Chart", data);
        }

        // Допоміжний метод 
        private int GetWeekOfYear(DateTime date)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        // 4. Вузькі місця
        public IActionResult Bottlenecks()
        {
            var data = _context.Employees
                .Select(e => new
                {
                    e.Name,
                    ActiveProjects = e.Tasks
                        .Where(t => !t.Status && t.StartDate <= DateTime.Today && (t.EndDate == null || t.EndDate >= DateTime.Today))
                        .Select(t => t.ProjectId)
                        .Distinct()
                        .Count()
                })
                .Where(x => x.ActiveProjects > 1)
                .OrderByDescending(x => x.ActiveProjects)
                .ToList();

            ViewBag.Title = "Працівники з кількома активними проєктами";
            return View("Table", data);
        }

        // 5. Планові vs Фактичні терміни (якщо є PlannedEndDate — додай у модель)
        public IActionResult PlannedVsActual()
        {
            // Приклад: якщо додаси поле PlannedEndDate у Task
            var data = _context.Tasks
                .Where(t => t.Status && t.StartDate.HasValue && t.EndDate.HasValue /* && t.PlannedEndDate.HasValue */)
                .Select(t => new
                {
                    t.Name,
                    PlannedDays = 7, // або t.PlannedEndDate - t.StartDate
                    ActualDays = (t.EndDate.Value - t.StartDate.Value).Days,
                    Deviation = (t.EndDate.Value - t.StartDate.Value).Days - 7
                })
                .ToList();

            ViewBag.Title = "Планові vs Фактичні терміни";
            return View("Table", data);
        }

        // 6. Сезонність
        public IActionResult Seasonality()
        {
            var projects = _context.Projects
                .Include(p => p.Tasks)
                .Where(p => p.Tasks.Any(t => t.StartDate.HasValue))
                .ToList();

            ViewBag.DebugProjectsCount = projects.Count;
            ViewBag.DebugProjects = projects.Select(p => new
            {
                p.Name,
                StartDates = p.Tasks.Where(t => t.StartDate.HasValue).Select(t => t.StartDate.Value.ToString("yyyy-MM-dd")).ToList()
            }).ToList();

            if (!projects.Any())
            {
                ViewBag.Title = "Сезонність: немає даних";
                ViewBag.Labels = new string[0];
                ViewBag.Data = new double[0];
                return View("Chart", new List<object>());
            }

            var data = projects
                .Select(p => new
                {
                    Project = p,
                    MinStartDate = p.Tasks
                        .Where(t => t.StartDate.HasValue)  // ← ТІЛЬКИ НЕ NULL
                        .Select(t => t.StartDate.Value)
                        .DefaultIfEmpty(DateTime.MaxValue)  // ← якщо всі null
                        .Min()
                })
                .Where(x => x.MinStartDate != DateTime.MaxValue)  // ← відкидаємо порожні
                .GroupBy(x => x.MinStartDate.Month)
                .Select(g => new
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    Projects = g.Count()
                })
                .OrderBy(x => Array.IndexOf(CultureInfo.CurrentCulture.DateTimeFormat.MonthNames, x.Month))
                .ToList();

            ViewBag.Title = "Сезонність запуску проєктів";
            ViewBag.Labels = data.Select(x => x.Month).ToArray();
            ViewBag.Data = data.Select(x => (double)x.Projects).ToArray();
            ViewBag.ChartType = "bar";
            ViewBag.Label = " ";

            return View("Chart", data);
        }

        // 7. Рентабельність клієнтів
        public IActionResult CustomerMargin()
        {
            var data = _context.Customers
                .Select(c => new
                {
                    c.Name,
                    Revenue = c.Projects.Sum(p => p.Price),
                    TasksCount = c.Projects.SelectMany(p => p.Tasks).Count(),
                    AvgPerTask = c.Projects.SelectMany(p => p.Tasks).Any()
                        ? Math.Round(c.Projects.Sum(p => p.Price) / (double)c.Projects.SelectMany(p => p.Tasks).Count(), 0)
                        : 0
                })
                .OrderByDescending(x => x.AvgPerTask)
                .Take(10)
                .ToList();

            ViewBag.Title = "Рентабельність клієнтів (грн/завдання)";
            return View("Table", data);
        }

        public IActionResult AssignmentSuccess()
        {
            var today = DateTime.Today;

            var data = _context.Tasks
                .Where(t => t.EmployeeId != null
                         && t.StartDate.HasValue
                         && t.StartDate.Value <= today
                         && t.EndDate.HasValue
                         && !t.Status) // ТІЛЬКИ НЕЗАВЕРШЕНІ
                .GroupBy(t => t.Employee.Name)
                .Select(g => new
                {
                    Employee = g.Key,
                    TotalActive = g.Count(),
                    Overdue = g.Count(t => t.EndDate.Value < today),
                    OverdueRate = g.Count() > 0
                        ? Math.Round(g.Count(t => t.EndDate.Value < today) * 100.0 / g.Count(), 1)
                        : 0
                })
                .Where(x => x.TotalActive > 0)
                .OrderByDescending(x => x.OverdueRate)
                .ToList();

            ViewBag.Title = "Прострочені активні завдання (%)";
            ViewBag.Labels = data.Select(x => x.Employee).ToArray();
            ViewBag.Data = data.Select(x => (double)x.OverdueRate).ToArray();
            ViewBag.ChartType = "bar";
            ViewBag.Label = "Прострочено (%)";

            ViewBag.DebugData = data;

            return View("Chart", data);
        }

        // 9. Прогноз доходу
        public IActionResult RevenueForecast()
        {
            var total = _context.Projects
                .Where(p => p.Tasks.Any(t => !t.Status))
                .Sum(p => p.Tasks.Count(t => !t.Status) * (p.Price / (double)p.Tasks.Count));

            var forecastValue = Math.Round(total, 0); // ← double

            ViewBag.Value = forecastValue;           // ← число, не рядок!
            ViewBag.Title = "Прогноз залишку доходу";
            ViewBag.Description = "Очікуваний дохід від незавершених завдань у поточних проєктах";

            return View("Simple");
        }

        // 10. Ризик провалу проєкту
        public IActionResult ProjectRisk()
        {
            var data = _context.Projects
                .Select(p => new
                {
                    p.Name,
                    Delayed = p.Tasks.Count(t => t.EndDate < DateTime.Today && !t.Status),
                    NoEmployee = p.Tasks.Count(t => t.EmployeeId == null && !t.Status),
                    SinglePerson = p.Tasks.Where(t => t.EmployeeId != null && !t.Status).Select(t => t.EmployeeId).Distinct().Count() <= 1 && p.Tasks.Count(t => !t.Status) > 3,
                    RiskScore = (p.Tasks.Count(t => t.EndDate < DateTime.Today && !t.Status) * 3) +
                                (p.Tasks.Count(t => t.EmployeeId == null && !t.Status) * 2) +
                                (p.Tasks.Count(t => !t.Status) > 3 && p.Tasks.Select(t => t.EmployeeId).Distinct().Count() <= 1 ? 5 : 0)
                })
                .Where(x => x.RiskScore > 0)
                .OrderByDescending(x => x.RiskScore)
                .ToList();

            ViewBag.Title = "Проєкти під ризиком";
            return View("Table", data);
        }
    }
}
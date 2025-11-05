using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1;
using WebApplication1.Models;

[ApiController]
[Route("api/tasks")]
public class TasksApiController : ControllerBase
{
    private readonly ContextDataBase _context;

    public TasksApiController(ContextDataBase context)
    {
        _context = context;
    }

    [HttpGet("by-employee")]
    public async Task<IActionResult> GetTasksByEmployeeName([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Ім'я обов'язкове");

        // Шукаємо працівника
        var employee = await _context.Employees
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Name == name.Trim());

        if (employee == null)
            return NotFound($"Працівник '{name}' не знайдений");

        var today = DateTime.Today;

        // Отримуємо завдання + назву проєкту через JOIN
        var tasks = await _context.Tasks
            .Include(t => t.Project)
            .Where(t => t.EmployeeId == employee.Id)
            .Select(t => new
            {
                Project = t.Project.Name,
                TaskName = t.Name,
                StartDate = t.StartDate.HasValue ? t.StartDate.Value.ToString("yyyy-MM-dd") : null,
                EndDate = t.EndDate.HasValue ? t.EndDate.Value.ToString("yyyy-MM-dd") : null,
                Status = t.Status,
                IsOverdue = t.EndDate.HasValue && t.EndDate.Value < today && !t.Status
            })
            .ToListAsync();

        var response = new
        {
            Employee = employee.Name,
            TotalTasks = tasks.Count,
            Tasks = tasks
        };

        return Ok(response);
    }
}
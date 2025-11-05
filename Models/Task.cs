namespace WebApplication1.Models
{
    public class Task
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;
    }
}

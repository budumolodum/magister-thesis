namespace WebApplication1.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Skills { get; set; }
        public string? Description { get; set; }
        public List<Task> Tasks { get; set; } = new();
    }
}

using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
namespace WebApplication1
{
    public class ContextDataBase : DbContext
    {
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Models.Task> Tasks { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;

        public ContextDataBase()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite("Data Source=Base.db");
    }
}

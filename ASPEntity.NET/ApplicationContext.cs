using Microsoft.EntityFrameworkCore;

public class ApplicationContext : DbContext
{
    public DbSet<Record> Records { get; set; } = null!;

    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>().HasData(
                new() { Id = 1, Name = "Tom", Text = "Hello" },
                new() { Id = 2, Name = "Genry", Text = "Good" },
                new() { Id = 3, Name = "Pavel", Text = "Bye" }
        );
    }
}

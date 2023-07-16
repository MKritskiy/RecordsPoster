using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Record> Records { get; set; } = null!;

    public ApplicationContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connection = "Host=localhost;Port=5432;Database=usersdb;Username=postgres;Password=NaGorshkeSiditKoro1!";

        optionsBuilder.UseNpgsql(connection);
    }
    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    //Role adminRole = new Role() { Id = 1, Name = "Admin" };
    //    //Role userRole = new Role() { Id = 2, Name = "User" };

    //    //User tom = new User() { Id = 1, Nickname = "Tom", Password = "12345", Role = "Admin"};
    //    //User tim = new User() { Id = 2, Nickname = "Tim", Password = "11111", Role = "User" };
    //    //User jerry = new User() { Id = 3, Nickname = "Jerry", Password = "123", Role = "User"};

    //    //Record record = new Record() { Id= 1, Name = "Test Record", Text = "Test Record Text" };
    //    //modelBuilder.Entity<User>().HasData(tom, tim, jerry);
    //    //modelBuilder.Entity<Record>().HasData(record);
    //    //modelBuilder.Entity<Role>().HasData(adminRole, userRole);

    //}
}

using Fitpad.Model;
using System.Data.Entity;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext() : base("DefaultConnection") { }

    public DbSet<UserModel> Users { get; set; }
    // Добавьте другие DbSet для таблиц, которые есть в базе
}

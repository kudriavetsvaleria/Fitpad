using Fitpad.Model;
using Fitpad.Model.Entities;
using System.Data.Entity;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext() : base("DefaultConnection")
    {
        // Отключаем автоматическое создание базы данных
        Database.SetInitializer<ApplicationDbContext>(null);
    }

    public DbSet<UserModel> Users { get; set; }
    public DbSet<UserInfoModel> UserInfos { get; set; } // Изменено на UserInfos

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Привязываем модели к существующим таблицам
        modelBuilder.Entity<UserModel>().ToTable("Users");
        modelBuilder.Entity<UserInfoModel>().ToTable("UserInfos");
    }
}

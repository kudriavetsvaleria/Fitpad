using System.Data.Entity;
using System.Windows;

namespace Fitpad.Model
{
public class ApplicationContext : DbContext
{
    public ApplicationContext() : base("DefaultConnection")
    {
        // Проверка подключения к базе данных
        DatabaseConnectionChecker.CheckDatabaseConnection("Data Source=FitpadDB.db;Version=3;");
    }

    public DbSet<UserModel> UserModels { get; set; }
}


}

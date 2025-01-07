using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fitpad.Model.Entities
{
    public class UserInfoModel
    {
        [Key]
        public int Id { get; set; } // Первичный ключ

        [Required]
        public int UserId { get; set; } // Внешний ключ

        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; } // Навигационное свойство

        public string Gender { get; set; }
        public int Age { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public string ActivityLevel { get; set; }
    }
}

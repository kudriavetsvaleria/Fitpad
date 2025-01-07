using System.ComponentModel.DataAnnotations;

namespace Fitpad.Model
{
    public class UserModel
    {
        [Key]
        public int Id { get; set; } // Первичный ключ

        [Required]
        public string Username { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}

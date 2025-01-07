using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fitpad.Model.Entities
{
    public class UserInfoModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string Gender { get; set; }
        public int Age { get; set; }

        // Меняем тип на int
        public int Height { get; set; }

        public double Weight { get; set; }
        public string ActivityLevel { get; set; }
    }

}

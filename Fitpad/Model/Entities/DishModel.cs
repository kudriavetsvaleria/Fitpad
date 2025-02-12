using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitpad.Model.Entities
{
    public class DishModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string CookingTime { get; set; }
        public string Recipe { get; set; }
        public List<string> Ingredients { get; set; }
        public bool IsFavorite { get; set; }  // Новое поле
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fitpad.Model.Entities
{
    public class ProductItem
    {
        public int Index { get; set; } // Добавлен номер продукта
        public string Name { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public double Carbs { get; set; }
        public double Sugar { get; set; }
        public double Quantity { get; set; } // Количество продукта
        public string Unit { get; set; } // Единица измерения

        public override string ToString()
        {
            return $"{Name} (Калорії: {Calories}, Білки: {Protein}, Жири: {Fat}, Вуглеводи: {Carbs})";
        }
    }
}

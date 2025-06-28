using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCenterOnline.Models
{
    public class Employee
    {
        public int ID_сотрудника { get; set; }
        public int Id_сервиса { get; set; }
        public int? Id_пользователя { get; set; } // Nullable, если не все сотрудники имеют учетную запись пользователя
        public string ФИО { get; set; }
        public string Телефон { get; set; }
        public string Должность { get; set; }
        public decimal Зарплата { get; set; }
        public DateTime? Дата_найма { get; set; } // Nullable
    }
}

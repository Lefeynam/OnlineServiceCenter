using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCenterOnline.Models
{
    public class User
    {
        public int id_user { get; set; }
        public int Id_сервиса { get; set; }
        public string Логин { get; set; }
        public string Пароль { get; set; }
        public string Роль { get; set; } // "Администратор", "Мастер", "Менеджер"
        public int? ID_сотрудника { get; set; } // Nullable, если не все пользователи являются сотрудниками
    }
}

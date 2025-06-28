using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCenterOnline.Models
{
    public class Client
    {
        public int ID_клиента { get; set; }
        public int Id_сервиса { get; set; }
        public int? Id_пользователя { get; set; } // Changed to int? for NULL support
        public string Тип_клиента { get; set; }

        // Individual client specific properties (only those in DB)
        public string ФИО { get; set; }

        // Legal client specific properties (only those in DB)
        public string Название_компании { get; set; }
        public string ИНН { get; set; }
        public string Юридический_адрес { get; set; } // Corrected to match DB schema
        public string Директор { get; set; }

        // Common properties (consistent with DB schema)
        public string Номер_телефона { get; set; }
        public string Email { get; set; } // Corrected to match DB schema
        public string Адрес { get; set; } // General address for individuals, could be used for other addresses for legal

        public string DisplayName => Тип_клиента == "Физическое лицо" ? ФИО : Название_компании;
        public string DisplayPhone => Номер_телефона;
        public string DisplayEmail => Email;
    }
}
// ServiceCenterOnline.Models/Order.cs
using System;

namespace ServiceCenterOnline.Models
{
    public class Order
    {
        public int ID_заказа { get; set; }
        public int Id_сервиса { get; set; }
        public int? Id_пользователя { get; set; } // Changed to int? if it can be null in DB
        public int ID_клиента { get; set; } // Foreign key to Clients table
        public int? ID_сотрудника { get; set; } // Foreign key to Сотрудники (Master) - nullable
        public int? ID_услуги { get; set; } // Foreign key to Услуги (Services) - Added this line
        public string Статус_заказа { get; set; }
        public decimal Общая_стоимость { get; set; }
        public DateTime Дата_заказа { get; set; }
        public string Описание_проблемы { get; set; }
        public string Тип_услуги { get; set; } // This will store the comma-separated service types OR the product group
        public string Важность { get; set; }
        public string Бренд { get; set; }
        public string Модель { get; set; }
        public string Серийный_номер { get; set; } // Nullable
        public string Комплектация { get; set; } // Nullable

        // Properties for display/convenience (not directly mapped to DB columns for Order table itself)
        public string ClientFIO { get; set; } // To hold client's FIO/CompanyName for display
        public string ClientPhoneNumber { get; set; } // To hold client's phone number for display
    }
}
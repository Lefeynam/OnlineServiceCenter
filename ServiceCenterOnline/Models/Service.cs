using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceCenterOnline.Models
{
    public class Service
    {
        public int ID_услуги { get; set; }
        public int Id_сервиса { get; set; }
        public int? Id_пользователя { get; set; }
        public string Название_услуги { get; set; }
        public string Описание { get; set; }
        public decimal Цена { get; set; }
    }
}

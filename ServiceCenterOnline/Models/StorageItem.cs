using System;
using System.IO; // Добавлено
using System.Windows.Media; // Добавлено
using System.Windows.Media.Imaging; // Добавлено

namespace ServiceCenterOnline.Models
{
    public class StorageItem
    {
        public int ID_товара { get; set; }
        public int Id_сервиса { get; set; }
        public int? Id_пользователя { get; set; } // Nullable, так как в базе DEFAULT NULL
        public string Наименование { get; set; }
        public string Описание { get; set; }
        public int Количество { get; set; }
        public decimal Цена_за_единицу { get; set; }
        public DateTime Дата_поступления { get; set; }
        public string Местоположение { get; set; }
        public byte[] Фотография { get; set; } // Для хранения сырых данных фотографии

        // Свойство для отображения фотографии в UI (DataGrid ImageColumn, Image control и т.д.)
        public ImageSource ImageSource
        {
            get
            {
                if (Фотография == null || Фотография.Length == 0)
                {
                    return null; // Можно вернуть изображение-заполнитель, если нет фото
                }
                try
                {
                    using (MemoryStream stream = new MemoryStream(Фотография))
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad; // Загрузка сразу
                        image.StreamSource = stream;
                        image.EndInit();
                        image.Freeze(); // Для лучшей производительности в WPF
                        return image;
                    }
                }
                catch
                {
                    // Обработка ошибок при загрузке изображения
                    return null;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ServiceCenterOnline
{
    public static class LocalizationManager
    {
        private static ResourceDictionary _currentLanguageDictionary;
        private const string LanguageResourceBasePath = "pack://application:,,,/ServiceCenterOnline;component/resources/";
        private const string LanguageResourceSuffix = ".xaml";
        private const string DefaultCulture = "ru-RU"; // Язык по умолчанию

        public static void SetLanguage(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode))
            {
                cultureCode = DefaultCulture;
            }

            CultureInfo newCulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            var newLanguageDictUri = new Uri($"{LanguageResourceBasePath}{cultureCode}{LanguageResourceSuffix}", UriKind.Absolute);

            ResourceDictionary newLanguageDictionary;
            try
            {
                newLanguageDictionary = new ResourceDictionary { Source = newLanguageDictUri };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить языковые ресурсы для '{cultureCode}': {ex.Message}\nВозможно, файл ресурсов отсутствует или путь неверен.", "Ошибка локализации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Удаляем предыдущий словарь языка из MergedDictionaries
            if (_currentLanguageDictionary != null)
            {
                // Проверяем, существует ли он еще в MergedDictionaries
                if (Application.Current.Resources.MergedDictionaries.Contains(_currentLanguageDictionary))
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageDictionary);
                }
            }

            // Добавляем новый словарь языка
            Application.Current.Resources.MergedDictionaries.Add(newLanguageDictionary);
            _currentLanguageDictionary = newLanguageDictionary; // Сохраняем ссылку на текущий словарь

            // Принудительное обновление всех привязок DynamicResource в активных окнах
            // Это часто необходимо, если элементы уже отрисованы
            foreach (Window window in Application.Current.Windows)
            {
                // Это небольшой хак для принудительного обновления UI.
                // В некоторых случаях DynamicResource может не обновиться сам по себе.
                var oldContent = window.Content;
                window.Content = null;
                window.Content = oldContent;
            }
        }

        public static string GetCurrentCultureCode()
        {
            return Thread.CurrentThread.CurrentUICulture.Name;
        }
    }
}

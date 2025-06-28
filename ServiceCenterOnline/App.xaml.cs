using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;

namespace ServiceCenterOnline
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Ошибка: {ex.Exception.Message}\nStackTrace: {ex.Exception.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                // Создаем пользовательскую палитру
                var customPrimaryColor = System.Windows.Media.Color.FromRgb(52, 152, 219); // #3498db
                theme.PrimaryLight = new ColorPair(customPrimaryColor);
                theme.PrimaryMid = new ColorPair(customPrimaryColor);
                theme.PrimaryDark = new ColorPair(customPrimaryColor);

                paletteHelper.SetTheme(theme);
                System.Diagnostics.Debug.WriteLine("Тема успешно применена с цветом #3498db");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при применении темы: {ex.Message}");
            }

            string savedLanguage = global::ServiceCenterOnline.Properties.Settings.Default.DefaultLanguage;
            try
            {
                LocalizationManager.SetLanguage(savedLanguage);
                System.Diagnostics.Debug.WriteLine($"Язык установлен: {savedLanguage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при установке языка: {ex.Message}");
            }
        }
    }
}
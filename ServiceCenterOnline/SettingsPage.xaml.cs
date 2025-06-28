// SettingsPage.xaml.cs
using BCrypt.Net; // Для BCrypt
using MaterialDesignColors;
using MaterialDesignThemes.Wpf; // Для PaletteHelper и BaseTheme
using MySql.Data.MySqlClient; // Для работы с MySQL
using System;
using System.Configuration; // Для ConfigurationManager
using System.Linq;
using System.Threading; // Для Thread.CurrentThread.CurrentUICulture
using System.Windows;
using System.Windows.Controls;


namespace ServiceCenterOnline
{
    public partial class SettingsPage : Page
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string connectionString = DbConnection.ConnectionString;

        public SettingsPage(int userId, int serviceId)
        {
            InitializeComponent();
          
            _currentUserId = userId;
            _currentServiceId = serviceId;

            InitializeSettings();
            SetupPasswordVisibilityHandlers();

        }

        private void InitializeSettings()
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            ThemeComboBox.SelectedItem = theme.GetBaseTheme() == BaseTheme.Light ?
                                          ThemeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Светлая") :
                                          ThemeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Темная");

            // Для языка
            string currentCulture = LocalizationManager.GetCurrentCultureCode();
            LanguageComboBox.SelectedItem = LanguageComboBox.Items.OfType<ComboBoxItem>()
                                                       .FirstOrDefault(item => item.Tag?.ToString() == currentCulture);
            if (LanguageComboBox.SelectedItem == null)
            {
                LanguageComboBox.SelectedItem = currentCulture.StartsWith("en") ?
                                                  LanguageComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "English") :
                                                  LanguageComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Русский");
            }


            ThemeComboBox.SelectionChanged += ThemeComboBox_SelectionChanged;
            LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

            SavePasswordButton.Click += SavePasswordButton_Click;
            SaveSettingsButton.Click += SaveSettingsButton_Click;
        }

        private void SetupPasswordVisibilityHandlers()
        {
            ShowCurrentPassword.Checked += (s, e) =>
            {
                CurrentPasswordText.Text = CurrentPassword.Password;
                CurrentPassword.Visibility = Visibility.Collapsed;
                CurrentPasswordText.Visibility = Visibility.Visible;
            };
            ShowCurrentPassword.Unchecked += (s, e) =>
            {
                CurrentPassword.Password = CurrentPasswordText.Text;
                CurrentPasswordText.Visibility = Visibility.Collapsed;
                CurrentPassword.Visibility = Visibility.Visible;
            };

            ShowNewPassword.Checked += (s, e) =>
            {
                NewPasswordText.Text = NewPassword.Password;
                NewPassword.Visibility = Visibility.Collapsed;
                NewPasswordText.Visibility = Visibility.Visible;
            };
            ShowNewPassword.Unchecked += (s, e) =>
            {
                NewPassword.Password = NewPasswordText.Text;
                NewPasswordText.Visibility = Visibility.Collapsed;
                NewPassword.Visibility = Visibility.Visible;
            };

            ShowConfirmNewPassword.Checked += (s, e) =>
            {
                ConfirmNewPasswordText.Text = ConfirmNewPassword.Password;
                ConfirmNewPassword.Visibility = Visibility.Collapsed;
                ConfirmNewPasswordText.Visibility = Visibility.Visible;
            };
            ShowConfirmNewPassword.Unchecked += (s, e) =>
            {
                ConfirmNewPassword.Password = ConfirmNewPasswordText.Text;
                ConfirmNewPasswordText.Visibility = Visibility.Collapsed;
                ConfirmNewPassword.Visibility = Visibility.Visible;
            };
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                try
                {
                    var paletteHelper = new PaletteHelper();
                    var theme = paletteHelper.GetTheme();
                    theme.SetBaseTheme(selectedItem.Content.ToString() == "Светлая" ? Theme.Light : Theme.Dark);
                    paletteHelper.SetTheme(theme);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при смене темы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Используем Tag для получения кода культуры
                string cultureCode = selectedItem.Tag?.ToString();
                if (string.IsNullOrEmpty(cultureCode))
                {
                    // Запасной вариант, если Tag не установлен (менее надежный)
                    cultureCode = selectedItem.Content.ToString() == "English" ? "en-US" : "ru-RU";
                }

                try
                {
                    LocalizationManager.SetLanguage(cultureCode);
                    // RefreshUI() больше не требуется, так как LocalizationManager.SetLanguage это делает.
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при смене языка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SavePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string currentPassword = ShowCurrentPassword.IsChecked == true ? CurrentPasswordText.Text : CurrentPassword.Password;
            string newPassword = ShowNewPassword.IsChecked == true ? NewPasswordText.Text : NewPassword.Password;
            string confirmNewPassword = ShowConfirmNewPassword.IsChecked == true ? ConfirmNewPasswordText.Text : ConfirmNewPassword.Password;

            // Валидация ввода
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmNewPassword))
            {
                MessageBox.Show("Все поля пароля должны быть заполнены.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmNewPassword)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("Новый пароль должен содержать минимум 6 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    const string query = "SELECT Пароль FROM Пользователи WHERE id_user = @UserId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", _currentUserId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHashedPassword = reader.GetString("Пароль");
                                bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, storedHashedPassword);

                                if (!isCurrentPasswordValid)
                                {
                                    MessageBox.Show("Текущий пароль неверный.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }
                    }

                    // Обновление пароля
                    string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    const string updateQuery = "UPDATE Пользователи SET Пароль = @NewPassword WHERE id_user = @UserId";
                    using (MySqlCommand updateCommand = new MySqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@NewPassword", newHashedPassword);
                        updateCommand.Parameters.AddWithValue("@UserId", _currentUserId);
                        updateCommand.ExecuteNonQuery();
                    }

                    MessageBox.Show("Пароль успешно изменен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearPasswordFields();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPasswordFields()
        {
            CurrentPassword.Password = string.Empty;
            CurrentPasswordText.Text = string.Empty;
            NewPassword.Password = string.Empty;
            NewPasswordText.Text = string.Empty;
            ConfirmNewPassword.Password = string.Empty;
            ConfirmNewPasswordText.Text = string.Empty;
            ShowCurrentPassword.IsChecked = false;
            ShowNewPassword.IsChecked = false;
            ShowConfirmNewPassword.IsChecked = false;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сохранение темы
                string selectedTheme = "Light";
                if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
                {
                    selectedTheme = themeItem.Content.ToString() == "Светлая" ? "Light" : "Dark";
                }
                SaveSetting("Theme", selectedTheme);

                // Сохранение языка
                string selectedLanguageCode = "ru-RU";
                if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
                {
                    selectedLanguageCode = langItem.Tag?.ToString() ?? (langItem.Content.ToString() == "English" ? "en-US" : "ru-RU");
                }
                SaveSetting("Language", selectedLanguageCode);

                MessageBox.Show("Настройки успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSetting(string key, string value)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] == null)
            {
                config.AppSettings.Settings.Add(key, value);
            }
            else
            {
                config.AppSettings.Settings[key].Value = value;
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            // Также обновите DefaultLanguage в Properties.Settings.Default
            if (key == "Language")
            {
                Properties.Settings.Default.DefaultLanguage = value;
            }
            else if (key == "Theme")
            {
                Properties.Settings.Default.DefaultTheme = value; // Если вы храните тему там же
            }
            Properties.Settings.Default.Save();
        }
    }
}
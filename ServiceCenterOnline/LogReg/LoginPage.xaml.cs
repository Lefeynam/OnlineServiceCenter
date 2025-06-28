using MySql.Data.MySqlClient;
using ServiceCenterOnline.Administrator;
using ServiceCenterOnline.Manager;
using ServiceCenterOnline.Master;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace ServiceCenterOnline.LogReg
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        string connectionString = DbConnection.ConnectionString;

        public LoginPage()
        {
            InitializeComponent();
            Loaded += LoginPage_Loaded;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем выбранный язык в ComboBox при загрузке окна
            string defaultLanguage = Properties.Settings.Default.DefaultLanguage;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag.ToString() == defaultLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            // Если язык не найден или не установлен, можно выбрать первый элемент или установить по умолчанию
            if (LanguageComboBox.SelectedItem == null && LanguageComboBox.Items.Count > 0)
            {
                LanguageComboBox.SelectedItem = LanguageComboBox.Items[0];
            }

            // Возможно, здесь также нужно подписаться на LanguageChanged, если DGridAdministrator
            // находится в этом же окне и не обновляется.
            // LocalizationManager.LanguageChanged += OnLanguageChanged;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string cultureCode = selectedItem.Tag.ToString();

                // Если выбранный язык уже установлен, ничего не делаем
                if (Properties.Settings.Default.DefaultLanguage == cultureCode)
                {
                    return;
                }

                // Изменяем язык через ваш LocalizationManager
                LocalizationManager.SetLanguage(cultureCode);

                // Сохраняем новый язык по умолчанию
                Properties.Settings.Default.DefaultLanguage = cultureCode;
                Properties.Settings.Default.Save(); // Не забудьте сохранить изменения!
            }
        }


        private void checkPass_Checked(object sender, RoutedEventArgs e)
        {
            ПарольТекст.Text = Пароль.Password; // Копируем пароль из PasswordBox
            Пароль.Visibility = Visibility.Collapsed;
            ПарольТекст.Visibility = Visibility.Visible;
        }

        private void checkPass_Unchecked(object sender, RoutedEventArgs e)
        {
            Пароль.Password = ПарольТекст.Text; // Копируем обратно
            ПарольТекст.Visibility = Visibility.Collapsed;
            Пароль.Visibility = Visibility.Visible;
        }

        private void ButLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = Логин.Text.Trim();
            string password = checkPass.IsChecked == true ? ПарольТекст.Text : Пароль.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    const string query = @"
                SELECT p.id_user, p.Id_сервиса, p.Логин, p.Пароль, p.Роль, COALESCE(s.ФИО, '') AS ФИО, s.ID_сотрудника
                FROM Пользователи p
                LEFT JOIN Сотрудники s ON p.id_user = s.Id_пользователя
                WHERE p.Логин = @Login";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Login", login);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHashedPassword = reader.GetString("Пароль");
                                int userId = reader.GetInt32("id_user");
                                int serviceId = reader.GetInt32("Id_сервиса");
                                string userRole = reader.GetString("Роль");
                                string fio = reader.IsDBNull(reader.GetOrdinal("ФИО")) ? string.Empty : reader.GetString("ФИО");
                                int? employeeId = reader.IsDBNull(reader.GetOrdinal("ID_сотрудника")) ? (int?)null : reader.GetInt32("ID_сотрудника");

                                // Временная проверка пароля (НЕБЕЗОПАСНО, заменить на хеширование)
                                bool isPasswordValid = (password == storedHashedPassword);
                                // bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);

                                if (isPasswordValid)
                                {
                                    //MessageBox.Show($"Вход выполнен успешно! ФИО: {fio}, ID_сотрудника: {employeeId?.ToString() ?? "NULL"}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                                    // Навигация на основе роли
                                    Page targetPage = null;
                                    switch (userRole)
                                    {
                                        case "Администратор":
                                            targetPage = new AdminMainPage(userId, serviceId, fio, userRole);
                                            break;
                                        case "Менеджер":
                                            targetPage = new ManagerMainPage(userId, serviceId, fio, userRole);
                                            break;
                                        case "Мастер":
                                            targetPage = new MainAMasterPage(userId, serviceId, fio, userRole);
                                            break;
                                        default:
                                            MessageBox.Show("Неизвестная роль пользователя. Обратитесь к администратору.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                            return;
                                    }

                                    if (targetPage != null && NavigationService != null)
                                    {
                                        NavigationService.Navigate(targetPage);
                                    }
                                    else
                                    {
                                        MessageBox.Show("Не удалось выполнить навигацию. Возможно, NavigationService недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RegisterClick(object sender, MouseButtonEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(new CreateServicePage());
            }
            else
            {
                MessageBox.Show("Не удалось выполнить навигацию. Возможно, NavigationService недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

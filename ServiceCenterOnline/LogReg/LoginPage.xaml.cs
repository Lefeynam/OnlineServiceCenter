using MySql.Data.MySqlClient;
using ServiceCenterOnline.Administrator;
using ServiceCenterOnline.Manager;
using ServiceCenterOnline.Master;
using System;
using System.Threading.Tasks; // Добавляем для async/await
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Media.Animation; // Для Storyboard

namespace ServiceCenterOnline.LogReg
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        string connectionString = DbConnection.ConnectionString;

        // Это свойство будет управлять видимостью анимации загрузки
        // В реальном приложении лучше использовать ViewModel и INotifyPropertyChanged
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLoading.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(LoginPage), new PropertyMetadata(false, OnIsLoadingChanged));

        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LoginPage page = (LoginPage)d;
            if ((bool)e.NewValue)
            {
                page.ShowLoading();
            }
            else
            {
                page.HideLoading();
            }
        }

        public LoginPage()
        {
            InitializeComponent();
            Loaded += LoginPage_Loaded;
            // Устанавливаем DataContext для того, чтобы DependencyProperty IsLoading мог работать
            // и чтобы мы могли получить доступ к элементам UI через XAML (если понадобится)
            this.DataContext = this;
        }

        private void LoginPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем выбранный язык в ComboBox при загрузке окна
            string defaultLanguage = Properties.Settings.Default.DefaultLanguage;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == defaultLanguage) // Добавил проверку на null для Tag
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
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string cultureCode = selectedItem.Tag?.ToString(); // Добавил проверку на null для Tag

                if (string.IsNullOrEmpty(cultureCode) || Properties.Settings.Default.DefaultLanguage == cultureCode)
                {
                    return;
                }

                LocalizationManager.SetLanguage(cultureCode);
                Properties.Settings.Default.DefaultLanguage = cultureCode;
                Properties.Settings.Default.Save();
            }
        }

        private void checkPass_Checked(object sender, RoutedEventArgs e)
        {
            ПарольТекст.Text = Пароль.Password;
            Пароль.Visibility = Visibility.Collapsed;
            ПарольТекст.Visibility = Visibility.Visible;
        }

        private void checkPass_Unchecked(object sender, RoutedEventArgs e)
        {
            Пароль.Password = ПарольТекст.Text;
            ПарольТекст.Visibility = Visibility.Collapsed;
            Пароль.Visibility = Visibility.Visible;
        }

        // *** Асинхронный метод для обработки входа в аккаунт ***
        private async void ButLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = Логин.Text.Trim();
            string password = checkPass.IsChecked == true ? ПарольТекст.Text : Пароль.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите логин и пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // *** Начинаем показывать анимацию загрузки ***
            IsLoading = true;

            try
            {
                await Task.Run(async () => // Выполняем операцию входа в отдельном потоке
                {
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        await connection.OpenAsync(); // Используем асинхронное открытие соединения
                        const string query = @"
                            SELECT p.id_user, p.Id_сервиса, p.Логин, p.Пароль, p.Роль, COALESCE(s.ФИО, '') AS ФИО, s.ID_сотрудника
                            FROM Пользователи p
                            LEFT JOIN Сотрудники s ON p.id_user = s.Id_пользователя
                            WHERE p.Логин = @Login";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Login", login);
                            using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync()) // Асинхронное чтение
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
                                        // Навигация на основе роли
                                        // Важно: навигация и показ MessageBox должны выполняться в UI-потоке!
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
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
                                        });
                                    }
                                    else
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                                        });
                                    }
                                }
                                else
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show("Неверный логин или пароль.", "Ошибка входа", MessageBoxButton.OK, MessageBoxImage.Error);
                                    });
                                }
                            }
                        }
                    }
                });
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // *** Завершаем показывать анимацию загрузки ***
                IsLoading = false;
            }
        }

        // Методы для управления видимостью загрузочного оверлея
        private void ShowLoading()
        {
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                // Анимация для ProgressRing активируется через IsActive="{Binding IsLoading}" в XAML
            }
        }

        private void HideLoading()
        {
            if (LoadingOverlay != null)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
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
using MySql.Data.MySqlClient;
using ServiceCenterOnline.AddEditPage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServiceCenterOnline.Administrator
{
    public partial class AUsersPage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        // Используем приватное поле _users для хранения всех загруженных пользователей
        private ObservableCollection<UserViewModel> _users = new ObservableCollection<UserViewModel>();
        // ICollectionView для эффективной фильтрации на стороне клиента
        private ICollectionView _usersView;

        public AUsersPage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            LoadData(); // Загружаем все данные один раз при инициализации страницы

            // Инициализируем View для DataGrid
            _usersView = CollectionViewSource.GetDefaultView(_users);
            DGridAdministrator.ItemsSource = _usersView;

            searchbox.Text = "Поиск по ФИО";
            searchbox.Foreground = Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Этот метод теперь загружает ВСЕХ пользователей для текущего сервиса
        private void LoadData()
        {
            _users.Clear(); // Очищаем коллекцию перед загрузкой новых данных
            try
            {
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    connection.Open();
                    // ОБНОВЛЕННЫЙ ЗАПРОС: Теперь выбираем id_user и Id_сервиса
                    const string query = @"
                        SELECT p.id_user, p.Id_сервиса, p.Логин, p.Пароль, p.Роль, COALESCE(s.ФИО, '') AS ФИО
                        FROM Пользователи p
                        LEFT JOIN Сотрудники s ON p.ID_сотрудника = s.ID_сотрудника
                        WHERE p.Id_сервиса = @serviceId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _users.Add(new UserViewModel
                                {
                                    id_user = reader.GetInt32("id_user"), // Заполняем новое свойство
                                    Id_сервиса = reader.GetInt32("Id_сервиса"), // Заполняем новое свойство
                                    Логин = reader.IsDBNull(reader.GetOrdinal("Логин")) ? string.Empty : reader.GetString("Логин"),
                                    Пароль = reader.IsDBNull(reader.GetOrdinal("Пароль")) ? string.Empty : reader.GetString("Пароль"),
                                    Роль = reader.IsDBNull(reader.GetOrdinal("Роль")) ? string.Empty : reader.GetString("Роль"),
                                    ФИО = reader.GetString("ФИО")
                                });
                            }
                        }
                    }
                }
                // DGridAdministrator.ItemsSource уже привязан к _usersView, который в свою очередь привязан к _users.
                // Нет необходимости повторно присваивать ItemSource, если мы меняем саму коллекцию _users.
                // Если _usersView уже был создан, то изменения в _users отразятся автоматически.
                // _usersView.Refresh() понадобится, если вы меняете Filter, но не если меняете саму коллекцию.

            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButAdd(object sender, RoutedEventArgs e)
        {
            AddUser addUserWindow = new AddUser(_currentUserId, _currentServiceId);
            addUserWindow.ShowDialog();
            // После добавления нового пользователя, перезагружаем данные, чтобы обновить DataGrid
            LoadData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;

            if (button == null)
            {
                MessageBox.Show("Внутренняя ошибка: Отправитель не является кнопкой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // ИСправлено: теперь button.DataContext должен быть UserViewModel
            var selectedUser = button.DataContext as UserViewModel;

            if (selectedUser == null)
            {
                MessageBox.Show("Выберите запись для удаления (не удалось получить данные строки).", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены? Запись будет удалена безвозвратно! Все записи, созданные этим пользователем, будут переназначены системному администратору.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                    {
                        connection.Open();

                        int systemAdminUserId = GetSystemAdminUserId(connection);

                        if (systemAdminUserId == 0)
                        {
                            MessageBox.Show("Не удалось найти системного администратора для переназначения записей. Удаление невозможно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        MySqlTransaction transaction = connection.BeginTransaction();
                        try
                        {
                            // Используем id_user из UserViewModel
                            UpdateAssociatedRecords(connection, transaction, "Клиенты", "Id_пользователя", selectedUser.id_user, systemAdminUserId);
                            UpdateAssociatedRecords(connection, transaction, "Заказы", "Id_пользователя", selectedUser.id_user, systemAdminUserId);
                            UpdateAssociatedRecords(connection, transaction, "Услуги", "Id_пользователя", selectedUser.id_user, systemAdminUserId);
                            UpdateAssociatedRecords(connection, transaction, "Склад", "Id_пользователя", selectedUser.id_user, systemAdminUserId);
                            UpdateAssociatedRecords(connection, transaction, "Лог_заказов", "Id_пользователя", selectedUser.id_user, systemAdminUserId);

                            string deleteUserQuery = "DELETE FROM Пользователи WHERE id_user = @id_user AND Id_сервиса = @serviceId";
                            using (MySqlCommand command = new MySqlCommand(deleteUserQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id_user", selectedUser.id_user);
                                command.Parameters.AddWithValue("@serviceId", selectedUser.Id_сервиса); // Используем Id_сервиса из выбранного UserViewModel
                                int rowsAffected = command.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    transaction.Commit();
                                    LoadData(); // Перезагружаем данные после удаления
                                    MessageBox.Show("Запись успешно удалена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    transaction.Rollback();
                                    MessageBox.Show("Запись не найдена в базе данных или не принадлежит вашему сервису.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UpdateAssociatedRecords(MySqlConnection connection, MySqlTransaction transaction, string tableName, string fkColumnName, int oldUserId, int newUserId)
        {
            string query = $"UPDATE `{tableName}` SET `{fkColumnName}` = @newUserId WHERE `{fkColumnName}` = @oldUserId";
            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@newUserId", newUserId);
                command.Parameters.AddWithValue("@oldUserId", oldUserId);
                command.ExecuteNonQuery();
            }
        }

        private int GetSystemAdminUserId(MySqlConnection connection)
        {
            string query = "SELECT id_user FROM Пользователи WHERE Роль = 'Администратор' ORDER BY id_user ASC LIMIT 1";

            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                object result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
            return 0;
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchbox.Text == "Поиск по ФИО")
            {
                searchbox.Text = string.Empty;
                searchbox.Foreground = Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchbox.Text))
            {
                searchbox.Text = "Поиск по ФИО";
                searchbox.Foreground = Brushes.Gray;
            }
        }

        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = searchbox.Text?.Trim();

            // Если поле пустое или содержит текст-заполнитель, сбрасываем фильтр
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по ФИО")
            {
                _usersView.Filter = null; // Сброс фильтра
                return;
            }

            // Применяем фильтр к ICollectionView
            _usersView.Filter = item =>
            {
                UserViewModel user = item as UserViewModel;
                // Учитываем регистр для поиска, если нужно, используйте ToLower()/ToUpper()
                return user != null && user.ФИО.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            // Можно добавить сообщение, если фильтр не дал результатов
            if (_usersView.IsEmpty)
            {
                MessageBox.Show("По вашему запросу ничего не найдено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class UserViewModel
    {
        public int id_user { get; set; }        // Добавлено: ID пользователя
        public int Id_сервиса { get; set; }     // Добавлено: ID сервиса, к которому принадлежит пользователь
        public string Логин { get; set; }
        public string Пароль { get; set; }
        public string Роль { get; set; }
        public string ФИО { get; set; }
    }
}

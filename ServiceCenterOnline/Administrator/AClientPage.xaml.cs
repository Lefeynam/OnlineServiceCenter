using MySql.Data.MySqlClient;
using ServiceCenterOnline.AddEditPage;
using ServiceCenterOnline.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ServiceCenterOnline.Administrator
{
    public partial class AClientPage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        // Используем приватное поле _clients для хранения всех загруженных клиентов
        private ObservableCollection<Client> _clients = new ObservableCollection<Client>();

        private ICollectionView _clientsView;

        public AClientPage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            LoadData(); // Загружаем все данные один раз при инициализации страницы

            // Инициализируем View для DataGrid
            _clientsView = CollectionViewSource.GetDefaultView(_clients);
            DGridClient.ItemsSource = _clientsView; // Привязываем DataGrid к представлению

            searchbox.Text = "Поиск по ФИО,Компания";
            searchbox.Foreground = Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Этот метод теперь загружает ВСЕХ клиентов для текущего сервиса
        private void LoadData()
        {
            _clients.Clear(); // Очищаем коллекцию перед загрузкой новых данных
            try
            {
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    connection.Open();
                    string query = "SELECT * FROM Клиенты WHERE Id_сервиса = @serviceId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _clients.Add(new Client
                                {
                                    ID_клиента = reader.GetInt32("ID_клиента"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    Id_пользователя = reader.GetInt32("Id_пользователя"),
                                    Тип_клиента = reader.IsDBNull(reader.GetOrdinal("Тип_клиента")) ? string.Empty : reader.GetString("Тип_клиента"),
                                    ФИО = reader.IsDBNull(reader.GetOrdinal("ФИО")) ? string.Empty : reader.GetString("ФИО"),
                                    Номер_телефона = reader.IsDBNull(reader.GetOrdinal("Номер_телефона")) ? string.Empty : reader.GetString("Номер_телефона"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString("Email"),
                                    Адрес = reader.IsDBNull(reader.GetOrdinal("Адрес")) ? string.Empty : reader.GetString("Адрес"),
                                    Название_компании = reader.IsDBNull(reader.GetOrdinal("Название_компании")) ? string.Empty : reader.GetString("Название_компании"),
                                    ИНН = reader.IsDBNull(reader.GetOrdinal("ИНН")) ? string.Empty : reader.GetString("ИНН"),
                                    Юридический_адрес = reader.IsDBNull(reader.GetOrdinal("Юридический_адрес")) ? string.Empty : reader.GetString("Юридический_адрес"),
                                    Директор = reader.IsDBNull(reader.GetOrdinal("Директор")) ? string.Empty : reader.GetString("Директор")
                                });
                            }
                        }
                    }
                }
                // DGridClient.ItemsSource уже привязан к _clientsView, который в свою очередь привязан к _clients.
                // Нет необходимости повторно присваивать ItemSource.
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке клиентов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchbox.Text == "Поиск по ФИО,Компания")
            {
                searchbox.Text = string.Empty;
                searchbox.Foreground = Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchbox.Text))
            {
                searchbox.Text = "Поиск по ФИО,Компания";
                searchbox.Foreground = Brushes.Gray;
            }
        }

        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = searchbox.Text?.Trim();

            // Если поле пустое или содержит текст-заполнитель, сбрасываем фильтр
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по ФИО,Компания")
            {
                _clientsView.Filter = null; // Сброс фильтра
                return;
            }

            // Применяем фильтр к ICollectionView
            // Поиск будет выполняться по ФИО или Названию_компании
            _clientsView.Filter = item =>
            {
                Client client = item as Client;
                if (client == null) return false;

                // Сравниваем без учета регистра
                bool matchesFIO = !string.IsNullOrEmpty(client.ФИО) &&
                                  client.ФИО.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
                bool matchesCompany = !string.IsNullOrEmpty(client.Название_компании) &&
                                      client.Название_компании.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;

                return matchesFIO || matchesCompany;
            };

            // Можно добавить сообщение, если фильтр не дал результатов
            if (_clientsView.IsEmpty)
            {
                MessageBox.Show("По вашему запросу ничего не найдено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ButAdd(object sender, RoutedEventArgs e)
        {
            AddClient addClientWindow = new AddClient(_currentUserId, _currentServiceId);
            addClientWindow.ShowDialog();
            // Важно: после закрытия окна добавления, перезагружаем данные,
            // чтобы новый клиент появился в DataGrid
            LoadData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selClient = (Client)DGridClient.SelectedItem;
            if (selClient == null)
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены? Запись будет удалена безвозвратно! Все связанные заказы будут также удалены.", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                    {
                        connection.Open();

                        // Использование транзакции для обеспечения целостности данных
                        MySqlTransaction transaction = connection.BeginTransaction();
                        try
                        {
                            // Удаление связанных заказов (если таблица Заказы имеет внешний ключ с CASCADE DELETE,
                            // то этот запрос может быть не нужен, но лучше оставить для явности или если FK нет)
                            string deleteOrdersQuery = "DELETE FROM Заказы WHERE ID_клиента = @ID_клиента AND Id_сервиса = @serviceId";
                            using (MySqlCommand commandOrders = new MySqlCommand(deleteOrdersQuery, connection, transaction))
                            {
                                commandOrders.Parameters.AddWithValue("@ID_клиента", selClient.ID_клиента);
                                commandOrders.Parameters.AddWithValue("@serviceId", _currentServiceId);
                                commandOrders.ExecuteNonQuery();
                            }

                            // Удаление самого клиента
                            string deleteClientQuery = "DELETE FROM Клиенты WHERE ID_клиента = @ID AND Id_сервиса = @serviceId";
                            using (MySqlCommand commandClient = new MySqlCommand(deleteClientQuery, connection, transaction))
                            {
                                commandClient.Parameters.AddWithValue("@ID", selClient.ID_клиента);
                                commandClient.Parameters.AddWithValue("@serviceId", _currentServiceId);
                                int rowsAffected = commandClient.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    transaction.Commit(); // Подтверждаем транзакцию
                                    LoadData(); // Перезагружаем данные после успешного удаления
                                    MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    transaction.Rollback(); // Откатываем, если клиент не найден
                                    MessageBox.Show("Запись не найдена в базе данных или не принадлежит вашему сервису.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            transaction.Rollback(); // Откатываем при любой ошибке в транзакции
                            throw; // Перебрасываем исключение для обработки внешним try-catch
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

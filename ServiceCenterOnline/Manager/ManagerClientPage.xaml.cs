using MySql.Data.MySqlClient;
// REMOVED: using MySqlX.XDevAPI; // This caused the ambiguity. No longer needed for core operations.
using ServiceCenterOnline.AddEditPage;
using ServiceCenterOnline.Models; // Your custom Client model
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Needed for ICollectionView

namespace ServiceCenterOnline.Manager
{
    /// <summary>
    /// Логика взаимодействия для ManagerClientPage.xaml
    /// </summary>
    public partial class ManagerClientPage : Page
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private ObservableCollection<Client> _allClients = new ObservableCollection<Client>();
        private ICollectionView _clientsView; // Used for filtering

        public ManagerClientPage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            // Initialize the ICollectionView from the ObservableCollection
            _clientsView = CollectionViewSource.GetDefaultView(_allClients);
            DGridClient.ItemsSource = _clientsView; // Bind DataGrid to the view

            LoadData(); // Initial data load

            searchbox.Text = "Поиск по Имя,Фамилия,Компания";
            searchbox.Foreground = System.Windows.Media.Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        /// <summary>
        /// Asynchronously loads client data from the database.
        /// </summary>
        private async void LoadData()
        {
            try
            {
                _allClients.Clear(); // Clear existing data before loading new
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    await connection.OpenAsync();
                    const string query = @"
                        SELECT ID_клиента, Id_сервиса, Id_пользователя, Тип_клиента, ФИО, Название_компании,
                               Номер_телефона, Email, Адрес, ИНН, Юридический_адрес, Директор
                        FROM Клиенты
                        WHERE Id_сервиса = @serviceId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                _allClients.Add(new Client
                                {
                                    ID_клиента = reader.GetInt32("ID_клиента"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    Id_пользователя = reader.IsDBNull(reader.GetOrdinal("Id_пользователя")) ? (int?)null : reader.GetInt32("Id_пользователя"),
                                    Тип_клиента = reader.IsDBNull(reader.GetOrdinal("Тип_клиента")) ? string.Empty : reader.GetString("Тип_клиента"),
                                    ФИО = reader.IsDBNull(reader.GetOrdinal("ФИО")) ? string.Empty : reader.GetString("ФИО"),
                                    Название_компании = reader.IsDBNull(reader.GetOrdinal("Название_компании")) ? string.Empty : reader.GetString("Название_компании"),
                                    Номер_телефона = reader.IsDBNull(reader.GetOrdinal("Номер_телефона")) ? string.Empty : reader.GetString("Номер_телефона"),
                                    Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString("Email"),
                                    Адрес = reader.IsDBNull(reader.GetOrdinal("Адрес")) ? string.Empty : reader.GetString("Адрес"),
                                    ИНН = reader.IsDBNull(reader.GetOrdinal("ИНН")) ? string.Empty : reader.GetString("ИНН"),
                                    Юридический_адрес = reader.IsDBNull(reader.GetOrdinal("Юридический_адрес")) ? string.Empty : reader.GetString("Юридический_адрес"),
                                    Директор = reader.IsDBNull(reader.GetOrdinal("Директор")) ? string.Empty : reader.GetString("Директор")
                                });
                            }
                        }
                    }
                    // No need to set ItemsSource again here, it's bound to _clientsView which uses _allClients
                }
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

        /// <summary>
        /// Applies filter to the client data based on search term.
        /// </summary>
        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = searchbox.Text.Trim();

            // Reset filter if search box is empty or contains placeholder text
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по Имя,Фамилия,Компания")
            {
                _clientsView.Filter = null; // Remove filter
            }
            else
            {
                // Apply filter based on FIO or Company Name
                _clientsView.Filter = item =>
                {
                    // Cast to your Client model
                    if (item is Client client)
                    {
                        return (client.ФИО?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                client.Название_компании?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                    return false;
                };
            }
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchbox.Text == "Поиск по Имя,Фамилия,Компания")
            {
                searchbox.Text = string.Empty;
                searchbox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchbox.Text))
            {
                searchbox.Text = "Поиск по Имя,Фамилия,Компания";
                searchbox.Foreground = System.Windows.Media.Brushes.Gray;
                // Important: If you want to show all data when searchbox is empty and loses focus,
                // ensure the filter is cleared. The TextChanged event will handle this.
            }
        }

        private void ButAdd(object sender, RoutedEventArgs e)
        {
            AddClient addClientWindow = new AddClient(_currentUserId, _currentServiceId);
            bool? result = addClientWindow.ShowDialog(); // Use ShowDialog and capture result

            // If a client was added/edited successfully, refresh data
            if (result == true)
            {
                LoadData(); // Reload data after add/edit
            }
        }

        private void ButEdit(object sender, RoutedEventArgs e)
        {
            // Assuming DGridClient allows single selection and SelectedItem is of type Client
            if (DGridClient.SelectedItem is Client selectedClient)
            {
                // Pass the selected client data to the edit window
                AddClient editClientWindow = new AddClient(_currentUserId, _currentServiceId, selectedClient);
                bool? result = editClientWindow.ShowDialog();

                if (result == true)
                {
                    LoadData(); // Reload data after edit
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void ButDelete(object sender, RoutedEventArgs e)
        {
            if (DGridClient.SelectedItem is Client selectedClient)
            {
                MessageBoxResult confirmResult = MessageBox.Show(
                    $"Вы уверены, что хотите удалить клиента '{selectedClient.DisplayName}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                        {
                            await connection.OpenAsync();
                            const string query = "DELETE FROM Клиенты WHERE ID_клиента = @clientId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@clientId", selectedClient.ID_клиента);
                                int rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Клиент успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    _allClients.Remove(selectedClient); // Remove from ObservableCollection directly
                                    // No need to call LoadData() if you're only removing one item and the collection is already bound.
                                }
                                else
                                {
                                    MessageBox.Show("Не удалось удалить клиента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show($"Ошибка базы данных при удалении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла непредвиденная ошибка при удалении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите клиента для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
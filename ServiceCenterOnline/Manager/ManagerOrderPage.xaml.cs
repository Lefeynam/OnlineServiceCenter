using MySql.Data.MySqlClient;
using ServiceCenterOnline.AddEditPage;
using ServiceCenterOnline.Models; // Ensure this is present
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks; // Added for async Task
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Needed for ICollectionView
using System.Windows.Input; // Needed for MouseButtonEventArgs in tbHistory_MouseDown if it were used

namespace ServiceCenterOnline.Manager
{
    /// <summary>
    /// Логика взаимодействия для ManagerOrderPage.xaml
    /// </summary>
    public partial class ManagerOrderPage : Page
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private ObservableCollection<Models.Order> _allOrders = new ObservableCollection<Models.Order>();
        private ICollectionView _ordersView; // Used for filtering

        public ManagerOrderPage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            // Initialize the ICollectionView from the ObservableCollection
            _ordersView = CollectionViewSource.GetDefaultView(_allOrders);
            DGridZakaz.ItemsSource = _ordersView; // Bind DataGrid to the filtered view

            // Load data when the page is loaded (async)
            this.Loaded += ManagerOrderPage_Loaded;

            // Placeholder for Searchbox, assuming you'll add one in XAML
            searchbox.Text = "Поиск по описанию или клиенту";
            searchbox.Foreground = System.Windows.Media.Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Use a Loaded event handler to call async LoadData
        private async void ManagerOrderPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= ManagerOrderPage_Loaded; // Unsubscribe to prevent multiple calls
            await LoadData(); // Initial data load
        }

        /// <summary>
        /// Asynchronously loads order data from the database.
        /// </summary>
        private async Task LoadData() // Changed from async void to async Task
        {
            try
            {
                _allOrders.Clear(); // Clear existing data before loading new
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    await connection.OpenAsync(); // Use async open
                    const string query = @"
                        SELECT z.ID_заказа, z.Id_сервиса, z.Id_пользователя, z.Статус_заказа,
                               COALESCE(c.ФИО, c.Название_компании, 'Не указан') AS ФИО,
                               z.Описание_проблемы, z.Тип_услуги,
                               COALESCE(c.Номер_телефона, 'Не указан') AS Телефон,
                               z.Общая_стоимость, z.Дата_заказа,
                               z.ID_клиента, -- Add ID_клиента to fetch for AddEditOrderWindow
                               z.ID_сотрудника, -- Corrected from Id_мастера to ID_сотрудника as per your Order model and common DB practice
                               z.Важность, z.Бренд, z.Модель, z.Серийный_номер, z.Комплектация
                        FROM Заказы z
                        JOIN Клиенты c ON z.ID_клиента = c.ID_клиента
                        WHERE z.Статус_заказа != 'Завершен' AND z.Id_сервиса = @serviceId";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync()) // Use async reader
                        {
                            while (await reader.ReadAsync()) // Use async read
                            {
                                _allOrders.Add(new Models.Order
                                {
                                    ID_заказа = reader.GetInt32("ID_заказа"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    // Handle Id_пользователя (if it can be null in DB, it should be int? in model)
                                    Id_пользователя = reader.IsDBNull(reader.GetOrdinal("Id_пользователя")) ? (int?)null : reader.GetInt32("Id_пользователя"),
                                    Статус_заказа = reader.IsDBNull(reader.GetOrdinal("Статус_заказа")) ? string.Empty : reader.GetString("Статус_заказа"),
                                    ClientFIO = reader.GetString("ФИО"), // Corrected property name
                                    Описание_проблемы = reader.IsDBNull(reader.GetOrdinal("Описание_проблемы")) ? string.Empty : reader.GetString("Описание_проблемы"),
                                    Тип_услуги = reader.IsDBNull(reader.GetOrdinal("Тип_услуги")) ? string.Empty : reader.GetString("Тип_услуги"),
                                    ClientPhoneNumber = reader.GetString("Телефон"), // Corrected property name
                                    Общая_стоимость = reader.GetDecimal("Общая_стоимость"),
                                    Дата_заказа = reader.GetDateTime("Дата_заказа"),
                                    ID_клиента = reader.GetInt32("ID_клиента"),
                                    // Handle ID_сотрудника (nullable)
                                    ID_сотрудника = reader.IsDBNull(reader.GetOrdinal("ID_сотрудника")) ? (int?)null : reader.GetInt32("ID_сотрудника"),
                                    Важность = reader.IsDBNull(reader.GetOrdinal("Важность")) ? string.Empty : reader.GetString("Важность"),
                                    Бренд = reader.IsDBNull(reader.GetOrdinal("Бренд")) ? string.Empty : reader.GetString("Бренд"),
                                    Модель = reader.IsDBNull(reader.GetOrdinal("Модель")) ? string.Empty : reader.GetString("Модель"),
                                    Серийный_номер = reader.IsDBNull(reader.GetOrdinal("Серийный_номер")) ? string.Empty : reader.GetString("Серийный_номер"),
                                    Комплектация = reader.IsDBNull(reader.GetOrdinal("Комплектация")) ? string.Empty : reader.GetString("Комплектация")
                                });
                            }
                        }
                    }
                }

                if (_allOrders.Count == 0)
                {
                    MessageBox.Show("Нет незавершённых заказов.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Searchbox Handlers (Assume you have a TextBox named 'searchbox' in XAML) ---
        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = (sender as TextBox)?.Text.Trim();

            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по описанию или клиенту")
            {
                _ordersView.Filter = null; // Remove filter
            }
            else
            {
                _ordersView.Filter = item =>
                {
                    if (item is Models.Order order)
                    {
                        // Search by problem description, service type, client FIO, or phone
                        return (order.Описание_проблемы?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                order.Тип_услуги?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                order.ClientFIO?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0 || // Corrected property name
                                order.ClientPhoneNumber?.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0); // Corrected property name
                    }
                    return false;
                };
            }
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox searchBox = sender as TextBox;
            if (searchBox != null && searchBox.Text == "Поиск по описанию или клиенту")
            {
                searchBox.Text = string.Empty;
                searchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox searchBox = sender as TextBox;
            if (searchBox != null && string.IsNullOrEmpty(searchBox.Text))
            {
                searchBox.Text = "Поиск по описанию или клиенту";
                searchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
        // --- End Searchbox Handlers ---

        private async void ButAdd(object sender, RoutedEventArgs e)
        {
            // You'll need to create an AddOrderWindow in your AddEditPage project.
            // It should have a constructor like: public AddOrderWindow(int userId, int serviceId)
            AddOrderWindow addOrderWindow = new AddOrderWindow(_currentUserId, _currentServiceId);
            bool? result = addOrderWindow.ShowDialog();

            if (result == true) // If order was successfully added/edited
            {
                await LoadData(); // Reload all orders
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (DGridZakaz.SelectedItem is Models.Order selectedOrder)
            {
                // Pass the selected Order object to the AddOrderWindow constructor for editing
                AddOrderWindow editOrderWindow = new AddOrderWindow(_currentUserId, _currentServiceId, selectedOrder);
                bool? result = editOrderWindow.ShowDialog();

                if (result == true) // If order was successfully edited
                {
                    await LoadData(); // Reload all orders
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DGridZakaz.SelectedItem is Models.Order selectedOrder)
            {
                MessageBoxResult confirmResult = MessageBox.Show(
                    $"Вы уверены, что хотите удалить заказ №{selectedOrder.ID_заказа}?",
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
                            const string query = "DELETE FROM Заказы WHERE ID_заказа = @orderId";
                            using (MySqlCommand command = new MySqlCommand(query, connection))
                            {
                                command.Parameters.AddWithValue("@orderId", selectedOrder.ID_заказа);
                                int rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Заказ успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    _allOrders.Remove(selectedOrder); // Remove from ObservableCollection directly
                                }
                                else
                                {
                                    MessageBox.Show("Не удалось удалить заказ.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show($"Ошибка базы данных при удалении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Произошла непредвиденная ошибка при удалении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите заказ для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // This event handler was empty, kept for completeness, but likely not needed or should be removed.
        private void tbHistory_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // You might want to navigate to a history page or show completed orders here.
            // Example: NavigationService.Navigate(new OrderHistoryPage(_currentUserId, _currentServiceId));
        }
    }
}
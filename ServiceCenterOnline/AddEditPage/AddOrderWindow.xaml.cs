using MySql.Data.MySqlClient;
using ServiceCenterOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ServiceCenterOnline.AddEditPage
{
    public class ClientItem
    {
        public int Id { get; set; }
        public string FIO { get; set; }
        public string PhoneNumber { get; set; }

        public override string ToString()
        {
            return FIO;
        }
    }

    public class ServiceItem
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class MasterItem
    {
        public int Id { get; set; }
        public string FIO { get; set; }

        public override string ToString()
        {
            return FIO;
        }
    }

    public partial class AddOrderWindow : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private Order _editingOrder;
        private string connectionString = DbConnection.ConnectionString;

        public AddOrderWindow(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
            _editingOrder = null;
            Title = "Добавить новый заказ";
            btnSave.Content = "Добавить заказ";

            Calendar.SelectedDate = DateTime.Today;
            txtStatus.Text = "Новый";

            Loaded += AddOrderWindow_Loaded;
        }

        public AddOrderWindow(int userId, int serviceId, Order orderToEdit)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
            _editingOrder = orderToEdit;
            Title = $"Редактировать заказ №{orderToEdit.ID_заказа}";
            btnSave.Content = "Сохранить изменения";

            Loaded += AddOrderWindow_Loaded;
        }

        private async void AddOrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddOrderWindow_Loaded; // Отписываемся от события для предотвращения повторных вызовов

            await LoadClientsForComboBox();
            await LoadServicesForListBox();
            await LoadMastersForComboBox();

            if (_editingOrder != null)
            {
                PopulateFieldsForEdit(_editingOrder);
            }
            else
            {
                if (cmbVazhnost.Items.Count > 0 && cmbVazhnost.SelectedIndex == -1)
                {
                    cmbVazhnost.SelectedIndex = 0;
                }
            }
        }

        private async Task LoadClientsForComboBox()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString)) // Изменено здесь
                {
                    await connection.OpenAsync();
                    string query = @"SELECT ID_клиента, COALESCE(ФИО, Название_компании) AS DisplayName, Номер_телефона FROM Клиенты WHERE Id_сервиса = @ServiceId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync()) // Изменено здесь
                    {
                        List<ClientItem> clients = new List<ClientItem>();
                        while (await reader.ReadAsync())
                        {
                            clients.Add(new ClientItem
                            {
                                Id = reader.GetInt32("ID_клиента"),
                                FIO = reader.IsDBNull(reader.GetOrdinal("DisplayName")) ? "Не указано ФИО" : reader.GetString("DisplayName"),
                                PhoneNumber = reader.IsDBNull(reader.GetOrdinal("Номер_телефона")) ? string.Empty : reader.GetString("Номер_телефона")
                            });
                        }

                        txtFIO.Items.Clear(); // Очищаем коллекцию перед установкой ItemsSource
                        txtFIO.ItemsSource = clients;
                    }
                }

                if (txtFIO.Items.Count == 0)
                {
                    MessageBox.Show("В данном сервисе нет доступных клиентов. Пожалуйста, добавьте клиентов перед созданием заказа.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtFIO_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtFIO.SelectedItem is ClientItem selectedClient)
            {
                txtPhoneNumber.Text = selectedClient.PhoneNumber;
            }
            else
            {
                txtPhoneNumber.Text = string.Empty;
            }
        }

        private async Task LoadServicesForListBox()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString)) // Изменено здесь
                {
                    await connection.OpenAsync();
                    string query = @"SELECT ID_услуги, Название_услуги FROM Услуги WHERE Id_сервиса = @ServiceId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync()) // Изменено здесь
                    {
                        List<ServiceItem> services = new List<ServiceItem>();
                        while (await reader.ReadAsync())
                        {
                            services.Add(new ServiceItem
                            {
                                Id = reader.GetInt32("ID_услуги"),
                                Name = reader.GetString("Название_услуги")
                            });
                        }
                        lstServiceType.ItemsSource = services;
                    }
                }

                if (lstServiceType.Items.Count == 0)
                {
                    MessageBox.Show("В данном сервисе нет доступных услуг. Пожалуйста, добавьте услуги перед созданием заказа.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке услуг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMastersForComboBox()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString)) // Изменено здесь
                {
                    await connection.OpenAsync();
                    string query = @"SELECT Id_сотрудника, ФИО FROM Сотрудники WHERE Id_сервиса = @ServiceId AND Должность = 'Мастер'";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);

                    using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync()) // Изменено здесь
                    {
                        List<MasterItem> masters = new List<MasterItem>
                        {
                            new MasterItem { Id = 0, FIO = "Не назначен" } // Опция "Не назначен"
                        };
                        while (await reader.ReadAsync())
                        {
                            masters.Add(new MasterItem
                            {
                                Id = reader.GetInt32("Id_сотрудника"),
                                FIO = reader.GetString("ФИО")
                            });
                        }
                        cmbMaster.ItemsSource = masters;
                        cmbMaster.DisplayMemberPath = "FIO";
                        cmbMaster.SelectedValuePath = "Id";
                        cmbMaster.SelectedIndex = 0; // По умолчанию выбираем "Не назначен"
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке мастеров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFieldsForEdit(Order order)
        {
            ClientItem clientToSelect = txtFIO.Items.Cast<ClientItem>().FirstOrDefault(c => c.Id == order.ID_клиента);
            if (clientToSelect != null)
            {
                txtFIO.SelectedItem = clientToSelect;
            }

            Calendar.SelectedDate = order.Дата_заказа;
            txtStatus.Text = order.Статус_заказа;

            foreach (ComboBoxItem item in cmbVazhnost.Items)
            {
                if (item.Content.ToString() == order.Важность)
                {
                    cmbVazhnost.SelectedItem = item;
                    break;
                }
            }

            txtGroup.Text = order.Тип_услуги;
            txtBrand.Text = order.Бренд;
            txtModel.Text = order.Модель;
            txtSerialNumber.Text = order.Серийный_номер;
            txtDescription.Text = order.Описание_проблемы;
            txtConfiguration.Text = order.Комплектация;
            txtTotalCost.Text = order.Общая_стоимость.ToString();

            ServiceItem serviceToSelect = lstServiceType.Items.Cast<ServiceItem>().FirstOrDefault(s => s.Id == order.ID_услуги);
            if (serviceToSelect != null)
            {
                lstServiceType.SelectedItems.Clear();
                lstServiceType.SelectedItems.Add(serviceToSelect);
            }
            UpdateServiceTypeText();

            MasterItem masterToSelect = cmbMaster.Items.Cast<MasterItem>().FirstOrDefault(m => m.Id == order.ID_сотрудника);
            if (masterToSelect != null)
            {
                cmbMaster.SelectedItem = masterToSelect;
            }
            else
            {
                cmbMaster.SelectedIndex = 0; // Выбираем "Не назначен", если мастер не найден
            }
        }

        private void Тип_услуги_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateServiceTypeText();
        }

        private void UpdateServiceTypeText()
        {
            List<string> selectedServices = lstServiceType.SelectedItems.Cast<ServiceItem>().Select(item => item.Name).ToList();
            txtServiceType.Text = string.Join(", ", selectedServices);
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (Calendar.SelectedDate == null ||
                txtFIO.SelectedItem == null ||
                string.IsNullOrWhiteSpace(txtGroup.Text) ||
                string.IsNullOrWhiteSpace(txtBrand.Text) ||
                string.IsNullOrWhiteSpace(txtModel.Text) ||
                string.IsNullOrWhiteSpace(txtDescription.Text) ||
                lstServiceType.SelectedItem == null ||
                cmbVazhnost.SelectedItem == null ||
                string.IsNullOrWhiteSpace((cmbVazhnost.SelectedItem as ComboBoxItem)?.Content?.ToString()) ||
                string.IsNullOrWhiteSpace(txtTotalCost.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtTotalCost.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal totalCost))
            {
                MessageBox.Show("Некорректный формат стоимости. Используйте только цифры и десятичную точку (например, 123.45).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime orderDate = Calendar.SelectedDate.Value;
            int clientId = (txtFIO.SelectedItem as ClientItem).Id;
            string productGroup = txtGroup.Text.Trim();
            string brand = txtBrand.Text.Trim();
            string model = txtModel.Text.Trim();
            string serialNumber = txtSerialNumber.Text.Trim();
            string description = txtDescription.Text.Trim();
            string configuration = txtConfiguration.Text.Trim();
            int? serviceTypeId = (lstServiceType.SelectedItem as ServiceItem)?.Id;
            string importance = (cmbVazhnost.SelectedItem as ComboBoxItem).Content.ToString();
            string status = txtStatus.Text;
            int? masterId = (cmbMaster.SelectedItem as MasterItem)?.Id;
            if (masterId == 0) masterId = null; // Привязка "Не назначен" (Id=0) к DBNull

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString)) // Изменено здесь
                {
                    await connection.OpenAsync();
                    string query;
                    MySqlCommand command;

                    if (_editingOrder == null) // Режим добавления
                    {
                        query = @"INSERT INTO Заказы
                               (Id_сервиса, Id_пользователя, ID_клиента, ID_сотрудника, ID_услуги,
                                Дата_заказа, Статус_заказа, Общая_стоимость,
                                Описание_проблемы, Тип_услуги, Важность, Бренд, Модель, Серийный_номер, Комплектация)
                               VALUES
                               (@ServiceId, @CurrentUserId, @ClientId, @EmployeeId, @ServiceTypeId,
                                @OrderDate, @Status, @TotalCost,
                                @Description, @ProductGroup, @Importance, @Brand, @Model, @SerialNumber, @Configuration)";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                        command.Parameters.AddWithValue("@CurrentUserId", _currentUserId);
                    }
                    else // Режим редактирования
                    {
                        query = @"UPDATE Заказы SET
                               ID_клиента = @ClientId,
                               ID_сотрудника = @EmployeeId,
                               ID_услуги = @ServiceTypeId,
                               Дата_заказа = @OrderDate,
                               Статус_заказа = @Status,
                               Общая_стоимость = @TotalCost,
                               Описание_проблемы = @Description,
                               Тип_услуги = @ProductGroup,
                               Важность = @Importance,
                               Бренд = @Brand,
                               Модель = @Model,
                               Серийный_номер = @SerialNumber,
                               Комплектация = @Configuration
                               WHERE ID_заказа = @OrderId";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@OrderId", _editingOrder.ID_заказа);
                    }

                    command.Parameters.AddWithValue("@ClientId", clientId);
                    command.Parameters.AddWithValue("@EmployeeId", (object)masterId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ServiceTypeId", (object)serviceTypeId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@OrderDate", orderDate);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@TotalCost", totalCost);
                    command.Parameters.AddWithValue("@Description", description);
                    command.Parameters.AddWithValue("@ProductGroup", productGroup);
                    command.Parameters.AddWithValue("@Importance", importance);
                    command.Parameters.AddWithValue("@Brand", brand);
                    command.Parameters.AddWithValue("@Model", model);
                    command.Parameters.AddWithValue("@SerialNumber", string.IsNullOrEmpty(serialNumber) ? DBNull.Value : (object)serialNumber);
                    command.Parameters.AddWithValue("@Configuration", string.IsNullOrEmpty(configuration) ? DBNull.Value : (object)configuration);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show(_editingOrder == null ? "Заказ успешно добавлен!" : "Изменения заказа успешно сохранены!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(_editingOrder == null ? "Не удалось добавить заказ." : "Не удалось сохранить изменения заказа.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при {(_editingOrder == null ? "добавлении" : "редактировании")} заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешает ввод только цифр и одной десятичной точки
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && e.Text != ".")
            {
                e.Handled = true;
                return;
            }

            TextBox textBox = sender as TextBox;
            // Предотвращает ввод нескольких десятичных точек
            if (e.Text == "." && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
        }
    }
}
using MySql.Data.MySqlClient;
using ServiceCenterOnline.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions; // Для Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ServiceCenterOnline.AddEditPage
{
    public partial class AddClient : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly Client _editingClient;
        private readonly string _connectionString = DbConnection.ConnectionString;

        /// <summary>
        /// Конструктор для добавления нового клиента.
        /// </summary>
        public AddClient(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
            _editingClient = null;
            Title = "Добавить нового клиента";
            Loaded += AddClient_Loaded;
        }

        /// <summary>
        /// Конструктор для редактирования существующего клиента.
        /// </summary>
        public AddClient(int userId, int serviceId, Client clientToEdit)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
            _editingClient = clientToEdit;
            Title = $"Редактировать клиента: {clientToEdit.ФИО ?? clientToEdit.Название_компании}";
            btnAddClient.Content = "Сохранить изменения";
            Loaded += AddClient_Loaded;
        }

        private void AddClient_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddClient_Loaded;

            if (_editingClient != null)
            {
                PopulateFieldsForEdit(_editingClient);
            }
            else
            {
                // Установить "Физическое лицо" по умолчанию для нового клиента.
                if (cmbClientType.Items.Count > 0)
                {
                    cmbClientType.SelectedIndex = 1;
                }
            }
        }

        private void PopulateFieldsForEdit(Client client)
        {
            // Используем cmbClientType_SelectionChanged для правильного отображения панелей.
            // Сначала устанавливаем выбранный тип, затем вызываем метод для обновления UI.
            if (client.Тип_клиента == "Физическое лицо")
            {
                cmbClientType.SelectedItem = cmbClientType.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Физическое лицо");
                txtIndividualName.Text = client.ФИО;
                txtIndividualPhone.Text = client.Номер_телефона;
                txtIndividualEmail.Text = client.Email;
                txtIndividualAddress.Text = client.Адрес;
            }
            else if (client.Тип_клиента == "Юридическое лицо")
            {
                cmbClientType.SelectedItem = cmbClientType.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Юридическое лицо");
                txtCompanyName.Text = client.Название_компании;
                txtCompanyINN.Text = client.ИНН;
                txtCompanyPhone.Text = client.Номер_телефона;
                txtCompanyEmail.Text = client.Email;
                txtCompanyLegalAddress.Text = client.Юридический_адрес;
                txtCompanyDirector.Text = client.Директор;
            }

            cmbClientType_SelectionChanged(null, null); // Обновить видимость панелей.
        }

        private async void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbClientType.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content.ToString() != "Выбрать"))
            {
                MessageBox.Show("Пожалуйста, выберите тип клиента.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string clientType = selectedItem.Content.ToString();

            // Объявляем переменные для хранения данных клиента.
            string fio = null;
            string companyName = null;
            string inn = null;
            string phoneNumber = null;
            string email = null;
            string address = null;
            string legalAddress = null;
            string director = null;

            // Валидация в зависимости от типа клиента.
            if (clientType == "Физическое лицо")
            {
                fio = txtIndividualName.Text.Trim();
                phoneNumber = txtIndividualPhone.Text.Trim();
                email = txtIndividualEmail.Text.Trim();
                address = txtIndividualAddress.Text.Trim();

                if (string.IsNullOrWhiteSpace(fio))
                {
                    MessageBox.Show("Пожалуйста, введите ФИО клиента.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else if (clientType == "Юридическое лицо")
            {
                companyName = txtCompanyName.Text.Trim();
                inn = txtCompanyINN.Text.Trim();
                phoneNumber = txtCompanyPhone.Text.Trim();
                email = txtCompanyEmail.Text.Trim();
                legalAddress = txtCompanyLegalAddress.Text.Trim();
                director = txtCompanyDirector.Text.Trim();

                if (string.IsNullOrWhiteSpace(companyName))
                {
                    MessageBox.Show("Пожалуйста, введите название компании.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(inn) || !(inn.Length == 10 || inn.Length == 12) || !long.TryParse(inn, out _))
                {
                    MessageBox.Show("Пожалуйста, введите корректный ИНН (10 или 12 цифр).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Общие валидации.
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                MessageBox.Show("Пожалуйста, введите номер телефона.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Regex.IsMatch(phoneNumber, @"^\+?\d{10,15}$"))
            {
                MessageBox.Show("Некорректный формат номера телефона. Используйте только цифры и необязательный '+' в начале (например, +79XXXXXXXXX).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query;
                    MySqlCommand command;

                    if (_editingClient == null) // Режим добавления
                    {
                        query = @"INSERT INTO Клиенты (Id_сервиса, Id_пользователя, Тип_клиента, ФИО, Название_компании, Номер_телефона, Email, Адрес, ИНН, Юридический_адрес, Директор)
                                  VALUES (@ServiceId, @UserId, @ClientType, @FIO, @CompanyName, @PhoneNumber, @Email, @Address, @INN, @LegalAddress, @Director)";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                        command.Parameters.AddWithValue("@UserId", _currentUserId);
                    }
                    else // Режим редактирования
                    {
                        query = @"UPDATE Клиенты SET
                                  Тип_клиента = @ClientType,
                                  ФИО = @FIO,
                                  Название_компании = @CompanyName,
                                  Номер_телефона = @PhoneNumber,
                                  Email = @Email,
                                  Адрес = @Address,
                                  ИНН = @INN,
                                  Юридический_адрес = @LegalAddress,
                                  Директор = @Director
                                  WHERE ID_клиента = @ClientId";
                        command = new MySqlCommand(query, connection);
                        command.Parameters.AddWithValue("@ClientId", _editingClient.ID_клиента);
                    }

                    // Общие параметры для INSERT и UPDATE.
                    command.Parameters.AddWithValue("@ClientType", clientType);
                    command.Parameters.AddWithValue("@FIO", string.IsNullOrEmpty(fio) ? DBNull.Value : (object)fio);
                    command.Parameters.AddWithValue("@CompanyName", string.IsNullOrEmpty(companyName) ? DBNull.Value : (object)companyName);
                    command.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrEmpty(phoneNumber) ? DBNull.Value : (object)phoneNumber);
                    command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? DBNull.Value : (object)email);

                    // Если физическое лицо, используем 'address'. Иначе - 'legalAddress'.
                    command.Parameters.AddWithValue("@Address", clientType == "Физическое лицо" && !string.IsNullOrEmpty(address) ? (object)address : DBNull.Value);
                    command.Parameters.AddWithValue("@LegalAddress", clientType == "Юридическое лицо" && !string.IsNullOrEmpty(legalAddress) ? (object)legalAddress : DBNull.Value);

                    command.Parameters.AddWithValue("@INN", string.IsNullOrEmpty(inn) ? DBNull.Value : (object)inn);
                    command.Parameters.AddWithValue("@Director", string.IsNullOrEmpty(director) ? DBNull.Value : (object)director);

                    int rowsAffected = await command.ExecuteNonQueryAsync();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show(_editingClient == null ? "Клиент успешно добавлен!" : "Изменения клиента успешно сохранены!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show(_editingClient == null ? "Не удалось добавить клиента." : "Не удалось сохранить изменения клиента.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при {(_editingClient == null ? "добавлении" : "редактировании")} клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void cmbClientType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbClientType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedClientType = selectedItem.Content.ToString();

                if (selectedClientType == "Физическое лицо")
                {
                    IndividualClientPanel.Visibility = Visibility.Visible;
                    CompanyClientPanel.Visibility = Visibility.Collapsed;

                    // Очистка полей юридического лица при переключении.
                    txtCompanyName.Text = string.Empty;
                    txtCompanyINN.Text = string.Empty;
                    txtCompanyPhone.Text = string.Empty;
                    txtCompanyEmail.Text = string.Empty;
                    txtCompanyLegalAddress.Text = string.Empty;
                    txtCompanyDirector.Text = string.Empty;
                }
                else if (selectedClientType == "Юридическое лицо")
                {
                    IndividualClientPanel.Visibility = Visibility.Collapsed;
                    CompanyClientPanel.Visibility = Visibility.Visible;

                    // Очистка полей физического лица при переключении.
                    txtIndividualName.Text = string.Empty;
                    txtIndividualPhone.Text = string.Empty;
                    txtIndividualEmail.Text = string.Empty;
                    txtIndividualAddress.Text = string.Empty;
                }
                else
                {
                    // Если выбран "Выбрать" или что-то другое, скрываем обе панели.
                    IndividualClientPanel.Visibility = Visibility.Collapsed;
                    CompanyClientPanel.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic; // Не используется, можно удалить
using System.Globalization;
using System.Linq; // Не используется, можно удалить
using System.Windows;
using System.Windows.Controls;

namespace ServiceCenterOnline.AddEditPage
{
    public partial class AddService : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string connectionString = DbConnection.ConnectionString; // Ваша строка подключения

        public AddService(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
        }

        private void ButAdd(object sender, RoutedEventArgs e)
        {
            string serviceName = txtName.Text.Trim();
            string description = txtDescription.Text.Trim();
            decimal cost;

            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                MessageBox.Show("Пожалуйста, введите название услуги.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtCost.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out cost))
            {
                MessageBox.Show("Пожалуйста, введите корректное значение для стоимости. Используйте только цифры и десятичную точку (например, 123.45).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO Услуги
                                   (Id_сервиса, Id_пользователя, Название_услуги, Описание, Цена)
                                   VALUES
                                   (@ServiceId, @UserId, @ServiceName, @Description, @Cost)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                    command.Parameters.AddWithValue("@UserId", _currentUserId); // ID пользователя, который добавляет услугу
                    command.Parameters.AddWithValue("@ServiceName", serviceName);
                    // Описание может быть NULL, поэтому явно передаем DBNull.Value, если оно пустое
                    command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                    command.Parameters.AddWithValue("@Cost", cost);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Услуга успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true; // Устанавливаем результат диалога для родительского окна
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить услугу.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Код ошибки 1062 для дубликата записи в MySQL
                if (ex.Number == 1062)
                {
                    MessageBox.Show($"Ошибка: Услуга с таким названием уже существует в этом сервисе. {ex.Message}", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Ошибка базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Отменяем операцию
            Close();
        }
    }
}
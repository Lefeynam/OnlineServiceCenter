using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ServiceCenterOnline.AddEditPage
{
    public partial class AddPersonal : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string connectionString = DbConnection.ConnectionString;

        public AddPersonal(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            dpHireDate.SelectedDate = DateTime.Today; // Устанавливаем текущую дату по умолчанию
            if (cmbPosition.Items.Count > 0)
            {
                cmbPosition.SelectedIndex = 0; // Выбираем первый элемент ComboBox по умолчанию
            }
        }

        private void ButAdd(object sender, RoutedEventArgs e)
        {
            string fio = txtFIO.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string position = (cmbPosition.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(fio) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(position))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля (ФИО, Телефон, Должность).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtZarplata.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out decimal salary))
            {
                MessageBox.Show("Пожалуйста, введите корректное значение для зарплаты.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? hireDate = dpHireDate.SelectedDate;
            if (hireDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату найма.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO Сотрудники
                                   (Id_сервиса, Id_пользователя, ФИО, Телефон, Должность, Зарплата, Дата_найма)
                                   VALUES
                                   (@ServiceId, @UserId, @FIO, @Phone, @Position, @Salary, @HireDate)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                    // Передаём фактический ID пользователя, если это необходимо.
                    // Если поле `Id_пользователя` может быть NULL, используйте `DBNull.Value`.
                    command.Parameters.AddWithValue("@UserId", _currentUserId);
                    command.Parameters.AddWithValue("@FIO", fio);
                    command.Parameters.AddWithValue("@Phone", phone);
                    command.Parameters.AddWithValue("@Position", position);
                    command.Parameters.AddWithValue("@Salary", salary);
                    command.Parameters.AddWithValue("@HireDate", hireDate.Value);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Сотрудник успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true; // Устанавливаем результат диалога для родительского окна
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить сотрудника.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Код ошибки 1062 для дубликата записи в MySQL
                if (ex.Number == 1062)
                {
                    MessageBox.Show($"Ошибка: Сотрудник с таким номером телефона уже существует в этом сервисе. {ex.Message}", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
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
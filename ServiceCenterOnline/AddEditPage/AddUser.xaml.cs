using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks; // Для асинхронных операций
using System.Windows;
using System.Windows.Controls;

namespace ServiceCenterOnline.AddEditPage
{
    public class EmployeeItem
    {
        public int Id { get; set; }
        public string FIO { get; set; }

        public override string ToString()
        {
            return FIO;
        }
    }

    public partial class AddUser : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string connectionString = DbConnection.ConnectionString;

        public AddUser(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            if (cmbPosition.Items.Count > 0)
            {
                cmbPosition.SelectedIndex = 0;
            }

            Loaded += AddUser_Loaded;
        }

        private async void AddUser_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AddUser_Loaded;

            await LoadEmployeesForComboBox(); 
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private async Task LoadEmployeesForComboBox()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Получаем ID сотрудников, которые уже имеют учетную запись в текущем сервисе
                    HashSet<int> assignedEmployeeIds = new HashSet<int>();
                    string getAssignedEmployeesQuery = @"
                        SELECT ID_сотрудника
                        FROM Пользователи
                        WHERE Id_сервиса = @ServiceId AND ID_сотрудника IS NOT NULL";
                    using (MySqlCommand cmdAssigned = new MySqlCommand(getAssignedEmployeesQuery, connection))
                    {
                        cmdAssigned.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                        using (MySqlDataReader reader = (MySqlDataReader)await cmdAssigned.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                assignedEmployeeIds.Add(reader.GetInt32("ID_сотрудника"));
                            }
                        }
                    }

                    // Получаем всех сотрудников для текущего сервиса, исключая уже назначенных
                    string getEmployeesQuery = @"
                        SELECT ID_сотрудника, ФИО
                        FROM Сотрудники
                        WHERE Id_сервиса = @ServiceId";
                    using (MySqlCommand cmdEmployees = new MySqlCommand(getEmployeesQuery, connection))
                    {
                        cmdEmployees.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                        using (MySqlDataReader reader = (MySqlDataReader)await cmdEmployees.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int employeeId = reader.GetInt32("ID_сотрудника");
                                if (!assignedEmployeeIds.Contains(employeeId))
                                {
                                    cmbFIO.Items.Add(new EmployeeItem
                                    {
                                        Id = employeeId,
                                        FIO = reader.GetString("ФИО")
                                    });
                                }
                            }
                        }
                    }

                    if (cmbFIO.Items.Count > 0)
                    {
                        cmbFIO.SelectedIndex = 0; // Выбираем первый элемент по умолчанию
                    }
                    else
                    {
                        MessageBox.Show("Все сотрудники уже привязаны к учетным записям или нет доступных сотрудников для привязки.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e) // Переименован для соответствия общей конвенции
        {
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();
            string role = (cmbPosition.SelectedItem as ComboBoxItem)?.Content?.ToString();
            int? employeeId = (cmbFIO.SelectedItem as EmployeeItem)?.Id;

            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Пожалуйста, введите логин.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, введите пароль.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show("Пожалуйста, выберите должность (роль) пользователя.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (employeeId == null)
            {
                MessageBox.Show("Пожалуйста, выберите ФИО сотрудника.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string hashedPassword = HashPassword(password);

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"INSERT INTO Пользователи
                                   (Id_сервиса, Логин, Пароль, Роль, ID_сотрудника)
                                   VALUES
                                   (@ServiceId, @Login, @Password, @Role, @EmployeeId)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Password", hashedPassword);
                    command.Parameters.AddWithValue("@Role", role);
                    // Использование .Value безопасно, так как employeeId уже проверен на null.
                    command.Parameters.AddWithValue("@EmployeeId", employeeId.Value);

                    // Выполняем вставку пользователя и получаем сгенерированный ID
                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    long newUserId = command.LastInsertedId; // Получаем ID нового пользователя

                    if (rowsAffected > 0)
                    {
                        // Если был выбран сотрудник для привязки, обновляем его запись в таблице Сотрудники
                        if (employeeId.HasValue)
                        {
                            string updateEmployeeQuery = @"UPDATE Сотрудники
                                                        SET Id_пользователя = @NewUserId
                                                        WHERE ID_сотрудника = @EmployeeId";
                            MySqlCommand updateCommand = new MySqlCommand(updateEmployeeQuery, connection);
                            updateCommand.Parameters.AddWithValue("@NewUserId", newUserId);
                            updateCommand.Parameters.AddWithValue("@EmployeeId", employeeId.Value);
                            await updateCommand.ExecuteNonQueryAsync();
                        }

                        MessageBox.Show("Пользователь успешно добавлен и привязан к сотруднику!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true; // Устанавливаем результат диалога для родительского окна
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить пользователя.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Проверяем на дубликаты логина (код ошибки 1062 для дубликата записи в MySQL)
                if (ex.Number == 1062)
                {
                    MessageBox.Show($"Ошибка: Пользователь с логином '{login}' уже существует.", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
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
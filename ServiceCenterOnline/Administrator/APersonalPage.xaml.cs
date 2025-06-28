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
    public partial class APersonalPage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        // Используем приватное поле _employees для хранения всех загруженных сотрудников
        private ObservableCollection<Employee> _employees = new ObservableCollection<Employee>();
        // ICollectionView для эффективной фильтрации на стороне клиента
        private ICollectionView _employeesView;

        public APersonalPage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            LoadData(); // Загружаем все данные один раз при инициализации страницы

            // Инициализируем View для DataGrid
            _employeesView = CollectionViewSource.GetDefaultView(_employees);
            DGridPeronal.ItemsSource = _employeesView; // Привязываем DataGrid к представлению

            searchbox.Text = "Поиск по ФИО";
            searchbox.Foreground = Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Этот метод теперь загружает ВСЕХ сотрудников для текущего сервиса
        private void LoadData()
        {
            _employees.Clear(); // Очищаем коллекцию перед загрузкой новых данных
            const string query = "SELECT ID_сотрудника, ФИО, Телефон, Должность, Зарплата, Дата_найма, Id_сервиса, Id_пользователя FROM Сотрудники WHERE Id_сервиса = @Id_сервиса";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id_сервиса", _currentServiceId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _employees.Add(new Employee
                                {
                                    ID_сотрудника = reader.GetInt32("ID_сотрудника"),
                                    ФИО = reader.IsDBNull(reader.GetOrdinal("ФИО")) ? string.Empty : reader.GetString("ФИО"),
                                    Телефон = reader.IsDBNull(reader.GetOrdinal("Телефон")) ? string.Empty : reader.GetString("Телефон"),
                                    Должность = reader.IsDBNull(reader.GetOrdinal("Должность")) ? string.Empty : reader.GetString("Должность"),
                                    Зарплата = reader.IsDBNull(reader.GetOrdinal("Зарплата")) ? 0 : reader.GetDecimal("Зарплата"),
                                    Дата_найма = reader.IsDBNull(reader.GetOrdinal("Дата_найма")) ? (DateTime?)null : reader.GetDateTime("Дата_найма"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    Id_пользователя = reader.IsDBNull(reader.GetOrdinal("Id_пользователя")) ? (int?)null : reader.GetInt32("Id_пользователя")
                                });
                            }
                        }
                    }
                }
                // DGridPeronal.ItemsSource уже привязан к _employeesView, который в свою очередь привязан к _employees.
                // Нет необходимости повторно присваивать ItemSource.
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке сотрудников: {ex.Message}",
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
            AddPersonal addPersonalWindow = new AddPersonal(_currentUserId, _currentServiceId);
            addPersonalWindow.ShowDialog();
            // Важно: после закрытия окна добавления, перезагружаем данные,
            // чтобы новый сотрудник появился в DataGrid
            LoadData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DGridPeronal.SelectedItem is Employee selectedEmployee)
            {
                // Проверяем, есть ли у сотрудника связанный пользователь
                if (selectedEmployee.Id_пользователя.HasValue)
                {
                    MessageBox.Show("Невозможно удалить сотрудника, так как с ним связана учетная запись пользователя. Сначала удалите связанную учетную запись пользователя.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {selectedEmployee.ФИО}? Это также удалит связанные заказы и записи в логах.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                    {
                        connection.Open();
                        MySqlTransaction transaction = connection.BeginTransaction(); // Начинаем транзакцию

                        try
                        {
                            // 1. Обновляем все связанные заказы, устанавливая ID_сотрудника в NULL
                            // Это безопасно, если ID_сотрудника в Заказах допускает NULL
                            // ИЛИ если внешний ключ FK_Заказов_Сотрудники имеет ON DELETE SET NULL.
                            // Согласно servicecenter(1).sql, FK_Заказов_Сотрудники имеет ON DELETE SET NULL,
                            // поэтому этот UPDATE не строго обязателен, но может быть полезен,
                            // если вы хотите явного контроля или если FK изменится.
                            // Лучше полагаться на ON DELETE SET NULL, если он настроен.
                            // Если же в `Заказы` ID_сотрудника NOT NULL, то нужно либо запрещать удаление
                            // сотрудника с заказами, либо удалять заказы (см. ниже).
                            // В текущей схеме FK_Заказов_Сотрудники ON DELETE SET NULL, так что это безопасно.

                            // Проверяем, есть ли в таблице `Лог_заказов` внешний ключ на `Сотрудники` с ON DELETE CASCADE
                            // Если `Лог_заказов` имеет `FK_Лог_заказов_Сотрудники` с ON DELETE CASCADE, то ручное удаление не требуется.
                            // Если нет, то нужно удалить записи из `Лог_заказов`.
                            // Судя по servicecenter(1).sql, FK_Лог_заказов_Сотрудники не имеет ON DELETE CASCADE.
                            // Поэтому нужно явно удалить.

                            // 2. Удаляем связанные записи из таблицы `Лог_заказов`
                            string deleteOrderLogsQuery = "DELETE FROM Лог_заказов WHERE ID_сотрудника = @employeeId";
                            using (MySqlCommand commandOrderLogs = new MySqlCommand(deleteOrderLogsQuery, connection, transaction))
                            {
                                commandOrderLogs.Parameters.AddWithValue("@employeeId", selectedEmployee.ID_сотрудника);
                                commandOrderLogs.ExecuteNonQuery();
                            }

                            // 3. Теперь безопасно удаляем сотрудника
                            string query = "DELETE FROM Сотрудники WHERE ID_сотрудника = @id AND Id_сервиса = @Id_сервиса";
                            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@id", selectedEmployee.ID_сотрудника);
                                command.Parameters.AddWithValue("@Id_сервиса", _currentServiceId);
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit(); // Подтверждаем транзакцию
                                    MessageBox.Show("Сотрудник успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadData(); // Обновляем данные
                                }
                                else
                                {
                                    transaction.Rollback(); // Откатываем, если сотрудник не найден
                                    MessageBox.Show("Сотрудник не найден в базе данных или не принадлежит вашему сервису.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (MySqlException ex)
                        {
                            transaction.Rollback(); // Откатываем при ошибке базы данных
                            MessageBox.Show($"Ошибка при удалении сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback(); // Откатываем при любой другой ошибке
                            MessageBox.Show($"Произошла непредвиденная ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Здесь должна быть логика для открытия окна редактирования сотрудника.
            // Пример:
            // if (DGridPeronal.SelectedItem is Employee selectedEmployee)
            // {
            //     EditPersonalWindow editWindow = new EditPersonalWindow(selectedEmployee.ID_сотрудника, _currentServiceId);
            //     editWindow.ShowDialog();
            //     LoadData(); // Обновить данные после редактирования
            // }
            // else
            // {
            //     MessageBox.Show("Пожалуйста, выберите сотрудника для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            // }
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
                _employeesView.Filter = null; // Сброс фильтра
                return;
            }

            // Применяем фильтр к ICollectionView
            _employeesView.Filter = item =>
            {
                Employee employee = item as Employee;
                if (employee == null) return false;

                // Сравниваем ФИО без учета регистра
                return !string.IsNullOrEmpty(employee.ФИО) &&
                       employee.ФИО.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            // Можно добавить сообщение, если фильтр не дал результатов
            if (_employeesView.IsEmpty && (searchbox.Text != "Поиск по ФИО" && !string.IsNullOrEmpty(searchbox.Text)))
            {
                MessageBox.Show("По вашему запросу ничего не найдено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

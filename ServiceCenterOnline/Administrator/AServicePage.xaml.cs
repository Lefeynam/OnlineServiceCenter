using MySql.Data.MySqlClient;
using ServiceCenterOnline.AddEditPage;
using ServiceCenterOnline.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Добавлено для ICollectionView
using System.Windows.Media;

namespace ServiceCenterOnline.Administrator
{
    /// <summary>
    /// Логика взаимодействия для AServicePage.xaml
    /// </summary>
    public partial class AServicePage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        // Используем приватное поле _services для хранения всех загруженных услуг
        private ObservableCollection<Service> _services = new ObservableCollection<Service>();
        // ICollectionView для эффективной фильтрации на стороне клиента
        private ICollectionView _servicesView;

        public AServicePage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            LoadData(); // Загружаем все данные один раз при инициализации страницы

            // Инициализируем View для DataGrid
            _servicesView = CollectionViewSource.GetDefaultView(_services);
            DGridService.ItemsSource = _servicesView; // Привязываем DataGrid к представлению

            searchbox.Text = "Поиск по Названию";
            searchbox.Foreground = Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Этот метод теперь загружает ВСЕ услуги для текущего сервиса
        private void LoadData()
        {
            _services.Clear(); // Очищаем коллекцию перед загрузкой новых данных
            const string query = "SELECT ID_услуги, Id_сервиса, Id_пользователя, Название_услуги, Описание, Цена FROM Услуги WHERE Id_сервиса = @serviceId";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                _services.Add(new Service
                                {
                                    ID_услуги = reader.GetInt32("ID_услуги"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    // Id_пользователя в базе по CREATE TABLE `Услуги` является int DEFAULT NULL,
                                    // поэтому его нужно читать как nullable
                                    Id_пользователя = reader.IsDBNull(reader.GetOrdinal("Id_пользователя")) ? (int?)null : reader.GetInt32("Id_пользователя"),
                                    Название_услуги = reader.IsDBNull(reader.GetOrdinal("Название_услуги")) ? string.Empty : reader.GetString("Название_услуги"),
                                    Описание = reader.IsDBNull(reader.GetOrdinal("Описание")) ? string.Empty : reader.GetString("Описание"),
                                    Цена = reader.GetDecimal("Цена") // NOT NULL в базе
                                });
                            }
                        }
                    }
                }
                // DGridService.ItemsSource уже привязан к _servicesView, который в свою очередь привязан к _services.
                // Нет необходимости повторно присваивать ItemSource.
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке услуг: {ex.Message}",
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
            AddService addServiceWindow = new AddService(_currentUserId, _currentServiceId);
            addServiceWindow.ShowDialog();
            // После закрытия окна добавления, перезагружаем данные,
            // чтобы новая услуга появилась в DataGrid
            LoadData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DGridService.SelectedItem is Service selectedService)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить услугу '{selectedService.Название_услуги}'? (Связанные заказы будут обновлены: ID_услуги станет NULL)",
                                             "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                    {
                        // Транзакция не строго необходима, так как ON DELETE SET NULL справляется с зависимостью Заказов.
                        // Но если были бы другие зависимости без каскада, транзакция была бы важна.
                        // Оставляем транзакцию для демонстрации хорошей практики и возможности добавления других зависимостей.
                        connection.Open();
                        MySqlTransaction transaction = connection.BeginTransaction();

                        try
                        {
                            // Благодаря FK_Заказов_Услуги с ON DELETE SET NULL, база данных автоматически
                            // установит ID_услуги в NULL для связанных заказов.
                            // Поэтому явный UPDATE запрос здесь не нужен.

                            // Удаляем услугу
                            string query = "DELETE FROM Услуги WHERE ID_услуги = @ID AND Id_сервиса = @serviceId";
                            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", selectedService.ID_услуги);
                                command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit(); // Подтверждаем транзакцию
                                    MessageBox.Show("Услуга успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadData(); // Обновляем данные
                                }
                                else
                                {
                                    transaction.Rollback(); // Откатываем, если услуга не найдена
                                    MessageBox.Show("Услуга не найдена в базе данных или не принадлежит вашему сервису.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (MySqlException ex)
                        {
                            transaction.Rollback(); // Откатываем при ошибке базы данных
                            MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Пожалуйста, выберите услугу для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Здесь должна быть логика для открытия окна редактирования услуги.
            // Пример:
            // if (DGridService.SelectedItem is Service selectedService)
            // {
            //     EditServiceWindow editWindow = new EditServiceWindow(selectedService.ID_услуги, _currentUserId, _currentServiceId);
            //     editWindow.ShowDialog();
            //     LoadData(); // Обновить данные после редактирования
            // }
            // else
            // {
            //     MessageBox.Show("Пожалуйста, выберите услугу для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            // }
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchbox.Text == "Поиск по Названию")
            {
                searchbox.Text = string.Empty;
                searchbox.Foreground = Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchbox.Text))
            {
                searchbox.Text = "Поиск по Названию";
                searchbox.Foreground = Brushes.Gray;
            }
        }

        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = searchbox.Text?.Trim();

            // Если поле пустое или содержит текст-заполнитель, сбрасываем фильтр
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по Названию")
            {
                _servicesView.Filter = null; // Сброс фильтра
                return;
            }

            // Применяем фильтр к ICollectionView
            _servicesView.Filter = item =>
            {
                Service service = item as Service;
                if (service == null) return false;

                // Сравниваем Название_услуги без учета регистра
                return !string.IsNullOrEmpty(service.Название_услуги) &&
                       service.Название_услуги.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            // Можно добавить сообщение, если фильтр не дал результатов
            if (_servicesView.IsEmpty && (searchbox.Text != "Поиск по Названию" && !string.IsNullOrEmpty(searchbox.Text)))
            {
                // Это сообщение будет появляться каждый раз, когда нет совпадений.
                // Возможно, стоит показывать его только один раз или по нажатию Enter.
                // Для простоты оставим как есть, но имейте в виду.
                MessageBox.Show("По вашему запросу ничего не найдено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
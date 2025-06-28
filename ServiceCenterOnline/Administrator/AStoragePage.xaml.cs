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
    /// Логика взаимодействия для AStoragePage.xaml
    /// </summary>
    public partial class AStoragePage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        // Используем приватное поле _storageItems для хранения всех загруженных товаров
        private ObservableCollection<StorageItem> _storageItems = new ObservableCollection<StorageItem>();
        // ICollectionView для эффективной фильтрации на стороне клиента
        private ICollectionView _storageItemsView;

        public AStoragePage(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            LoadData(); // Загружаем все данные один раз при инициализации страницы

            // Инициализируем View для DataGrid
            _storageItemsView = CollectionViewSource.GetDefaultView(_storageItems);
            DGridStorage.ItemsSource = _storageItemsView; // Привязываем DataGrid к представлению

            searchbox.Text = "Поиск по Наименованию";
            searchbox.Foreground = Brushes.Gray;
            searchbox.GotFocus += Searchbox_GotFocus;
            searchbox.LostFocus += Searchbox_LostFocus;
        }

        // Этот метод теперь загружает ВСЕ товары для текущего сервиса
        private void LoadData()
        {
            _storageItems.Clear(); // Очищаем коллекцию перед загрузкой новых данных
            const string query = "SELECT ID_товара, Id_сервиса, Id_пользователя, Наименование, Описание, Количество, Цена_за_единицу, Дата_поступления, Местоположение, Фотография FROM Склад WHERE Id_сервиса = @serviceId";

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
                                var item = new StorageItem
                                {
                                    ID_товара = reader.GetInt32("ID_товара"),
                                    Id_сервиса = reader.GetInt32("Id_сервиса"),
                                    // Id_пользователя в базе по CREATE TABLE `Склад` является int DEFAULT NULL,
                                    // поэтому его нужно читать как nullable
                                    Id_пользователя = reader.IsDBNull(reader.GetOrdinal("Id_пользователя")) ? (int?)null : reader.GetInt32("Id_пользователя"),
                                    Наименование = reader.GetString("Наименование"), // NOT NULL в базе
                                    Описание = reader.IsDBNull(reader.GetOrdinal("Описание")) ? string.Empty : reader.GetString("Описание"),
                                    Количество = reader.GetInt32("Количество"), // NOT NULL в базе
                                    Цена_за_единицу = reader.GetDecimal("Цена_за_единицу"), // NOT NULL в базе
                                    Дата_поступления = reader.GetDateTime("Дата_поступления"), // NOT NULL в базе
                                    Местоположение = reader.IsDBNull(reader.GetOrdinal("Местоположение")) ? string.Empty : reader.GetString("Местоположение")
                                };

                                // Чтение фотографии как byte[]
                                if (!reader.IsDBNull(reader.GetOrdinal("Фотография")))
                                {
                                    item.Фотография = (byte[])reader["Фотография"];
                                }
                                else
                                {
                                    item.Фотография = null;
                                }

                                _storageItems.Add(item);
                            }
                        }
                    }
                }
                // DGridStorage.ItemsSource уже привязан к _storageItemsView, который в свою очередь привязан к _storageItems.
                // Нет необходимости повторно присваивать ItemSource.
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Ошибка базы данных при загрузке товаров: {ex.Message}",
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
            AddStoragetem addStorageItemWindow = new AddStoragetem(_currentUserId, _currentServiceId);
            addStorageItemWindow.ShowDialog();
            // После закрытия окна добавления, перезагружаем данные,
            // чтобы новый товар появился в DataGrid
            LoadData();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DGridStorage.SelectedItem is StorageItem selectedItem)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить товар '{selectedItem.Наименование}'?",
                                             "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (MySqlConnection connection = new MySqlConnection(DbConnection.ConnectionString))
                    {
                        connection.Open();
                        MySqlTransaction transaction = connection.BeginTransaction(); // Используем транзакцию

                        try
                        {
                            // ВАЖНО: Согласно вашей схеме, таблица Заказы НЕ ссылается на ID_товара из Склад.
                            // Поэтому закомментированный код для обновления Заказов здесь не нужен и удален.

                            // Удаляем товар со склада
                            string query = "DELETE FROM Склад WHERE ID_товара = @ID AND Id_сервиса = @serviceId";
                            using (MySqlCommand command = new MySqlCommand(query, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", selectedItem.ID_товара);
                                command.Parameters.AddWithValue("@serviceId", _currentServiceId);
                                int rowsAffected = command.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    transaction.Commit(); // Подтверждаем транзакцию
                                    MessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LoadData(); // Обновляем данные
                                }
                                else
                                {
                                    transaction.Rollback(); // Откатываем, если запись не найдена
                                    MessageBox.Show("Запись не найдена в базе данных или не принадлежит вашему сервису.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        catch (MySqlException ex)
                        {
                            transaction.Rollback(); // Откатываем при ошибке базы данных
                            MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Пожалуйста, выберите запись для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Здесь должна быть логика для открытия окна редактирования товара.
            // Пример:
            // if (DGridStorage.SelectedItem is StorageItem selectedItem)
            // {
            //     EditStorageItemWindow editWindow = new EditStorageItemWindow(selectedItem.ID_товара, _currentUserId, _currentServiceId);
            //     editWindow.ShowDialog();
            //     LoadData(); // Обновить данные после редактирования
            // }
            // else
            // {
            //     MessageBox.Show("Пожалуйста, выберите товар для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            // }
        }

        private void Searchbox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchbox.Text == "Поиск по Наименованию")
            {
                searchbox.Text = string.Empty;
                searchbox.Foreground = Brushes.Black;
            }
        }

        private void Searchbox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchbox.Text))
            {
                searchbox.Text = "Поиск по Наименованию";
                searchbox.Foreground = Brushes.Gray;
            }
        }

        private void Searchbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = searchbox.Text?.Trim();

            // Если поле пустое или содержит текст-заполнитель, сбрасываем фильтр
            if (string.IsNullOrEmpty(searchTerm) || searchTerm == "Поиск по Наименованию")
            {
                _storageItemsView.Filter = null; // Сброс фильтра
                return;
            }

            // Применяем фильтр к ICollectionView
            _storageItemsView.Filter = item =>
            {
                StorageItem storageItem = item as StorageItem;
                if (storageItem == null) return false;

                // Сравниваем Наименование без учета регистра
                return !string.IsNullOrEmpty(storageItem.Наименование) &&
                       storageItem.Наименование.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
            };

            // Можно добавить сообщение, если фильтр не дал результатов
            if (_storageItemsView.IsEmpty && (searchbox.Text != "Поиск по Наименованию" && !string.IsNullOrEmpty(searchbox.Text)))
            {
                MessageBox.Show("По вашему запросу ничего не найдено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
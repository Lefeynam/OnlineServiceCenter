using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ServiceCenterOnline.AddEditPage
{
    public partial class AddStoragetem : Window
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string connectionString = DbConnection.ConnectionString;
        private byte[] _imageData; // Для хранения данных изображения

        public AddStoragetem(int userId, int serviceId)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            // Устанавливаем текущую дату как значение по умолчанию для DatePicker
            datePickerArrival.SelectedDate = DateTime.Today;
        }

        private void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Фильтр для выбора только файлов изображений
            openFileDialog.Filter = "Файлы изображений (*.png;*.jpeg;*.jpg;*.gif;*.bmp)|*.png;*.jpeg;*.jpg;*.gif;*.bmp|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Читаем файл в массив байтов
                    _imageData = File.ReadAllBytes(openFileDialog.FileName);

                    // Отображаем изображение в Image контроле
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(_imageData);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // Загружаем изображение сразу
                    bitmap.EndInit();
                    ProductImage.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _imageData = null; // Сбрасываем данные изображения в случае ошибки
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string itemName = txtItemName.Text.Trim();
            string description = txtDescription.Text.Trim();
            int quantity;
            decimal price;
            DateTime? arrivalDate = datePickerArrival.SelectedDate;
            string location = txtLocation.Text.Trim();

            // Валидация входных данных
            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("Пожалуйста, введите наименование комплектующего.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(txtQuantity.Text.Trim(), out quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное количество (целое положительное число).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtPrice.Text.Trim(), NumberStyles.Currency, CultureInfo.CurrentCulture, out price) || price <= 0)
            {
                MessageBox.Show("Пожалуйста, введите корректную цену (положительное число).", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (arrivalDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату поступления.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(location))
            {
                MessageBox.Show("Пожалуйста, введите местоположение.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO Склад
                                   (Id_сервиса, Id_пользователя, Наименование, Описание, Количество, Цена_за_единицу, Дата_поступления, Местоположение, Фотография)
                                   VALUES
                                   (@ServiceId, @UserId, @ItemName, @Description, @Quantity, @Price, @ArrivalDate, @Location, @Photo)";

                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@ServiceId", _currentServiceId);
                    command.Parameters.AddWithValue("@UserId", _currentUserId); // ID пользователя, который добавляет комплектующее
                    command.Parameters.AddWithValue("@ItemName", itemName);
                    // Описание может быть NULL, поэтому явно передаем DBNull.Value, если оно пустое
                    command.Parameters.AddWithValue("@Description", string.IsNullOrEmpty(description) ? DBNull.Value : (object)description);
                    command.Parameters.AddWithValue("@Quantity", quantity);
                    command.Parameters.AddWithValue("@Price", price);
                    command.Parameters.AddWithValue("@ArrivalDate", arrivalDate.Value);
                    command.Parameters.AddWithValue("@Location", location);
                    // Если изображение выбрано, добавляем его, иначе DBNull.Value
                    command.Parameters.AddWithValue("@Photo", (object)_imageData ?? DBNull.Value);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Комплектующее успешно добавлено на склад!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true; // Устанавливаем результат диалога для родительского окна
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось добавить комплектующее на склад.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Код ошибки 1062 для дубликата записи в MySQL
                if (ex.Number == 1062)
                {
                    MessageBox.Show($"Ошибка: Комплектующее с таким наименованием уже существует в этом сервисе. {ex.Message}", "Ошибка добавления", MessageBoxButton.OK, MessageBoxImage.Error);
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
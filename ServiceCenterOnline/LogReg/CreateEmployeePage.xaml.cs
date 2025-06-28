using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ServiceCenterOnline.LogReg
{
    /// <summary>
    /// Логика взаимодействия для CreateEmployeePage.xaml
    /// </summary>
    public partial class CreateEmployeePage : Page
    {
        public CreateEmployeePage()
        {
            InitializeComponent();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(new RegisterUserPage());
            }
            else
            {
                MessageBox.Show("Не удалось выполнить навигацию. Возможно, NavigationService недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

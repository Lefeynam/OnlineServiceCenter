using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ServiceCenterOnline.LogReg
{
    /// <summary>
    /// Логика взаимодействия для RegisterUserPage.xaml
    /// </summary>
    public partial class RegisterUserPage : Page
    {
        public RegisterUserPage()
        {
            InitializeComponent();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
            {
                NavigationService.Navigate(new LoginPage());
            }
            else
            {
                MessageBox.Show("Не удалось выполнить навигацию. Возможно, NavigationService недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

using ServiceCenterOnline.LogReg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ServiceCenterOnline.Master
{
    /// <summary>
    /// Логика взаимодействия для MainAMasterPage.xaml
    /// </summary>
    public partial class MainAMasterPage : Page
    {
        private int _currentUserId;
        private int _currentServiceId;
        private readonly string _fio;
        private readonly string _role;
        public MainAMasterPage(int userId, int serviceId, string fio, string role)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            _fio = fio;
            _role = role;

            Name.Text = _fio;
            AvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/pic_profile.png"));
        }

        private void TextBlock_Order(object sender, MouseButtonEventArgs e)
        {

        }

        private void TextBlock_Settings(object sender, MouseButtonEventArgs e)
        {

        }

        private void ExitApp(object sender, MouseButtonEventArgs e)
        {
            if (NavigationService != null)
            {
                while (NavigationService.CanGoBack)
                {
                    NavigationService.RemoveBackEntry();
                }
                NavigationService.Navigate(new LoginPage());
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}

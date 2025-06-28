using ServiceCenterOnline.Administrator;
using ServiceCenterOnline.LogReg;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

namespace ServiceCenterOnline.Manager
{
    /// <summary>
    /// Логика взаимодействия для ManagerMainPage.xaml
    /// </summary>
    public partial class ManagerMainPage : Page
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string _fio;
        private readonly string _role;

        // Приватные поля для кеширования страниц
        private ManagerClientPage _managerClientPage;
        private ManagerOrderPage _managerOrderPage;
        private SettingsPage _settingsPage; 


        public ManagerMainPage(int userId, int serviceId, string fio, string role)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;

            _fio = fio ?? string.Empty;
            _role = role ?? string.Empty;

            Name.Text = _fio;
            AvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/pic_profile.png"));

            

            // Инициализация страниц при создании ManagerMainPage
            _managerClientPage = new ManagerClientPage(_currentUserId, _currentServiceId);
            _managerOrderPage = new ManagerOrderPage(_currentUserId, _currentServiceId);
            _settingsPage = new SettingsPage(_currentUserId, _currentServiceId);

            OpenFirstPage();
        }

        private void OpenFirstPage()
        {
            ResetButtonStates();
            if (ManagerFrame != null)
            {
                BtnOrder.Style = (Style)Resources["SelectedButtonStyle"];
                ManagerFrame.Navigate(_managerOrderPage);
            }
        }

        private void ResetButtonStates()
        {
            BtnOrder.Style = (Style)Resources["RoundedButtonStyleBorder"];
            BtnClient.Style = (Style)Resources["RoundedButtonStyleBorder"];
            BtnSettings.Style = (Style)Resources["RoundedButtonStyleBorder"];
            BtnExit.Style = (Style)Resources["RoundedButtonStyleBorder"];
        }

        private void BtnClient_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (ManagerFrame != null)
            {
                BtnClient.Style = (Style)Resources["SelectedButtonStyle"];
                ManagerFrame.Navigate(_managerClientPage);
            }
        }

        private void BtnOrder_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (ManagerFrame != null)
            {
                BtnOrder.Style = (Style)Resources["SelectedButtonStyle"];
                ManagerFrame.Navigate(_managerOrderPage);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (ManagerFrame != null)
            {
                BtnSettings.Style = (Style)Resources["SelectedButtonStyle"];
                ManagerFrame.Navigate(_settingsPage);
            }
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
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
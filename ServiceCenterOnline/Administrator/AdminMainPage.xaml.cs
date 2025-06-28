using ServiceCenterOnline.Administrator; // Убедитесь, что этот using есть, так как AUsersPage и другие находятся здесь
using ServiceCenterOnline.LogReg;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ServiceCenterOnline.Administrator
{
    /// <summary>
    /// Логика взаимодействия для AdminMainPage.xaml
    /// </summary>
    public partial class AdminMainPage : Page
    {
        private readonly int _currentUserId;
        private readonly int _currentServiceId;
        private readonly string _fio;
        private readonly string _role;

        // Приватные поля для хранения кэшированных экземпляров страниц
        private AUsersPage _usersPage;
        private APersonalPage _personalPage;
        private AClientPage _clientPage;
        private AStoragePage _storagePage;
        private AServicePage _servicePage;
        private SettingsPage _settingsPage;

        public AdminMainPage(int userId, int serviceId, string fio, string role)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentServiceId = serviceId;
            _fio = fio;
            _role = role;

            Name.Text = _fio;
            AvatarImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/pic_profile.png"));

            _usersPage = new AUsersPage(_currentUserId, _currentServiceId);
            _personalPage = new APersonalPage(_currentUserId, _currentServiceId);
            _clientPage = new AClientPage(_currentUserId, _currentServiceId);
            _storagePage = new AStoragePage(_currentUserId, _currentServiceId);
            _servicePage = new AServicePage(_currentUserId, _currentServiceId);
            _settingsPage = new SettingsPage(_currentUserId, _currentServiceId);
            OpenFirstPage();
        }

        private void OpenFirstPage()
        {
            ResetButtonStates();

                UsersButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_usersPage);
            
        }

        private void ResetButtonStates()
        {
            UsersButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            PersonaleButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            ServiceButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            ClientButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            StorageButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            SettingsButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
            ExitButton.Style = (Style)Resources["RoundedButtonStyleBorder"];
        }


        private void OpenUsersPage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                UsersButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_usersPage);
            }
        }

        private void OpenPersonalePage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                PersonaleButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_personalPage);
            }
        }

        private void OpenClientPage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                ClientButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_clientPage);
            }
        }

        private void OpenStoragePage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                StorageButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_storagePage);
            }
        }

        private void OpenServicePage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                ServiceButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_servicePage);
            }
        }

        private void OpenSettingsPage(object sender, RoutedEventArgs e)
        {
            ResetButtonStates();
            if (AdminFrame != null)
            {
                SettingsButton.Style = (Style)Resources["SelectedButtonStyle"];
                AdminFrame.Navigate(_settingsPage);
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
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
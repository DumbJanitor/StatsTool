using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StatsTool.Views;

namespace StatsTool
{
    public partial class MainWindow : Window
    {
        // =====================================================
        // MAIN WINDOW
        //
        // FRONTEND:
        // Handles navigation, sidebar, window controls,
        // and application-level window sizing.
        //
        // DATABASE/BACKEND:
        // No database or business logic belongs here.
        // =====================================================


        private bool _sidebarExpanded = true;


        private readonly Brush _selectedBackground =
            new SolidColorBrush(Color.FromRgb(0, 83, 80));

        private readonly Brush _selectedBorder =
            new SolidColorBrush(Color.FromRgb(0, 196, 174));


        // =====================================================
        // STARTUP
        // =====================================================

        public MainWindow()
        {
            InitializeComponent();

            ApplyInitialWindowSize();

            ShowStart();
        }


        // =====================================================
        // SCREEN-AWARE INITIAL WINDOW SIZE
        // =====================================================

        private void ApplyInitialWindowSize()
        {
            // Get the available desktop area, excluding
            // the Windows taskbar.

            Rect workArea = SystemParameters.WorkArea;

            // Use a comfortable desktop window size.
            // Reduce it automatically on smaller displays.

            Width = Math.Min(
                1200,
                workArea.Width * 0.88);

            Height = Math.Min(
                760,
                workArea.Height * 0.86);

            // Center the window within the usable area.

            Left = workArea.Left +
                   (workArea.Width - Width) / 2;

            Top = workArea.Top +
                  (workArea.Height - Height) / 2;
        }


        // =====================================================
        // AUTHENTICATION NAVIGATION
        // =====================================================

        public void ShowStart()
        {
            ShowAuthView(new StartView());
        }

        public void ShowLogin()
        {
            ShowAuthView(new LoginView());
        }

        public void ShowSignUp()
        {
            ShowAuthView(new SignUpView());
        }


        public void ShowAuthView(UserControl view)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            AppShell.Visibility = Visibility.Collapsed;
            AuthHost.Visibility = Visibility.Visible;

            AuthContent.Content = view;
        }


        // =====================================================
        // APPLICATION NAVIGATION
        // =====================================================

        public void ShowDashboard()
        {
            NavigateTo("Dashboard");
        }


        public void NavigateTo(string pageName)
        {
            UserControl selectedView = pageName switch
            {
                "Dashboard" => new DashboardView(),

                "Inventory" => new InventoryView(),

                "Daily Entry" => new DailyEntryView(),

                "Comparisons" => new ComparisonsView(),

                "Insights" => new InsightsView(),

                "Settings" => new SettingsView(),

                _ => throw new ArgumentException(
                    "Unknown SalesScope page: " + pageName)
            };

            ShowAppView(selectedView, pageName);
        }


        public void ShowAppView(
            UserControl view,
            string pageName)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            AuthHost.Visibility = Visibility.Collapsed;
            AppShell.Visibility = Visibility.Visible;

            MainContent.Content = view;

            SetActiveNavigation(pageName);
        }


        // =====================================================
        // SIDEBAR NAVIGATION
        // =====================================================

        private void NavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is string pageName)
            {
                NavigateTo(pageName);
            }
        }


        private void SetActiveNavigation(string pageName)
        {
            Button[] navigationButtons =
            {
                DashboardButton,
                InventoryButton,
                DailyEntryButton,
                ComparisonsButton,
                InsightsButton,
                SettingsButton
            };

            foreach (Button button in navigationButtons)
            {
                bool isSelected =
                    button.Tag?.ToString() == pageName;

                button.Background = isSelected
                    ? _selectedBackground
                    : Brushes.Transparent;

                button.BorderBrush = isSelected
                    ? _selectedBorder
                    : Brushes.Transparent;
            }
        }


        // =====================================================
        // SIDEBAR COLLAPSE / EXPAND
        // =====================================================

        private void HamburgerButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _sidebarExpanded = !_sidebarExpanded;

            if (_sidebarExpanded)
            {
                SidebarColumn.Width = new GridLength(226);

                NavigationItems.Visibility =
                    Visibility.Visible;

                LogoutButton.Visibility =
                    Visibility.Visible;
            }
            else
            {
                SidebarColumn.Width = new GridLength(55);

                NavigationItems.Visibility =
                    Visibility.Collapsed;

                LogoutButton.Visibility =
                    Visibility.Collapsed;
            }
        }


        // =====================================================
        // LOGOUT
        // =====================================================

        private void LogoutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // BACKEND INTEGRATION:
            // The backend must invalidate the active user
            // session before showing Login.
            //
            // Do not delete account records during logout.

            ShowLogin();
        }


        // =====================================================
        // WINDOW CONTROLS
        // =====================================================

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }


        private void MaximizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }


        private void MainWindow_StateChanged(
            object sender,
            EventArgs e)
        {
            // Keep the maximize icon in sync even if
            // the user changes window state another way.

            if (MaximizeButton != null)
            {
                MaximizeButton.Content =
                    WindowState == WindowState.Maximized
                    ? "❐"
                    : "□";
            }
        }


        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }
    }
}
using System;
using System.Windows;
using System.Windows.Controls;

namespace StatsTool.Views
{
    public partial class LoginView : UserControl
    {
        private bool _passwordVisible;

        public LoginView()
        {
            InitializeComponent();
        }

        // =====================================================
        // GET THE EXISTING MAIN WINDOW
        // =====================================================

        private MainWindow? GetMainWindow()
        {
            return Window.GetWindow(this) as MainWindow;
        }

        // =====================================================
        // BACK TO START PAGE
        // =====================================================

        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            GetMainWindow()?.ShowStart();
        }

        // =====================================================
        // PASSWORD VISIBILITY — FRONTEND TEAM
        // =====================================================
        // PasswordInput remains the source used by login logic.
        // When a user reveals the password, a normal TextBox displays
        // it. When hidden again, the visible text is cleared.
        // BACKEND TEAM: Never log this text or persist it locally.
        // DATABASE TEAM: No database action belongs to this toggle.

        private void PasswordVisibilityButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_passwordVisible)
            {
                VisiblePasswordTextBox.Text = PasswordInput.Password;
                PasswordInput.Visibility = Visibility.Collapsed;
                VisiblePasswordTextBox.Visibility = Visibility.Visible;
                PasswordVisibilityButton.Content = "Hide";
                _passwordVisible = true;
                VisiblePasswordTextBox.Focus();
                VisiblePasswordTextBox.CaretIndex =
                    VisiblePasswordTextBox.Text.Length;
            }
            else
            {
                PasswordInput.Password = VisiblePasswordTextBox.Text;
                VisiblePasswordTextBox.Clear();
                VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
                PasswordVisibilityButton.Content = "Show";
                _passwordVisible = false;
                PasswordInput.Focus();
            }
        }

        // =====================================================
        // LOGIN
        // =====================================================

        private void LoginButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            FeedbackText.Visibility = Visibility.Collapsed;

            string username = UsernameTextBox.Text.Trim();

            // Keep the existing authentication source synchronized
            // if the user last typed while the password was visible.
            if (_passwordVisible)
            {
                PasswordInput.Password = VisiblePasswordTextBox.Text;
            }

            // FRONTEND VALIDATION:
            // Only verify that required fields are filled.
            // Actual credential verification belongs to backend.

            if (string.IsNullOrWhiteSpace(username) ||
                PasswordInput.SecurePassword.Length == 0)
            {
                ShowError("Please enter your username and password.");
                return;
            }

            // =================================================
            // BACKEND TEAM - AUTHENTICATION INTEGRATION
            // =================================================
            //
            // Replace the temporary navigation below with:
            //
            // 1. Read username and password securely.
            // 2. Submit credentials to the authentication service.
            // 3. Await authentication result.
            // 4. Display an error if authentication fails.
            // 5. Establish the authenticated user session.
            // 6. Apply RememberMeCheckBox.IsChecked == true
            //    according to the session persistence policy.
            // 7. Navigate to Dashboard only after success.
            //
            // Do not hardcode usernames or passwords.
            // Do not store passwords in application settings.
            // Do not perform SQL queries inside this view.
            //
            // DATABASE TEAM:
            // Coordinate account lookup and password verification
            // with backend. Keep database access out of UI code.
            //
            // =================================================
            // TEMPORARY FRONTEND PREVIEW
            // =================================================
            //
            // Any non-empty username/password opens Dashboard
            // ONLY so the frontend navigation can be tested.
            //
            // THIS IS NOT REAL AUTHENTICATION.
            // Remove this preview behavior during integration.

            GetMainWindow()?.ShowDashboard();
        }

        // =====================================================
        // SIGN UP NAVIGATION
        // =====================================================

        private void SignUpButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            GetMainWindow()?.ShowSignUp();
        }

        // =====================================================
        // VALIDATION MESSAGE
        // =====================================================

        private void ShowError(string message)
        {
            FeedbackText.Text = message;
            FeedbackText.Visibility = Visibility.Visible;
        }
    }
}
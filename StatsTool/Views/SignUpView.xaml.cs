using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StatsTool.Views
{
    public partial class SignUpView : UserControl
    {
        private bool _passwordVisible;

        public SignUpView()
        {
            InitializeComponent();
        }

        // =====================================================
        // MAIN WINDOW REFERENCE
        // =====================================================

        private StatsTool.MainWindow? GetMainWindow()
        {
            return Window.GetWindow(this) as StatsTool.MainWindow;
        }

        // =====================================================
        // BACK TO LOGIN
        // =====================================================

        private void BackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            GetMainWindow()?.ShowLogin();
        }

        // =====================================================
        // PASSWORD VISIBILITY — FRONTEND TEAM
        // =====================================================
        // Only the password may be revealed; username and email
        // stay as normal, fully readable TextBoxes at all times.
        // Hidden mode clears the plaintext display TextBox.
        // BACKEND TEAM: PasswordInput remains the registration source.
        // DATABASE TEAM: This event must never save a password.

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
        // SIGN UP
        // =====================================================

        private void SignUpButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            FeedbackText.Visibility = Visibility.Collapsed;

            string username = UsernameTextBox.Text.Trim();
            string email = EmailTextBox.Text.Trim();

            // Synchronize when the user typed in revealed mode.
            // Existing backend integration continues to read PasswordInput.
            if (_passwordVisible)
            {
                PasswordInput.Password = VisiblePasswordTextBox.Text;
            }

            // -----------------------------------------------
            // FRONTEND VALIDATION
            // -----------------------------------------------

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                PasswordInput.SecurePassword.Length == 0)
            {
                ShowFeedback(
                    "Please complete all fields.",
                    true);

                return;
            }

            // Basic email format check.
            // Backend must perform authoritative validation.

            bool emailIsValid =
                MailAddress.TryCreate(email, out MailAddress? parsedEmail)
                && string.Equals(
                    parsedEmail?.Address,
                    email,
                    StringComparison.OrdinalIgnoreCase);

            if (!emailIsValid)
            {
                ShowFeedback(
                    "Please enter a valid email address.",
                    true);

                return;
            }

            // =================================================
            // BACKEND TEAM - ACCOUNT REGISTRATION
            // =================================================
            //
            // Replace the preview message below with:
            //
            // 1. Submit username, email, and password securely.
            // 2. Validate all fields on the server.
            // 3. Check for duplicate username/email.
            // 4. Create the account only after validation.
            // 5. Handle errors returned by the backend.
            // 6. On successful registration:
            //
            //       GetMainWindow()?.ShowLogin();
            //
            //    OR follow the team's chosen authentication flow.
            //
            // SECURITY:
            // - Never log or hardcode passwords.
            // - Do not perform SQL queries in this view.
            // - Password storage and hashing belong to backend.
            //
            // DATABASE TEAM:
            // - Coordinate account creation with backend.
            // - Enforce unique username/email constraints.
            // - Provide appropriate database error handling.
            //
            // =================================================
            // TEMPORARY FRONTEND BEHAVIOR
            // =================================================
            //
            // No account is created yet.
            // This message is shown until backend is connected.

            ShowFeedback(
                "Registration is ready for backend integration. " +
                "No account has been created yet.",
                false);
        }

        // =====================================================
        // GOOGLE SIGN UP
        // =====================================================

        private void GoogleSignUpButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // =================================================
            // BACKEND TEAM - GOOGLE AUTHENTICATION
            // =================================================
            //
            // Implement Google OAuth / OpenID Connect.
            //
            // Expected behavior:
            //
            // 1. Begin authentication using the system browser.
            // 2. Complete Google's secure authorization flow.
            // 3. Validate the response through backend.
            // 4. Create or retrieve the corresponding user.
            // 5. Establish an authenticated session.
            // 6. Navigate to Dashboard on success.
            //
            // Do not collect Google passwords in this app.
            // Do not embed client secrets in frontend code.
            //
            // DATABASE TEAM:
            // Coordinate storage of the external provider's
            // account identifier with backend.
            //
            // =================================================
            // TEMPORARY FRONTEND BEHAVIOR
            // =================================================

            ShowFeedback(
                "Google sign-up will be available " +
                "after authentication is connected.",
                false);
        }

        // =====================================================
        // INLINE FEEDBACK
        // =====================================================

        private void ShowFeedback(string message, bool isError)
        {
            FeedbackText.Text = message;

            FeedbackText.Foreground = new SolidColorBrush(
                isError
                    ? Color.FromRgb(195, 59, 59)
                    : Color.FromRgb(0, 143, 131));

            FeedbackText.Visibility = Visibility.Visible;
        }
    }
}
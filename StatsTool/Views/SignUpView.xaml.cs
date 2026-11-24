using System;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;

namespace StatsTool.Views
{
    public partial class SignUpView : UserControl
    {
        private bool _passwordVisible;
        private string connectionString = @"Server=ANDREI-LP;Database=SaleScope;Trusted_Connection=True;TrustServerCertificate=True;";
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

        private async void SignUpButton_Click(
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

            string password = PasswordInput.Password;

            // -----------------------------------------------
            // FRONTEND VALIDATION
            // -----------------------------------------------

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
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
            // DATABASE AUTHENTICATION - ACCOUNT REGISTRATION
            // =================================================
            //
            // 1. Connect to the SQL Server database.
            // 2. Generate a unique user ID.
            // 3. Insert the username, email, and password.
            // 4. Assign the default Client role.
            // 5. Return to Login after successful registration.
            //
            // SECURITY:
            // - Do not hardcode usernames or passwords.
            // - Use SQL parameters to prevent SQL injection.
            // - Password hashing should be implemented before
            //   using this registration system in production.
            //
            // DATABASE TEAM:
            // - U_ID is generated automatically.
            // - U_Email must be unique.
            // - U_Name is stored in the U_Name column.
            //
            // =================================================

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Generate the next user ID.
                    // Format: U0001, U0002, U0003, and so on.

                    string idQuery = @"SELECT ISNULL(MAX(CAST(SUBSTRING(U_ID, 2, LEN(U_ID)) AS INT)), 0) + 1 FROM [User] WHERE U_ID LIKE 'U[0-9]%'";

                    int nextID;

                    using (SqlCommand idCommand =
                        new SqlCommand(idQuery, connection))
                    {
                        nextID = Convert.ToInt32(await idCommand.ExecuteScalarAsync());
                    }

                    string userID = "U" + nextID.ToString("D4");

                    string query = @"
                INSERT INTO [User]
                (U_ID, U_Email, U_Password, U_Name, U_Role)
                VALUES
                (@UserID, @Email, @Password, @Name, @Role)";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userID);
                        command.Parameters.AddWithValue("@Email", email);
                        command.Parameters.AddWithValue("@Password", password);
                        command.Parameters.AddWithValue("@Name", username);
                        command.Parameters.AddWithValue("@Role", "Client");

                        int result = await command.ExecuteNonQueryAsync();

                        if (result > 0)
                        {
                            // REGISTRATION SUCCESSFUL:
                            // The new account has been saved to SQL Server.

                            ShowFeedback(
                                "Account created successfully! You can now log in.",
                                false);

                            // Return to Login after successful registration.
                            GetMainWindow()?.ShowLogin();
                        }
                        else
                        {
                            // REGISTRATION FAILED:
                            // No account was inserted.
                            ShowFeedback(
                                "Account registration failed.",
                                true);
                        }
                    }
                }
            }
            catch (SqlException ex) when (
                ex.Number == 2601 || ex.Number == 2627)
            {
                // DUPLICATE EMAIL:
                // The email already exists in the User table.
                ShowFeedback(
                    "This email is already registered.",
                    true);
            }
            catch (Exception)
            {
                // DATABASE ERROR:
                // Display an error if the database connection fails.
                ShowFeedback(
                    "Unable to register. Please check your database connection.",
                    true);
            }
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
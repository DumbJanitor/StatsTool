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

namespace StatsTool.Views
{
    public partial class StartView : UserControl
    {
        public StartView()
        {
            InitializeComponent();
        }

        private void StartButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Find the application's MainWindow.
            if (Window.GetWindow(this) is StatsTool.MainWindow mainWindow)
            {
                // Navigate to Login.
                mainWindow.ShowLogin();
            }
        }
    }
}
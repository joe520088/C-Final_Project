using System.Windows;

namespace CreativeWrites.Views
{
    public partial class NewUserDialog : Window
    {
        public string Username { get; private set; } = string.Empty;
        public string Bio      { get; private set; } = string.Empty;

        public NewUserDialog() => InitializeComponent();

        private void OnCreate(object sender, RoutedEventArgs e)
        {
            string name = UsernameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Username cannot be empty.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Username    = name;
            Bio         = BioBox.Text.Trim();
            DialogResult = true;
        }
    }
}

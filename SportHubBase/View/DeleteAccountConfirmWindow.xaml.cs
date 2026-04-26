using System;
using System.Windows;
using System.Windows.Controls;

namespace SportHubBase.View
{
    public partial class DeleteAccountConfirmWindow : Window
    {
        private const string RequiredPhrase = "Я точно хочу удалить свой аккаунт";

        public DeleteAccountConfirmWindow()
        {
            InitializeComponent();
        }

        private void ConfirmationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ConfirmButton != null)
            {
                ConfirmButton.IsEnabled = string.Equals(ConfirmationTextBox.Text.Trim(), RequiredPhrase, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

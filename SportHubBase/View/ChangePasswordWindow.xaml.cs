using System;
using System.Windows;

namespace SportHubBase.View
{
    public partial class ChangePasswordWindow : Window
    {
        public string OldPassword => OldPasswordBox.Password;
        public string NewPassword => NewPasswordBox.Password;

        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OldPasswordBox.Password))
            {
                MessageBox.Show("Введите текущий пароль.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password) || NewPasswordBox.Password.Length < 4)
            {
                MessageBox.Show("Новый пароль должен быть не короче 4 символов.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают. Повторите ввод.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}

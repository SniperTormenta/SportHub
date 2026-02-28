using System;
using System.Windows;
using SportHubBase.ViewModels;

namespace SportHubBase.View
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        private void AuthAction_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AuthViewModel viewModel)
            {
                // Передаем пароль из PasswordBox в команду
                if (viewModel.IsLoginMode)
                {
                    if (viewModel.LoginCommand.CanExecute(PasswordInput.Password))
                    {
                        viewModel.LoginCommand.Execute(PasswordInput.Password);
                    }
                }
                else
                {
                    if (viewModel.RegisterCommand.CanExecute(PasswordInput.Password))
                    {
                        viewModel.RegisterCommand.Execute(PasswordInput.Password);
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        // Позволяет перетаскивать окно без рамок
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}

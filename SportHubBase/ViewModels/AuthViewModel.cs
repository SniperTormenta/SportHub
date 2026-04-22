using System;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    public class AuthViewModel : BaseViewModel
    {
        private readonly IAccountService _accountService;
        private string _username;
        private AuthMode _currentMode = AuthMode.Login;
        private string _errorMessage;

        public event Action LoginSuccess;

        public AuthViewModel(IAccountService accountService)
        {
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteAuth);
            RegisterCommand = new RelayCommand(ExecuteRegister, CanExecuteAuth);
            ToggleModeCommand = new RelayCommand(_ => CurrentMode = IsLoginMode ? AuthMode.Register : AuthMode.Login);
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public AuthMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLoginMode));
                    OnPropertyChanged(nameof(IsRegisterMode));
                    OnPropertyChanged(nameof(ViewTitle));
                    OnPropertyChanged(nameof(ActionName));
                    ErrorMessage = string.Empty;
                }
            }
        }

        public bool IsLoginMode => CurrentMode == AuthMode.Login;
        public bool IsRegisterMode => CurrentMode == AuthMode.Register;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ViewTitle => IsRegisterMode ? "Регистрация" : "Авторизация";
        public string ActionName => IsRegisterMode ? "Зарегистрироваться" : "Войти";

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ToggleModeCommand { get; }

        private bool CanExecuteAuth(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Username);
        }

        private void ExecuteLogin(object parameter)
        {
            string password = parameter?.ToString();
            UserAccount account;
            if (_accountService.LoginAndGetAccount(Username, password, out account, out string error))
            {
                CurrentSession.CurrentUser = account;
                LoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = error;
                System.Windows.MessageBox.Show(error, "Ошибка авторизации", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExecuteRegister(object parameter)
        {
            string password = parameter?.ToString();
            if (_accountService.Register(Username, password, out string error))
            {
                ErrorMessage = "Регистрация успешна! Теперь вы можете войти.";
                System.Windows.MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успешная регистрация", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                CurrentMode = AuthMode.Login;
            }
            else
            {
                ErrorMessage = error;
                System.Windows.MessageBox.Show(error, "Ошибка регистрации", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

using System;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using System.Text.RegularExpressions;

namespace SportHubBase.ViewModels
{
    public class AuthViewModel : BaseViewModel
    {
        private readonly IAccountService _accountService;
        private string _username;
        private string _email;
        private string _firstName;
        private string _phoneNumber;
        private string _city;
        private string _confirmPassword;
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

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (_confirmPassword != value)
                {
                    _confirmPassword = value;
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
            
            if (!ValidateRegistration(password, out string validationError))
            {
                ErrorMessage = validationError;
                return;
            }

            if (_accountService.Register(Username, password, Email, FirstName, PhoneNumber, City, out string error))
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

        private bool ValidateRegistration(string password, out string error)
        {
            error = string.Empty;

            // 1. Логин
            if (string.IsNullOrWhiteSpace(Username) || Username.Length < 4 || Username.Length > 32)
            {
                error = "Логин должен быть от 4 до 32 символов.";
                return false;
            }
            if (!Regex.IsMatch(Username, @"^[a-zA-Z0-9._-]+$"))
            {
                error = "Логин может содержать только латиницу, цифры и символы . _ -";
                return false;
            }

            // 2. Email
            if (string.IsNullOrWhiteSpace(Email) || Email.Length > 254)
            {
                error = "Email обязателен и не может превышать 254 символа.";
                return false;
            }
            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                error = "Введите корректный Email.";
                return false;
            }

            // 3. Имя
            if (string.IsNullOrWhiteSpace(FirstName) || FirstName.Length < 2 || FirstName.Length > 100)
            {
                error = "Имя должно быть от 2 до 100 символов.";
                return false;
            }

            // 4. Пароль
            if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 128)
            {
                error = "Пароль должен быть от 8 до 128 символов.";
                return false;
            }
            if (!Regex.IsMatch(password, @"[A-Z]") || !Regex.IsMatch(password, @"[a-z]") || !Regex.IsMatch(password, @"[0-9]"))
            {
                error = "Пароль должен содержать хотя бы одну заглавную букву, одну строчную и одну цифру.";
                return false;
            }
            if (password != ConfirmPassword)
            {
                error = "Пароли не совпадают.";
                return false;
            }

            // 5. Город (необязательно)
            if (!string.IsNullOrWhiteSpace(City))
            {
                if (City.Length < 2 || City.Length > 80)
                {
                    error = "Город должен быть от 2 до 80 символов.";
                    return false;
                }
            }

            // 6. Телефон (необязательно)
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                if (!Regex.IsMatch(PhoneNumber, @"^\+?[0-9\s\-\(\)]+$"))
                {
                    error = "Некорректный формат номера телефона.";
                    return false;
                }
            }

            return true;
        }
    }
}

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

        // Списки ограничений безопасности
        private static readonly string[] BannedDomains = { ".su", ".ua", "mailinator.com", "guerrillamail.com", "yopmail.com", "tempmail.com", "10minutemail.com" };
        private static readonly string[] BannedPrefixes = { "admin@", "support@", "info@", "root@", "noreply@", "security@", "webmaster@" };

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

            // Проверка обязательных полей для входа
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Пожалуйста, введите логин и пароль.";
                System.Windows.MessageBox.Show(ErrorMessage, "Ошибка ввода", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Убираем случайные пробелы по краям
            string cleanUsername = Username.Trim();

            UserAccount account;
            if (_accountService.LoginAndGetAccount(cleanUsername, password, out account, out string error))
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

            // Валидация
            if (!ValidateRegistration(password, out string validationError))
            {
                ErrorMessage = validationError;
                System.Windows.MessageBox.Show(validationError, "Ошибка заполнения данных", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Очистка данных от случайных начальных/конечных пробелов перед отправкой в БД
            string cleanUsername = Username.Trim();
            string cleanEmail = Email.Trim();
            string cleanFirstName = FirstName.Trim();
            string cleanCity = string.IsNullOrWhiteSpace(City) ? null : City.Trim();
            string cleanPhone = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();

            if (_accountService.Register(cleanUsername, password, cleanEmail, cleanFirstName, cleanPhone, cleanCity, out string error))
            {
                ErrorMessage = "Регистрация прошла успешно";
                System.Windows.MessageBox.Show(ErrorMessage, "Успешная регистрация", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

            // 1. Глобальная проверка на пустоту обязательных полей
            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(ConfirmPassword))
            {
                error = "Пожалуйста, заполните все обязательные поля (отмечены звездочкой *).";
                return false;
            }

            // 2. Логин
            if (Username.Length < 4 || Username.Length > 32)
            {
                error = "Логин должен быть от 4 до 32 символов.";
                return false;
            }
            if (!Regex.IsMatch(Username, @"^[a-zA-Z0-9._-]+$"))
            {
                error = "Логин может содержать только латиницу, цифры и символы . _ -";
                return false;
            }

            // 3. Email
            if (Email.Length > 254)
            {
                error = "Email не может превышать 254 символа.";
                return false;
            }
            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                error = "Введите корректный Email.";
                return false;
            }

            string emailLower = Email.ToLower();

            // 3.1. Проверка Email на системные префиксы
            foreach (var prefix in BannedPrefixes)
            {
                if (emailLower.StartsWith(prefix))
                {
                    error = "Регистрация на корпоративные и системные адреса запрещена.";
                    return false;
                }
            }

            // 3.2. Проверка Email на запрещенные домены и зоны
            string domain = emailLower.Substring(emailLower.IndexOf('@') + 1);
            foreach (var banned in BannedDomains)
            {
                if (banned.StartsWith("."))
                {
                    if (domain.EndsWith(banned))
                    {
                        error = $"Регистрация в доменной зоне '{banned}' запрещена политикой сервиса.";
                        return false;
                    }
                }
                else
                {
                    if (domain == banned || domain.EndsWith("." + banned))
                    {
                        error = "Использование одноразовых почтовых сервисов запрещено.";
                        return false;
                    }
                }
            }

            // 4. Имя
            if (FirstName.Length < 2 || FirstName.Length > 100)
            {
                error = "Имя должно быть от 2 до 100 символов.";
                return false;
            }
            if (!Regex.IsMatch(FirstName, @"^[а-яА-Яa-zA-Z\s\-]+$"))
            {
                error = "Имя может содержать только буквы, пробелы и дефис.";
                return false;
            }

            // 5. Пароль
            if (password.Length < 8 || password.Length > 128)
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

            // 6. Город (необязательно)
            if (!string.IsNullOrWhiteSpace(City))
            {
                if (City.Length < 2 || City.Length > 80)
                {
                    error = "Город должен быть от 2 до 80 символов.";
                    return false;
                }
            }

            // 7. Телефон (необязательно)
            if (!string.IsNullOrWhiteSpace(PhoneNumber))
            {
                // Очищаем строку от пробелов, скобок и тире для проверки
                string cleanPhone = PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

                // Проверяем, что номер начинается с +7 или 8 и содержит ровно 11 цифр (формат РФ)
                if (!Regex.IsMatch(cleanPhone, @"^(?:\+7|8)\d{10}$"))
                {
                    error = "Сервис работает только с номерами РФ. Введите корректный номер в формате +7 (XXX) XXX-XX-XX.";
                    return false;
                }
            }

            return true;
        }

        public bool UseSqlServer
        {
            get => DatabaseConfig.UseSqlServer;
            set
            {
                DatabaseConfig.UseSqlServer = value;
                OnPropertyChanged();
            }
        }

        public string SqlConnectionString
        {
            get => DatabaseConfig.SqlConnectionString;
            set
            {
                DatabaseConfig.SqlConnectionString = value;
                OnPropertyChanged();
            }
        }
    }
}
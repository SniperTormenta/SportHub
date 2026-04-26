using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    public class AccountViewModel : BaseViewModel
    {
        private readonly IAccountService _accountService;
        private UserAccount _user;

        // 1. Создаем локальные переменные для нашего "черновика"
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _phoneNumber;

        public AccountViewModel(IAccountService accountService)
        {
            _accountService = accountService;

            // Берем текущего пользователя из сессии
            User = CurrentSession.CurrentUser;

            // 2. При открытии окна заполняем черновик реальными данными из БД/сессии
            FirstName = User?.FirstName ?? "";
            LastName = User?.LastName ?? "";
            Email = User?.Email ?? "";
            PhoneNumber = User?.PhoneNumber ?? "";

            ChangeAvatarCommand = new RelayCommand(ChangeAvatar);
            SaveProfileCommand = new RelayCommand(SaveProfile);
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            DeleteAccountCommand = new RelayCommand(DeleteAccount);
        }

        public RelayCommand ChangeAvatarCommand { get; }
        public RelayCommand SaveProfileCommand { get; }
        public RelayCommand ChangePasswordCommand { get; }
        public RelayCommand DeleteAccountCommand { get; }

        public UserAccount User
        {
            get => _user;
            set
            {
                if (_user != value)
                {
                    _user = value;
                    OnPropertyChanged(nameof(User));
                    OnPropertyChanged(nameof(Username));
                    OnPropertyChanged(nameof(AvatarPath));
                    OnPropertyChanged(nameof(RoleDisplay));
                }
            }
        }

        public string Username => User?.Username ?? "Гость";
        public string AvatarPath => string.IsNullOrEmpty(User?.AvatarPath) ? "/Resources/avatar.png" : User.AvatarPath;

        // 3. Теперь свойства привязаны к локальным переменным, а не напрямую к User
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (_firstName != value)
                {
                    _firstName = value;
                    OnPropertyChanged(nameof(FirstName));
                }
            }
        }

        public string LastName
        {
            get => _lastName;
            set
            {
                if (_lastName != value)
                {
                    _lastName = value;
                    OnPropertyChanged(nameof(LastName));
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
                    OnPropertyChanged(nameof(Email));
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
                    OnPropertyChanged(nameof(PhoneNumber));
                }
            }
        }

        public string RoleDisplay
        {
            get
            {
                if (User == null) return "Гость";
                switch (User.Role?.ToLower())
                {
                    case "admin": return "Администратор";
                    case "user": return "Пользователь";
                    default: return User.Role ?? "Пользователь";
                }
            }
        }

        private void ChangeAvatar(object parameter)
        {
            if (User == null) return;

            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Изображения (*.jpg; *.jpeg; *.png)|*.jpg;*.jpeg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string sourceFile = openFileDialog.FileName;
                    string extension = Path.GetExtension(sourceFile);
                    string fileName = string.Format("avatar_{0}_{1}{2}", User.Id, DateTime.Now.Ticks, extension);

                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    string avatarsDir = Path.Combine(baseDir, "Resources", "Avatars");

                    if (!Directory.Exists(avatarsDir))
                    {
                        Directory.CreateDirectory(avatarsDir);
                    }

                    string targetPath = Path.Combine(avatarsDir, fileName);
                    File.Copy(sourceFile, targetPath, true);

                    if (_accountService.UpdateAccountDetail(User.Id, "AvatarPath", targetPath))
                    {
                        User.AvatarPath = targetPath;
                        CurrentSession.CurrentUser.AvatarPath = targetPath;
                        OnPropertyChanged(nameof(AvatarPath));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке аватара: " + ex.Message);
                }
            }
        }

        private void SaveProfile(object parameter)
        {
            if (User == null) return;

            try
            {
                // 4. Отправляем в базу данных значения из нашего черновика
                _accountService.UpdateAccountDetail(User.Id, "FirstName", FirstName);
                _accountService.UpdateAccountDetail(User.Id, "LastName", LastName);
                _accountService.UpdateAccountDetail(User.Id, "Email", Email);
                _accountService.UpdateAccountDetail(User.Id, "PhoneNumber", PhoneNumber);

                // 5. И только после успешного сохранения обновляем глобальную сессию
                CurrentSession.CurrentUser.FirstName = FirstName;
                CurrentSession.CurrentUser.LastName = LastName;
                CurrentSession.CurrentUser.Email = Email;
                CurrentSession.CurrentUser.PhoneNumber = PhoneNumber;

                // На всякий случай обновляем локальный объект User
                User.FirstName = FirstName;
                User.LastName = LastName;
                User.Email = Email;
                User.PhoneNumber = PhoneNumber;

                MessageBox.Show("Профиль успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении профиля: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePassword(object parameter)
        {
            if (User == null) return;

            var window = new SportHubBase.View.ChangePasswordWindow();
            if (window.ShowDialog() == true)
            {
                if (_accountService.ChangePassword(User.Id, window.OldPassword, window.NewPassword, out string errorMessage))
                {
                    MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteAccount(object parameter)
        {
            if (User == null) return;

            // System dialog confirmation first
            var result = MessageBox.Show(
                "Вы уверены, что хотите начать процедуру удаления аккаунта?\n\nВаши данные будут безвозвратно утеряны.",
                "Подтверждение", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Second phase string match confirmation
                var confirmWindow = new SportHubBase.View.DeleteAccountConfirmWindow();
                if (confirmWindow.ShowDialog() == true)
                {
                    if (_accountService.DeleteAccount(User.Id, out string errorMessage))
                    {
                        MessageBox.Show("Ваш аккаунт был успешно удален. Приложение будет перезапущено.", "Аккаунт удален", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        CurrentSession.Clear();
                        
                        // Restart the application
                        System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        Application.Current.Shutdown();
                    }
                    else
                    {
                        MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
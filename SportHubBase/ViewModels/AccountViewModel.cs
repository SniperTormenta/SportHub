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

        public AccountViewModel(IAccountService accountService)
        {
            _accountService = accountService;
            User = CurrentSession.CurrentUser;
            ChangeAvatarCommand = new RelayCommand(ChangeAvatar);
        }

        public RelayCommand ChangeAvatarCommand { get; }

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
                    OnPropertyChanged(nameof(FirstName));
                    OnPropertyChanged(nameof(LastName));
                    OnPropertyChanged(nameof(Email));
                    OnPropertyChanged(nameof(PhoneNumber));
                    OnPropertyChanged(nameof(AvatarPath));
                    OnPropertyChanged(nameof(RoleDisplay));
                }
            }
        }

        public string Username => User?.Username ?? "Гость";
        public string FirstName => User?.FirstName ?? "Не указано";
        public string LastName => User?.LastName ?? "Не указано";
        public string Email => User?.Email ?? "Не указано";
        public string PhoneNumber => User?.PhoneNumber ?? "Не указано";
        public string AvatarPath => string.IsNullOrEmpty(User?.AvatarPath) ? "/Resources/avatar.png" : User.AvatarPath;
        
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

                    // Сохраняем в БД (путь должен быть относительным или абсолютным, но лучше абсолютным для Uri или относительным для ресурсов)
                    // Для WPF лучше использовать полный путь для локальных файлов на диске
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
    }
}

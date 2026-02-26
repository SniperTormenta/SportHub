using SportHubBase.Models;
using System.Windows.Input;

namespace SportHubBase.ViewModels
{
    public class AuthViewModel : BaseViewModel
    {
        private AuthMode _currentMode = AuthMode.Login;
        
        public AuthMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    OnPropertyChanged(nameof(CurrentMode));
                    OnPropertyChanged(nameof(IsLoginMode));
                    OnPropertyChanged(nameof(IsRegisterMode));
                }
            }
        }

        public bool IsLoginMode => CurrentMode == AuthMode.Login;
        public bool IsRegisterMode => CurrentMode == AuthMode.Register;

        private bool _rememberMe;
        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                if (_rememberMe != value)
                {
                    _rememberMe = value;
                    OnPropertyChanged(nameof(RememberMe));
                }
            }
        }

        public ICommand SwitchToLoginCommand { get; }
        public ICommand SwitchToRegisterCommand { get; }

        public AuthViewModel()
        {
            SwitchToLoginCommand = new RelayCommand(_ => SwitchToLogin());
            SwitchToRegisterCommand = new RelayCommand(_ => SwitchToRegister());
        }

        private void SwitchToLogin()
        {
            CurrentMode = AuthMode.Login;
        }

        private void SwitchToRegister()
        {
            CurrentMode = AuthMode.Register;
        }
    }
}

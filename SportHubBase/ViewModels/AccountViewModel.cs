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
        }

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
                }
            }
        }

        public string Username => User?.Username ?? "Гость";
    }
}

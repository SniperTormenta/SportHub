using System;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.View
{
    public partial class OrganizerLoginWindow : Window
    {
        private readonly Tournament _tournament;
        private readonly IStorage _storage;

        public OrganizerLoginWindow(Tournament tournament, IStorage storage)
        {
            InitializeComponent();
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            TournamentTitleText.Text = string.Format("Турнир «{0}» (Сервер: {1})",
                _tournament.Name,
                string.IsNullOrWhiteSpace(_tournament.ServerAddress) ? "localhost" : _tournament.ServerAddress);

            if (!string.IsNullOrWhiteSpace(_tournament.OrganizerLogin))
            {
                LoginTextBox.Text = _tournament.OrganizerLogin;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
        }

        private void AttemptLogin()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            string login = LoginTextBox.Text != null ? LoginTextBox.Text.Trim() : string.Empty;
            string password = PasswordBox.Password;
            bool rememberMe = RememberMeCheckBox.IsChecked == true;

            if (_storage.UnlockTournament(_tournament, login, password, rememberMe, out string errorMessage))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                ErrorTextBlock.Text = errorMessage;
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

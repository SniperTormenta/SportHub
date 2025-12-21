using System.Windows;
using SportHubBase.Models;

namespace SportHubBase.View
{
    public partial class MatchDetailsWindow : Window
    {
        private readonly Match _match;

        public MatchDetailsWindow(Window owner, Match match)
        {
            InitializeComponent();
            Owner = owner;
            _match = match;
            DataContext = _match;
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_match.Status))
            {
                _match.Status = "Сыгран";
            }

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


// View/MatchDetailsWindow.xaml.cs
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System;
using System.Windows;

namespace SportHubBase.View
{
    public partial class MatchDetailsWindow : Window
    {
        private readonly MatchDetailsViewModel _viewModel;

        public MatchDetailsWindow(Window owner, Match match, Guid tournamentId)
        {
            InitializeComponent();
            Owner = owner;

            _viewModel = new MatchDetailsViewModel(match, tournamentId);
            DataContext = _viewModel;

            _viewModel.RequestClose += () =>
            {
                DialogResult = true; // или false при Cancel, но у нас одинаково закрываем
                Close();
            };
        }
    }
}
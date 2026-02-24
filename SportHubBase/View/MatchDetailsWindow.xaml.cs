// View/MatchDetailsWindow.xaml.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System;
using System.Windows;

namespace SportHubBase.View
{
    public partial class MatchDetailsWindow : Window
    {
        private readonly MatchDetailsViewModel _viewModel;

        public MatchDetailsWindow(Window owner, Match match, Guid tournamentId, IMatchService matchService)
        {
            InitializeComponent();
            Owner = owner;

            var storage = App.Container.GetInstance<IStorage>();
            var factory = App.Container.GetInstance<ISportScoreStrategyFactory>();
            
            var tournaments = storage.LoadTournaments();
            var tournament = tournaments.Find(t => t.Id == tournamentId);
            var strategy = factory.GetStrategy(tournament?.SportType);

            _viewModel = new MatchDetailsViewModel(match, tournamentId, storage, matchService, strategy);
            DataContext = _viewModel;

            _viewModel.RequestClose += () =>
            {
                DialogResult = true; // или false при Cancel, но у нас одинаково закрываем
                Close();
            };
        }
    }
}
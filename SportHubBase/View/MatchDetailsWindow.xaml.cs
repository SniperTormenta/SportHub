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

        public MatchDetailsWindow(Window owner, Match match, Guid tournamentId, IMatchService matchService, bool canEdit)
        {
            InitializeComponent();
            Owner = owner;

            var storage = App.Container.GetInstance<IStorage>();
            var factory = App.Container.GetInstance<ISportScoreStrategyFactory>();
            var encoderFactory = App.Container.GetInstance<IImageEncoderStrategyFactory>();
            
            var tournament = storage.GetTournamentById(tournamentId);
            var strategy = factory.GetStrategy(tournament?.SportType);

            _viewModel = new MatchDetailsViewModel(match, tournamentId, storage, matchService, strategy, canEdit);
            DataContext = _viewModel;

            _viewModel.RequestOpenCardPreview += (m, tId) =>
            {
                var currentTournament = storage.GetTournamentById(tId) ?? tournament;
                if (currentTournament != null)
                {
                    var cardVm = new MatchCardPreviewViewModel(currentTournament, m, storage, encoderFactory);
                    var previewWin = new MatchCardPreviewWindow(this, cardVm);
                    previewWin.ShowDialog();
                }
            };

            _viewModel.RequestClose += () =>
            {
                DialogResult = true; // или false при Cancel, но у нас одинаково закрываем
                Close();
            };
        }
    }
}
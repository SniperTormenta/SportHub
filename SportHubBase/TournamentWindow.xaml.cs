using System;
using System.Windows;
using SportHubBase.ViewModels;

namespace SportHubBase
{
    public partial class TournamentWindow : Window
    {
        public TournamentWindow(Guid tournamentId)
        {
            InitializeComponent();
            DataContext = new TournamentViewModel(tournamentId);
        }

        // Для дизайнера и тестов
        public TournamentWindow() : this(Guid.Empty) { }
    }
}
// View/MatchDetailsWindow.xaml.cs
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;
using System;
using System.Windows;

using SportHubBase.Interfaces;

namespace SportHubBase.View
{
    public partial class MatchDetailsWindow : Window
    {
        private readonly MatchDetailsViewModel _viewModel;

        public MatchDetailsWindow(Window owner, Match match, Guid tournamentId, IMatchService matchService)
        {
            InitializeComponent();
            Owner = owner;

            // Создание ViewModel через контейнер зависимостей (Storage) и переданные сервисы
            var storage = App.Container.GetInstance<IStorage>();
            _viewModel = new MatchDetailsViewModel(match, tournamentId, storage, matchService);
            DataContext = _viewModel;

            _viewModel.RequestClose += () =>
            {
                DialogResult = true; // или false при Cancel, но у нас одинаково закрываем
                Close();
            };
        }
    }
}
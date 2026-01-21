using System;
using System.Windows;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;

namespace SportHubBase.View // или SportHubBase, если в корне
{
    public partial class AddTeamWindow : Window
    {
        private readonly AddTeamViewModel _viewModel;

        public AddTeamWindow(Window owner, Guid tournamentId, Team team = null)
        {
            InitializeComponent();
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Создание ViewModel через контейнер зависимостей
            var storage = App.Container.GetInstance<IStorage>();
            _viewModel = new AddTeamViewModel(tournamentId, storage, team);
            DataContext = _viewModel;

            _viewModel.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
        }

        public AddTeamWindow() : this(null, Guid.Empty, null) { }

        // Оставляем только крестик
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
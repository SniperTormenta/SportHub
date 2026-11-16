using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.View
{
    public partial class ConnectRemoteTournamentWindow : Window
    {
        private readonly IStorage _storage;
        private string _loadedServerAddress;

        private void ServerAddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _loadedServerAddress = null;
            if (RemoteTournamentsGrid == null) return;
            RemoteTournamentsGrid.ItemsSource = null;
            if (ConnectButton != null) ConnectButton.IsEnabled = false;
            StatusTextBlock.Text = "Введите адрес сервера и нажмите «Найти турниры».";
            StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        }

        private void RemoteTournamentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConnectButton != null)
                ConnectButton.IsEnabled = _loadedServerAddress != null && RemoteTournamentsGrid.SelectedItem is Tournament;
        }

        public ConnectRemoteTournamentWindow()
        {
            InitializeComponent();
            _storage = App.Container.GetInstance<IStorage>();

            if (!string.IsNullOrWhiteSpace(DatabaseConfig.SqlServerAddress))
            {
                ServerAddressTextBox.Text = DatabaseConfig.SqlServerAddress;
            }
        }

        private void SearchServerButton_Click(object sender, RoutedEventArgs e)
        {
            string serverAddress = ServerAddressTextBox.Text != null ? ServerAddressTextBox.Text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                StatusTextBlock.Text = "Укажите адрес сервера для поиска турниров.";
                ServerAddressTextBox.Focus();
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _loadedServerAddress = null;
                ConnectButton.IsEnabled = false;
                RemoteTournamentsGrid.ItemsSource = null;
                StatusTextBlock.Text = "Поиск турниров на сервере…";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));

                DatabaseConfig.SqlServerAddress = serverAddress;
                List<Tournament> remoteList = _storage.GetRemoteTournamentsList(serverAddress);

                _loadedServerAddress = serverAddress;
                RemoteTournamentsGrid.ItemsSource = remoteList;

                if (remoteList.Count == 0)
                {
                    StatusTextBlock.Text = "Подключение успешно, но на сервере пока нет опубликованных турниров.";
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    StatusTextBlock.Text = string.Format("Найдено турниров: {0}. Выберите турнир для добавления.", remoteList.Count);
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                    RemoteTournamentsGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                SportHubBase.Services.ErrorLog.Write("Ошибка операции", ex);
                RemoteTournamentsGrid.ItemsSource = null;
                StatusTextBlock.Text = "Не удалось подключиться к серверу: " + SportHubBase.Services.ErrorLog.UserMessage(ex);
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectSelectedTournament();
        }

        private void RemoteTournamentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            var row = source == null ? null : ItemsControl.ContainerFromElement(RemoteTournamentsGrid, source) as DataGridRow;
            if (row != null && row.Item is Tournament)
            {
                RemoteTournamentsGrid.SelectedItem = row.Item;
                ConnectSelectedTournament();
            }
        }

        private void ConnectSelectedTournament()
        {
            var selected = RemoteTournamentsGrid.SelectedItem as Tournament;
            if (selected == null || _loadedServerAddress == null)
            {
                MessageBox.Show("Выберите турнир из списка для подключения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string serverAddress = _loadedServerAddress;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _storage.ConnectRemoteTournament(selected.Id, serverAddress);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                SportHubBase.Services.ErrorLog.Write("Ошибка операции", ex);
                MessageBox.Show(
                    "Ошибка при подключении турнира:\n" + SportHubBase.Services.ErrorLog.UserMessage(ex),
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

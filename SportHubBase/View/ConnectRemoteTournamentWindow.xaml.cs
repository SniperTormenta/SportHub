using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services;

namespace SportHubBase.View
{
    public partial class ConnectRemoteTournamentWindow : Window
    {
        private readonly IStorage _storage;

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
                MessageBox.Show("Пожалуйста, введите адрес SQL Server.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                StatusTextBlock.Text = "Подключение к серверу " + serverAddress + "...";
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139));

                DatabaseConfig.SqlServerAddress = serverAddress;
                List<Tournament> remoteList = _storage.GetRemoteTournamentsList(serverAddress);

                RemoteTournamentsGrid.ItemsSource = remoteList;

                if (remoteList.Count == 0)
                {
                    StatusTextBlock.Text = "Подключение успешно, но на сервере пока нет опубликованных турниров.";
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    StatusTextBlock.Text = string.Format("Найдено турниров на сервере ({0}): {1}. Выберите турнир для подключения.", serverAddress, remoteList.Count);
                    StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(22, 101, 52));
                    RemoteTournamentsGrid.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                RemoteTournamentsGrid.ItemsSource = null;
                StatusTextBlock.Text = "Не удалось подключиться к серверу: " + ex.Message;
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                MessageBox.Show(
                    "Не удалось подключиться к серверу MS SQL Server (" + serverAddress + ").\n\n" + ex.Message,
                    "Ошибка подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
            if (RemoteTournamentsGrid.SelectedItem != null)
            {
                ConnectSelectedTournament();
            }
        }

        private void ConnectSelectedTournament()
        {
            var selected = RemoteTournamentsGrid.SelectedItem as Tournament;
            if (selected == null)
            {
                MessageBox.Show("Выберите турнир из списка для подключения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string serverAddress = ServerAddressTextBox.Text != null ? ServerAddressTextBox.Text.Trim() : "localhost";

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _storage.ConnectRemoteTournament(selected.Id, serverAddress);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при подключении турнира:\n" + ex.Message,
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

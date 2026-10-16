using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.Services;
using SportHubBase.ViewModels;

namespace SportHubBase
{
    public partial class CreateTournamentWindow : Window
    {
        public CreateTournamentWindow()
        {
            InitializeComponent();

            var storage = App.Container.GetInstance<IStorage>();
            DataContext = new CreateTournamentViewModel(storage);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTournamentTypeDescription("Круговой");
            WindowState = WindowState.Normal;

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            Width = screenWidth * 0.95;
            Height = screenHeight * 0.95;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;

            StartDatePicker.SelectedDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(DatabaseConfig.SqlServerAddress))
            {
                ServerAddressTextBox.Text = DatabaseConfig.SqlServerAddress;
            }

            SetupCityComboBox();
        }

        private void IsRemoteCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (RemoteSettingsPanel != null)
            {
                RemoteSettingsPanel.Visibility = IsRemoteCheckBox.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                UpdateTournamentTypeDescription(radioButton.Content?.ToString() ?? "");
            }
        }

        private void UpdateTournamentTypeDescription(string selectedType)
        {
            string description;

            if (selectedType == "Круговой")
            {
                description = "Круговой турнир - каждый участник играет с каждым и получает очки. Места распределяются по количеству очков и другим показателям";
            }
            else if (selectedType == "Олимпийский")
            {
                description = "Олимпийская система — проиграл и вылетел. Плей-офф с выбыванием.";
            }
            else if (selectedType == "Швейцарский")
            {
                description = "Швейцарская система — участники с равным количеством очков играют между собой.";
            }
            else if (selectedType == "Многоэтапный")
            {
                description = "Турнир состоит из нескольких этапов (группы + плей-офф и т.д.).";
            }
            else
            {
                description = "";
            }

            if (TournamentTypeDescriptionPlaceholder == null)
                return;

            if (TotalRoundsStackPanel != null)
            {
                TotalRoundsStackPanel.Visibility = selectedType == "Швейцарский" ? Visibility.Visible : Visibility.Collapsed;
            }

            var newBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0),
                Width = double.NaN,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var textBlock = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                MaxWidth = 600
            };

            newBorder.Child = textBlock;
            TournamentTypeDescriptionPlaceholder.Content = newBorder;
        }

        private void CreateTournament_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Поле 'Название' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (StatusComboBox.SelectedItem == null)
            {
                MessageBox.Show("Поле 'Статус' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SportTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Поле 'Вид спорта' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedType = GetSelectedTournamentType();
            if (string.IsNullOrEmpty(selectedType))
            {
                MessageBox.Show("Поле 'Тип' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Поле 'Дата начала' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int totalRounds = 0;
            if (selectedType == "Швейцарский")
            {
                if (!int.TryParse(TotalRoundsTextBox.Text, out totalRounds) || totalRounds < 1)
                {
                    MessageBox.Show("Пожалуйста, введите корректное количество туров (число больше 0).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            bool isRemote = IsRemoteCheckBox.IsChecked == true;
            string serverAddress = null;
            string organizerLogin = null;
            string organizerPasswordHash = null;

            if (isRemote)
            {
                serverAddress = ServerAddressTextBox.Text?.Trim();
                organizerLogin = OrganizerLoginTextBox.Text?.Trim();
                string organizerPassword = OrganizerPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(serverAddress))
                {
                    MessageBox.Show("Укажите адрес SQL Server для публикации удалённого турнира.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(organizerLogin) || string.IsNullOrWhiteSpace(organizerPassword))
                {
                    MessageBox.Show("Для удалённого турнира необходимо задать логин и пароль организатора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                organizerPasswordHash = PasswordHasher.HashPassword(organizerPassword);
                DatabaseConfig.SqlServerAddress = serverAddress;
            }

            string sportType = ((ComboBoxItem)SportTypeComboBox.SelectedItem).Content.ToString();
            string defaultScoring = "Итальянская";
            if (sportType == "Футбол") defaultScoring = "Футбол";
            else if (sportType == "Баскетбол") defaultScoring = "Баскетбол";

            var tournament = new Tournament
            {
                Name = NameTextBox.Text.Trim(),
                LogoUrl = "",
                Status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString(),
                SportType = sportType,
                ScoringSystem = defaultScoring,
                Type = selectedType,
                TotalRounds = totalRounds,
                EditMode = "Только организатор турнира",
                AccessMode = "Доступно всем",
                IsPublic = true,
                NoScore = false,
                Description = DescriptionTextBox.Text?.Trim(),
                City = CityComboBox.Text?.Trim(),
                Contacts = ContactsTextBox.Text?.Trim(),
                StartDate = StartDatePicker.SelectedDate.Value,
                IsRemote = isRemote,
                ServerAddress = serverAddress,
                OrganizerLogin = organizerLogin,
                OrganizerPasswordHash = organizerPasswordHash,
                IsOrganizerRemembered = isRemote
            };

            try
            {
                var storage = App.Container.GetInstance<IStorage>();
                storage.CreateTournament(tournament);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось создать турнир:\n" + ex.Message,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetSelectedTournamentType()
        {
            if (TournamentTypeRadioPanel == null) return "";

            foreach (RadioButton rb in TournamentTypeRadioPanel.Children)
            {
                if (rb.IsChecked == true)
                {
                    return rb.Content.ToString();
                }
            }
            return "";
        }

        private void SetupCityComboBox()
        {
            var textBox = CityComboBox.Template.FindName("PART_EditableTextBox", CityComboBox) as TextBox;
            if (textBox != null)
            {
                textBox.TextChanged += CityComboBox_TextChanged;
            }
        }

        private void CityComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is CreateTournamentViewModel viewModel)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    viewModel.FilterCitiesByText(textBox.Text);
                }
            }
        }
    }
}
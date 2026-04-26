using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SportHubBase.Interfaces;
using SportHubBase.Models;
using SportHubBase.ViewModels;

namespace SportHubBase
{
    public partial class CreateTournamentWindow : Window
    {
        public CreateTournamentWindow()
        {
            InitializeComponent();


            // Создание ViewModel через контейнер зависимостей
            var storage = App.Container.GetInstance<IStorage>();
            DataContext = new CreateTournamentViewModel(storage);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAdministratorsBlock();
            UpdateTournamentTypeDescription("Круговой");
            WindowState = WindowState.Normal; // на всякий случай

            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;

            Width = screenWidth * 0.95;
            Height = screenHeight * 0.95;

            Left = (screenWidth - Width) / 2;
            Top = (screenHeight - Height) / 2;

            // Подписываемся на изменение текста в ComboBox
            SetupCityComboBox();
        }

        private void LoadAdministratorsBlock()
        {
            var block = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };

            var label = new TextBlock
            {
                Text = "Администраторы *",
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            block.Children.Add(label);

            var adminsText = new TextBlock
            {
                Text = string.Format("Владелец - {0}", CurrentSession.Username),
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 4, 0, 0)
            };
            block.Children.Add(adminsText);

            AdministratorsPlaceholder.Content = block;
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

            // Проверяем, что TournamentTypeDescriptionPlaceholder инициализирован
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
                // Фиксируем ширину Border, чтобы она не менялась
                Width = double.NaN, // Auto
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var textBlock = new TextBlock
            {
                Text = description,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(55, 65, 81)),
                // Ограничиваем ширину текста, чтобы он переносился
                MaxWidth = 600 // или другое значение
            };

            newBorder.Child = textBlock;
            TournamentTypeDescriptionPlaceholder.Content = newBorder;
        }

        private void CreateTournament_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
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

            if (EditModeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Поле 'Режим редактирования игр' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (AccessModeComboBox.SelectedItem == null)
            {
                MessageBox.Show("Поле 'Режим доступа' обязательно для заполнения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Если всё OK, собираем объект
            string contactsInfo = "";
            if (AddContactsCheckBox.IsChecked == true && CurrentSession.CurrentUser != null)
            {
                var contactsList = new List<string>();
                if (!string.IsNullOrWhiteSpace(CurrentSession.CurrentUser.Email)) contactsList.Add(CurrentSession.CurrentUser.Email);
                if (!string.IsNullOrWhiteSpace(CurrentSession.CurrentUser.PhoneNumber)) contactsList.Add(CurrentSession.CurrentUser.PhoneNumber);
                contactsInfo = string.Join(", ", contactsList);
            }

            var tournament = new Tournament
            {
                Name = NameTextBox.Text,
                LogoUrl = "", // Если есть поле для логотипа
                Status = ((ComboBoxItem)StatusComboBox.SelectedItem).Content.ToString(),
                SportType = ((ComboBoxItem)SportTypeComboBox.SelectedItem).Content.ToString(),
                Type = selectedType,
                TotalRounds = totalRounds,
                EditMode = ((ComboBoxItem)EditModeComboBox.SelectedItem).Content.ToString(),
                AccessMode = ((ComboBoxItem)AccessModeComboBox.SelectedItem).Content.ToString(),
                IsPublic = ((ComboBoxItem)AccessModeComboBox.SelectedItem).Content.ToString() != "Приватный",
                NoScore = NoScoreCheckBox.IsChecked ?? false,
                Description = DescriptionTextBox.Text,
                City = CityComboBox.Text,
                Contacts = contactsInfo,
                StartDate = StartDatePicker.SelectedDate.Value,
                // Устанавливаем владельца из текущей сессии
                OwnerId = CurrentSession.CurrentUser != null 
                    ? CurrentSession.CurrentUser.Id.ToString() 
                    : null
            };

            // Сохранение через сервис хранения
            var storage = App.Container.GetInstance<IStorage>();
            storage.CreateTournament(tournament);

            // Показать сообщение об успешном создании
            MessageBox.Show("Турнир успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close(); // Закрыть текущее окно
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
            // Находим TextBox внутри ComboBox для обработки изменения текста
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
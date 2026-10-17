using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SportHubBase.Converters;
using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.ViewModels
{
    /// <summary>
    /// ViewModel для автоматической генерации и предпросмотра графической карточки матча.
    /// </summary>
    public class MatchCardPreviewViewModel : BaseViewModel
    {
        private readonly Tournament _tournament;
        private readonly Match _match;
        private readonly IStorage _storage;
        private readonly IImageEncoderStrategyFactory _encoderFactory;

        public IImageEncoderStrategyFactory EncoderFactory => _encoderFactory;

        public string HeaderTag => "МОИ ТУРНИРЫ";

        public string TournamentName { get; private set; }
        public string StageAndCitySubtitle { get; private set; }
        public bool HasStageAndCitySubtitle => !string.IsNullOrWhiteSpace(StageAndCitySubtitle);

        public string Team1Name { get; private set; }
        public string Team2Name { get; private set; }

        public string Team1LogoUrl { get; private set; }
        public string Team2LogoUrl { get; private set; }

        public bool HasTeam1Logo => !string.IsNullOrWhiteSpace(Team1LogoUrl);
        public bool HasTeam2Logo => !string.IsNullOrWhiteSpace(Team2LogoUrl);

        public string Team1Initials { get; private set; }
        public string Team2Initials { get; private set; }

        public string Score1Text { get; private set; }
        public string Score2Text { get; private set; }

        public bool IsTeam1Winner { get; private set; }
        public bool IsTeam2Winner { get; private set; }

        public string SetsDetailText { get; private set; }
        public bool HasSetsDetail => !string.IsNullOrWhiteSpace(SetsDetailText);

        public string WinnerName { get; private set; }
        public bool HasWinner => !string.IsNullOrWhiteSpace(WinnerName);

        public string StartTimeText { get; private set; }
        public bool HasStartTime => !string.IsNullOrWhiteSpace(StartTimeText);

        public string DateText { get; private set; }
        public bool HasDate => !string.IsNullOrWhiteSpace(DateText);

        public string RefereeText { get; private set; }
        public bool HasReferee => !string.IsNullOrWhiteSpace(RefereeText);

        public string MvpText { get; private set; }
        public bool HasMvp => !string.IsNullOrWhiteSpace(MvpText);

        public bool HasDetailsColumn => HasDate || HasReferee || HasMvp;

        public bool ShowDividerAfterWinner => HasWinner && (HasStartTime || HasDetailsColumn);
        public bool ShowDividerAfterStartTime => HasStartTime && HasDetailsColumn;

        public bool HasAnyInfoBlock => HasWinner || HasStartTime || HasDetailsColumn;

        private string _cardBackgroundData;
        public string CardBackgroundData
        {
            get => _cardBackgroundData;
            set
            {
                if (_cardBackgroundData != value)
                {
                    _cardBackgroundData = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasCustomBackground));
                }
            }
        }

        public bool HasCustomBackground => !string.IsNullOrWhiteSpace(_cardBackgroundData);

        public ICommand SelectBackgroundCommand { get; }
        public ICommand ResetBackgroundCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action<object, bool> RequestClose;

        public MatchCardPreviewViewModel(
            Tournament tournament,
            Match match,
            IStorage storage,
            IImageEncoderStrategyFactory encoderFactory)
        {
            _tournament = tournament ?? throw new ArgumentNullException(nameof(tournament));
            _match = match ?? throw new ArgumentNullException(nameof(match));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _encoderFactory = encoderFactory ?? throw new ArgumentNullException(nameof(encoderFactory));

            SelectBackgroundCommand = new RelayCommand(_ => SelectBackground());
            ResetBackgroundCommand = new RelayCommand(_ => ResetBackground());
            CloseCommand = new RelayCommand(_ => Close(false));

            _cardBackgroundData = _tournament.CardBackgroundData;

            PopulateFromMatchData();
        }

        public void Close(bool result)
        {
            RequestClose?.Invoke(this, result);
        }

        private void PopulateFromMatchData()
        {
            TournamentName = !string.IsNullOrWhiteSpace(_tournament.Name)
                ? _tournament.Name.Trim()
                : "Спортивный турнир";

            // Этап / тур и город / площадка
            string locationPart = null;
            if (!string.IsNullOrWhiteSpace(_match.Location))
            {
                locationPart = _match.Location.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(_tournament.City))
            {
                string city = _tournament.City.Trim();
                locationPart = city.StartsWith("г.", StringComparison.OrdinalIgnoreCase) ? city : "г. " + city;
            }

            string stagePart = null;
            if (!string.IsNullOrWhiteSpace(_match.RoundName))
            {
                stagePart = _match.RoundName.Trim();
            }
            else if (_match.Round > 0)
            {
                stagePart = "Тур " + _match.Round;
            }
            else
            {
                stagePart = "Игры";
            }

            if (!string.IsNullOrWhiteSpace(locationPart) && !string.IsNullOrWhiteSpace(stagePart))
            {
                StageAndCitySubtitle = locationPart + " / " + stagePart;
            }
            else if (!string.IsNullOrWhiteSpace(locationPart))
            {
                StageAndCitySubtitle = locationPart;
            }
            else
            {
                StageAndCitySubtitle = stagePart;
            }

            // Команды и логотипы
            Team1Name = !string.IsNullOrWhiteSpace(_match.Team1) ? _match.Team1.Trim() : "Команда 1";
            Team2Name = !string.IsNullOrWhiteSpace(_match.Team2) ? _match.Team2.Trim() : "Команда 2";

            Team1Initials = BuildInitials(Team1Name);
            Team2Initials = BuildInitials(Team2Name);

            if (_tournament.Teams != null)
            {
                var team1Obj = _tournament.Teams.FirstOrDefault(t =>
                    t != null && string.Equals(t.Name, _match.Team1, StringComparison.OrdinalIgnoreCase));
                var team2Obj = _tournament.Teams.FirstOrDefault(t =>
                    t != null && string.Equals(t.Name, _match.Team2, StringComparison.OrdinalIgnoreCase));

                if (team1Obj != null && !string.IsNullOrWhiteSpace(team1Obj.LogoUrl))
                {
                    Team1LogoUrl = team1Obj.LogoUrl;
                }

                if (team2Obj != null && !string.IsNullOrWhiteSpace(team2Obj.LogoUrl))
                {
                    Team2LogoUrl = team2Obj.LogoUrl;
                }
            }

            // Разбор партий (SetsBySet)
            var validSets = new List<Tuple<int, int>>();
            int setsWon1 = 0;
            int setsWon2 = 0;

            if (!string.IsNullOrWhiteSpace(_match.SetsBySet))
            {
                string[] rawSets = _match.SetsBySet.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawSet in rawSets)
                {
                    string[] parts = rawSet.Split(':');
                    if (parts.Length == 2)
                    {
                        int s1;
                        int s2;
                        if (int.TryParse(parts[0].Trim(), out s1) && int.TryParse(parts[1].Trim(), out s2))
                        {
                            if (s1 > 0 || s2 > 0)
                            {
                                validSets.Add(Tuple.Create(s1, s2));
                                if (s1 > s2) setsWon1++;
                                else if (s2 > s1) setsWon2++;
                            }
                        }
                    }
                }
            }

            if (validSets.Count > 0)
            {
                SetsDetailText = string.Join("   ", validSets.Select(s => string.Format("({0}:{1})", s.Item1, s.Item2)));
            }
            else
            {
                SetsDetailText = string.Empty;
            }

            // Итоговый счёт и определение победителя по фактическому результату
            int mainScore1 = 0;
            int mainScore2 = 0;
            bool hasParsedMainScore = false;

            if (!string.IsNullOrWhiteSpace(_match.SetsScore))
            {
                string[] parts = _match.SetsScore.Split(':');
                if (parts.Length == 2)
                {
                    int left;
                    int right;
                    if (int.TryParse(parts[0].Trim(), out left) && int.TryParse(parts[1].Trim(), out right))
                    {
                        mainScore1 = left;
                        mainScore2 = right;
                        hasParsedMainScore = true;
                    }
                }
            }

            if (!hasParsedMainScore || (mainScore1 == 0 && mainScore2 == 0))
            {
                int q1;
                int q2;
                if (int.TryParse(_match.Team1QuickScore, out q1) && int.TryParse(_match.Team2QuickScore, out q2) && (q1 > 0 || q2 > 0))
                {
                    mainScore1 = q1;
                    mainScore2 = q2;
                    hasParsedMainScore = true;
                }
                else if (setsWon1 > 0 || setsWon2 > 0)
                {
                    mainScore1 = setsWon1;
                    mainScore2 = setsWon2;
                    hasParsedMainScore = true;
                }
            }

            Score1Text = mainScore1.ToString();
            Score2Text = mainScore2.ToString();

            if (hasParsedMainScore && (mainScore1 > 0 || mainScore2 > 0))
            {
                if (mainScore1 > mainScore2)
                {
                    IsTeam1Winner = true;
                    IsTeam2Winner = false;
                    WinnerName = Team1Name;
                }
                else if (mainScore2 > mainScore1)
                {
                    IsTeam1Winner = false;
                    IsTeam2Winner = true;
                    WinnerName = Team2Name;
                }
            }

            // Время начала игры
            if (!string.IsNullOrWhiteSpace(_match.StartTime))
            {
                StartTimeText = _match.StartTime.Trim();
            }
            else
            {
                StartTimeText = string.Empty;
            }

            // Дата проведения матча
            if (_match.MatchDate.HasValue)
            {
                DateText = _match.MatchDate.Value.ToString("yyyy-MM-dd");
            }
            else
            {
                DateText = string.Empty;
            }

            // Судья
            if (!string.IsNullOrWhiteSpace(_match.Referee))
            {
                RefereeText = _match.Referee.Trim();
            }
            else
            {
                RefereeText = string.Empty;
            }

            // MVP
            if (!string.IsNullOrWhiteSpace(_match.Mvp))
            {
                MvpText = _match.Mvp.Trim();
            }
            else
            {
                MvpText = string.Empty;
            }
        }

        private static string BuildInitials(string teamName)
        {
            if (string.IsNullOrWhiteSpace(teamName))
                return "К";

            string trimmed = teamName.Trim();
            string[] words = trimmed.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
            }

            return trimmed.Length >= 2
                ? trimmed.Substring(0, 2).ToUpperInvariant()
                : trimmed.Substring(0, 1).ToUpperInvariant();
        }

        private void SelectBackground()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите изображение заднего фона карточки",
                Filter = "Изображения (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string encoded = ImageStorageHelper.EncodeImageFileToBase64(dialog.FileName, 1600, 1000, true);
                    CardBackgroundData = encoded;
                    _tournament.CardBackgroundData = encoded;

                    if (_storage.IsTournamentEditable(_tournament))
                    {
                        _storage.UpdateTournament(_tournament);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Не удалось загрузить фоновое изображение:\n" + ex.Message,
                        "Ошибка загрузки фона",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ResetBackground()
        {
            CardBackgroundData = string.Empty;
            _tournament.CardBackgroundData = string.Empty;

            if (_storage.IsTournamentEditable(_tournament))
            {
                _storage.UpdateTournament(_tournament);
            }
        }
    }
}

using SportHubBase.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportHubBase.ViewModels
{
    /// Объект, передаваемый в DataTemplateSelector.
    /// Содержит рассчитанную статистику для отображения.
    public class TournamentStatistics : INotifyPropertyChanged
    {
        private string _sportType;
        public string SportType
        {
            get => _sportType;
            set { _sportType = value; OnPropertyChanged(); }
        }

        // --- Сводка ---
        public int TotalMatches { get; set; }
        public int PlayedMatches { get; set; }
        public int RemainingMatches { get; set; }
        public int FiveSetMatches { get; set; } // Количество пятисетовок (3:2 или 2:3)
        public string MostProductiveMatch { get; set; } = "—"; // Самый результативный матч
        public int TechnicalDefeatsCount { get; set; } // Количество технических поражений
        public double AvgGoals { get; set; } // Средняя разыгровка мячей

        // --- Лидер ---
        public string LeaderName { get; set; } = "—";
        public string LeaderTeam { get; set; }
        public int LeaderPoints { get; set; }
        public string LeaderForm { get; set; } // W D L W W

        // --- Списки и блоки ---
        public ObservableCollection<StatBlock> ExtraBlocks { get; set; } = new ObservableCollection<StatBlock>();
        public ObservableCollection<MvpItem> TopMvps { get; set; } = new ObservableCollection<MvpItem>();
        public ObservableCollection<TeamMatchHistory> LastMatches { get; set; } = new ObservableCollection<TeamMatchHistory>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
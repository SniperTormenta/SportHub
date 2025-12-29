// ViewModels/TournamentStatistics.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportHubBase.ViewModels
{
    /// Объект, передаваемый в DataTemplateSelector.
    /// Содержит только информацию о виде спорта (для выбора шаблона).
    /// При необходимости можно добавить общие свойства.
    public class TournamentStatistics : INotifyPropertyChanged
    {
        private string _sportType;
        public string SportType
        {
            get => _sportType;
            set { _sportType = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
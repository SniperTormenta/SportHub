using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportHubBase.ViewModels
{
    // Класс одного сета
    public class SetScore : INotifyPropertyChanged
    {
        public int Number { get; set; }

        private int _score1;
        public int Score1
        {
            get => _score1;
            set { _score1 = value; OnPropertyChanged(); }
        }

        private int _score2;
        public int Score2
        {
            get => _score2;
            set { _score2 = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

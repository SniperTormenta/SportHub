using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class StatBlock : INotifyPropertyChanged
    {
        private string _title;
        private string _value;
        private string _iconKind;
        private string _color;

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public string IconKind
        {
            get => _iconKind;
            set { _iconKind = value; OnPropertyChanged(); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

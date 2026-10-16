using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SportHubBase.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CellResult : INotifyPropertyChanged
    {
        private string _displayText;
        private string _outcome;
        private string _selfImageData;

        public string DisplayText
        {
            get { return _displayText; }
            set
            {
                if (_displayText != value)
                {
                    _displayText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Outcome
        {
            get { return _outcome; }
            set
            {
                if (_outcome != value)
                {
                    _outcome = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsSelf));
                    OnPropertyChanged(nameof(IsPlayed));
                    OnPropertyChanged(nameof(IsWin));
                    OnPropertyChanged(nameof(IsLoss));
                    OnPropertyChanged(nameof(IsDraw));
                    OnPropertyChanged(nameof(HasSelfImage));
                }
            }
        }

        /// <summary>
        /// Строка с данными изображения для серой ячейки (SELF):
        /// "preset:volleyball", "preset:football", "preset:basketball", "base64:..." или путь к файлу.
        /// </summary>
        public string SelfImageData
        {
            get { return _selfImageData; }
            set
            {
                if (_selfImageData != value)
                {
                    _selfImageData = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelfImage));
                }
            }
        }

        public int? HomeSets { get; set; }
        public int? AwaySets { get; set; }
        public bool IsTechnicalDefeat { get; set; }

        public bool IsSelf
        {
            get { return Outcome == "SELF"; }
        }

        public bool HasSelfImage
        {
            get { return IsSelf && !string.IsNullOrWhiteSpace(SelfImageData); }
        }

        public bool IsPlayed
        {
            get { return Outcome != "NOT_PLAYED" && Outcome != "SELF"; }
        }

        public bool IsWin
        {
            get { return Outcome == "WIN"; }
        }

        public bool IsLoss
        {
            get { return Outcome == "LOSS"; }
        }

        public bool IsDraw
        {
            get { return Outcome == "DRAW"; }
        }

        public CellResult()
        {
            _displayText = string.Empty;
            _outcome = "NOT_PLAYED";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
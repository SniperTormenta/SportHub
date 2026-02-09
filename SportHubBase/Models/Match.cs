// Models/Match.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SportHubBase.Models
{
    /// <summary>
    /// Простая модель матча для расписания.
    /// Теперь хранит статус, быстрый счёт и дополнительные данные карточки матча.
    /// </summary>
    public class Match : INotifyPropertyChanged
    {
        private int _round;
        private string _team1;
        private string _team2;
        private string _status = "Не сыгран";
        private string _team1QuickScore;
        private string _team2QuickScore;
        private string _setsScore;
        private string _setsBySet;
        private string _totalScore;
        private string _duration;
        private string _referee;
        private string _location;
        private string _mvp;

        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Порядковый номер матча (для поиска и отображения).
        /// </summary>
        public int? MatchNumber { get; set; }

        public int Round
        {
            get => _round;
            set => SetField(ref _round, value);
        }

        public string Team1
        {
            get => _team1;
            set => SetField(ref _team1, value);
        }

        public string Team2
        {
            get => _team2;
            set => SetField(ref _team2, value);
        }

        /// <summary>
        /// Статус матча: Не сыгран / Идёт / Сыгран / Перенесён и т.д.
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        /// <summary>
        /// Быстрый ввод счёта слева (для списков).
        /// </summary>
        public string Team1QuickScore
        {
            get => _team1QuickScore;
            set => SetField(ref _team1QuickScore, value);
        }

        /// <summary>
        /// Быстрый ввод счёта справа (для списков).
        /// </summary>
        public string Team2QuickScore
        {
            get => _team2QuickScore;
            set => SetField(ref _team2QuickScore, value);
        }

        /// <summary>
        /// Счёт по сетам, например "3:1".
        /// </summary>
        public string SetsScore
        {
            get => _setsScore;
            set => SetField(ref _setsScore, value);
        }

        /// <summary>
        /// Счёт матчей по сетам (детализация, например "25:20, 21:25...").
        /// </summary>
        public string SetsBySet
        {
            get => _setsBySet;
            set => SetField(ref _setsBySet, value);
        }

        /// <summary>
        /// Общий счёт мячей, например "75:68".
        /// </summary>
        public string TotalScore
        {
            get => _totalScore;
            set => SetField(ref _totalScore, value);
        }

        /// <summary>
        /// Длительность матча (в минутах или формате HH:MM).
        /// </summary>
        public string Duration
        {
            get => _duration;
            set => SetField(ref _duration, value);
        }

        public string Referee
        {
            get => _referee;
            set => SetField(ref _referee, value);
        }

        public string Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        /// <summary>
        /// MVP матча (один игрок из обеих команд).
        /// </summary>
        public string Mvp
        {
            get => _mvp;
            set => SetField(ref _mvp, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}



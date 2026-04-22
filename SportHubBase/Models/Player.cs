// Models/Player.cs
using SportHubBase.ViewModels;
using System;

namespace SportHubBase.Models
{
    /// <summary>
    /// Модель игрока. Наследует BaseViewModel для уведомлений об изменениях.
    /// Содержит Id (Guid) для хранения в SQLite и привязки к команде.
    /// </summary>
    public class Player : BaseViewModel
    {
        private Guid _id = Guid.NewGuid();
        private string _name;
        private string _role = "Обычный игрок";
        private bool _isCaptain;
        private Guid? _userId;

        /// <summary>
        /// Уникальный идентификатор игрока.
        /// </summary>
        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _nickname;
        public string Nickname
        {
            get => _nickname;
            set { _nickname = value; OnPropertyChanged(); }
        }

        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }

        public Guid? UserId
        {
            get => _userId;
            set { _userId = value; OnPropertyChanged(); }
        }
    }
}

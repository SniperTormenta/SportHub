using SportHubBase.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportHubBase.Models
{
    public class Player : BaseViewModel // Наследуем от BaseViewModel для уведомлений
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        private string _role = "Обычный игрок";
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        private bool _isCaptain;
        public bool IsCaptain
        {
            get => _isCaptain;
            set { _isCaptain = value; OnPropertyChanged(); }
        }
    }
}

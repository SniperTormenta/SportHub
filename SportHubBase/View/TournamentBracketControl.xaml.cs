using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SportHubBase.Models;
using SportHubBase.ViewModels;

namespace SportHubBase.View
{
    /// <summary>
    /// Логика взаимодействия для TournamentBracketControl.xaml
    /// </summary>
    public partial class TournamentBracketControl : UserControl
    {
        public TournamentBracketControl()
        {
            InitializeComponent();
            DataContextChanged += (s, e) =>
            {
                if (DataContext is TournamentViewModel vm)
                {
                    // Здесь можно подготовить AllMatches / Connections
                    // Но пока просто оставь — главное, чтобы VM был доступен
                }
            };
        }
   
    }
}
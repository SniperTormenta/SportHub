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
            
            // Пересчитываем линии при изменении размеров или обновлении макета
            SizeChanged += (s, e) => UpdateLines();
            LayoutUpdated += (s, e) => UpdateLines();
        }

        /// <summary>
        /// Отрисовывает соединительные линии между матчами.
        /// </summary>
        private void UpdateLines()
        {
            if (LineCanvas == null || RoundsContainer == null) return;
            
            LineCanvas.Children.Clear();
            
            var vm = DataContext as TournamentViewModel;
            if (vm == null || vm.Bracket == null) return;

            // 1. Находим все визуальные элементы (бордеры) матчей
            var matchBorders = new Dictionary<Guid, Border>();
            FindMatchBorders(RoundsContainer, matchBorders);

            // 2. Рисуем связи для каждого матча
            foreach (var round in vm.Bracket.Rounds)
            {
                foreach (var match in round.Matches)
                {
                    // Связь с следующим матчем (победитель)
                    if (match.NextMatch != null && 
                        matchBorders.TryGetValue(match.Id, out var start) && 
                        matchBorders.TryGetValue(match.NextMatch.Id, out var end))
                    {
                        DrawConnection(start, end, match.IsTeam1InNext);
                    }

                    // Связь с матчем за 3-е место (проигравший)
                    if (match.BronzeLoserTarget != null && 
                        matchBorders.TryGetValue(match.Id, out var s) && 
                        matchBorders.TryGetValue(match.BronzeLoserTarget.Id, out var e))
                    {
                        DrawConnection(s, e, match.IsTeam1InNext);
                    }
                }
            }
        }

        private void FindMatchBorders(DependencyObject parent, Dictionary<Guid, Border> dict)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border b && b.Tag is Guid id)
                {
                    dict[id] = b;
                }
                FindMatchBorders(child, dict);
            }
        }

        private void DrawConnection(Border start, Border end, bool isTargetTop)
        {
            // Точка выхода (справа в центре)
            var pStart = start.TranslatePoint(new Point(start.ActualWidth, start.ActualHeight / 2), LineCanvas);
            
            // Точка входа (слева в верхней или нижней четверти следующего матча)
            double targetY = isTargetTop ? end.ActualHeight * 0.25 : end.ActualHeight * 0.75;
            var pEnd = end.TranslatePoint(new Point(0, targetY), LineCanvas);

            Polyline line = new Polyline
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };

            // Ступенчатая линия
            double midX = pStart.X + (pEnd.X - pStart.X) / 2;
            line.Points.Add(pStart);
            line.Points.Add(new Point(midX, pStart.Y));
            line.Points.Add(new Point(midX, pEnd.Y));
            line.Points.Add(pEnd);

            LineCanvas.Children.Add(line);
        }
    }
}

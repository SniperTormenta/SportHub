using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SportHubBase.ViewModels;

namespace SportHubBase.View
{
    public partial class MatchCardPreviewWindow : Window
    {
        private readonly MatchCardPreviewViewModel _viewModel;

        public MatchCardPreviewWindow(Window owner, MatchCardPreviewViewModel viewModel)
        {
            InitializeComponent();
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            _viewModel.RequestClose += (sender, result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _viewModel.EncoderFactory == null)
                return;

            PreviewRoot.UpdateLayout();

            double baseWidth = PreviewRoot.ActualWidth > 0 ? PreviewRoot.ActualWidth : 1120;
            double baseHeight = PreviewRoot.ActualHeight > 0 ? PreviewRoot.ActualHeight : 720;

            // Рендерим в повышенном разрешении (1.5x -> 1680x1080) для чёткой публикации в соцсетях
            const double scale = 1.5;
            int renderWidth = (int)Math.Round(baseWidth * scale);
            int renderHeight = (int)Math.Round(baseHeight * scale);

            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(PreviewRoot)
                {
                    Stretch = Stretch.Fill
                };
                dc.DrawRectangle(vb, null, new Rect(0, 0, renderWidth, renderHeight));
            }

            var rtb = new RenderTargetBitmap(renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            string safeFileName = string.Format("{0} - {1} vs {2}",
                _viewModel.TournamentName,
                _viewModel.Team1Name,
                _viewModel.Team2Name);

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeFileName = safeFileName.Replace(invalidChar, '_');
            }

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить карточку матча как изображение",
                Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                FileName = safeFileName
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string extension = Path.GetExtension(dialog.FileName);
            var strategy = _viewModel.EncoderFactory.GetByExtension(extension);

            try
            {
                using (var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                {
                    strategy.Encode(rtb, stream);
                }

                MessageBox.Show(this,
                    "Карточка матча успешно сохранена!",
                    "Экспорт завершён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось сохранить карточку матча.\n" + ex.Message,
                    "Ошибка экспорта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}

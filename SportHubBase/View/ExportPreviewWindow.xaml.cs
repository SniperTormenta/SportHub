using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SportHubBase.ViewModels;

namespace SportHubBase.View
{
    public partial class ExportPreviewWindow : Window
    {
        private readonly ExportPreviewViewModel _viewModel;

        public ExportPreviewWindow(Window owner, ExportPreviewViewModel viewModel)
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

        public ExportPreviewWindow() : this(null, null)
        {
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _viewModel.EncoderFactory == null)
                return;

            // Обеспечиваем корректный размер визуального элемента
            EnsureMeasured(PreviewRoot);

            double baseWidth = PreviewRoot.ActualWidth;
            double baseHeight = PreviewRoot.ActualHeight;

            if (baseWidth <= 0 || baseHeight <= 0)
                return;

            const double scale = 1.5;
            int width = (int)Math.Ceiling(baseWidth * scale);
            int height = (int)Math.Ceiling(baseHeight * scale);

            var dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                var vb = new VisualBrush(PreviewRoot)
                {
                    Stretch = Stretch.Fill
                };
                dc.DrawRectangle(vb, null, new Rect(0, 0, width, height));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            string safeFileName = string.Format("{0} - {1}", _viewModel.TournamentName, _viewModel.TableTitle);
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeFileName = safeFileName.Replace(invalidChar, '_');
            }

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить таблицу как изображение",
                Filter = "PNG изображение (*.png)|*.png|JPEG изображение (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                FileName = safeFileName
            };

            if (dialog.ShowDialog(this) != true)
                return;

            var extension = Path.GetExtension(dialog.FileName);
            var strategy = _viewModel.EncoderFactory.GetByExtension(extension);

            try
            {
                using (var stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                {
                    strategy.Encode(rtb, stream);
                }

                MessageBox.Show(this,
                    "Таблица успешно сохранена.",
                    "Экспорт завершён",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _viewModel.Close(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Не удалось сохранить изображение таблицы.\n" + ex.Message,
                    "Ошибка экспорта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void EnsureMeasured(FrameworkElement element)
        {
            if (element == null)
                return;

            if (double.IsNaN(element.ActualWidth) || double.IsNaN(element.ActualHeight) ||
                element.ActualWidth == 0 || element.ActualHeight == 0)
            {
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                element.Arrange(new Rect(element.DesiredSize));
                element.UpdateLayout();
            }
        }
    }
}


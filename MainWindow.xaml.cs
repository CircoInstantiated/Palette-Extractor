using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PaletteExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isProcessing = false;
        private PaletteExtractorViewModel viewModel = new PaletteExtractorViewModel();
        private PaletteBuilder paletteBuilder = new PaletteBuilder();
        private readonly Regex oneToSixteenTest = new Regex("^([1-9]|1[0-6])$");
        private readonly Regex oneToFiveHundredTwelveTest = new Regex("^(?:50[0-9]|51[0-2]|[1-4]?[0-9]?[0-9])$");
        private readonly string fileFilter;
        private readonly string validExtensions;
        private Comparison<Color> sortBy = (a, b) => a.GetHue().CompareTo(b.GetHue());

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = viewModel;
            var imageCodecInfo = ImageCodecInfo.GetImageEncoders();
            var filter = new StringBuilder("All Files (*.*)|*.*;");
            var extensions = new StringBuilder();
            foreach (var codec in imageCodecInfo)
            {
                var name = codec.CodecName.Substring(8).Replace("Codec", "Files").Trim();
                extensions.Append(codec.FilenameExtension.ToLowerInvariant());
                filter.AppendFormat("|{0} ({1})|{1};", name, codec.FilenameExtension.ToLowerInvariant());
            }
            fileFilter = filter.ToString();
            validExtensions = extensions.ToString();
        }

        private void AddFiles(object sender, RoutedEventArgs e)
        {
            if (isProcessing)
                return;
            isProcessing = true;
            var fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.DefaultExt = "png";
            fileDialog.Filter = fileFilter;
            if (fileDialog.ShowDialog() == true)
            {
                var files = fileDialog.FileNames?.Select(file => new FileInfo(file)).Where(file => validExtensions.Contains(file.Extension.ToLowerInvariant()));
                if (viewModel.Files == null)
                    viewModel.Files = new ObservableCollection<FileInfo>(files);
                else
                {
                    foreach (var file in files)
                    {
                        if (!viewModel.Files.Any(f => f.Name.Equals(file.Name)))
                            viewModel.Files.Add(file);
                    }
                }
            }
            isProcessing = false;
        }

        private async void GeneratePalette(object sender, RoutedEventArgs e)
        {
            if (isProcessing)
                return;
            if (viewModel.Files.Count < 1)
            {
                MessageBox.Show("Please select one or more files from which to generate the palette.");
                return;
            }
            viewModel.Progress = 0;
            isProcessing = true;
            var progress = new Progress<float>(value => viewModel.Progress = value * 100);
            await paletteBuilder.Calculate(viewModel.Files, sortBy, viewModel.MaxColors, viewModel.MaxIterations, progress);
            viewModel.Image = await GenerateBitmap(paletteBuilder.Palette, viewModel.TileSize, viewModel.ColorsPerRow);
            isProcessing = false;
        }

        private async void ExportPalette(object sender, RoutedEventArgs e)
        {
            if (isProcessing)
                return;
            isProcessing = true;
            var file = string.Empty;
            var fileDialog = new SaveFileDialog();
            fileDialog.ValidateNames = true;
            fileDialog.FileName = $"Palette ({viewModel.MaxColors}) {DateTime.Now:MMddyyyy-HHmmss}.png";
            fileDialog.DefaultExt = "png";
            fileDialog.Filter = "PNG Files (*.png)|*.png;";
            if (fileDialog.ShowDialog() == true)
                file = fileDialog.FileName;
            if (!string.IsNullOrWhiteSpace(file))
            {
                using (var bitmap = await GenerateBitmap(paletteBuilder.Palette, viewModel.TileSize, viewModel.ColorsPerRow))
                {
                    bitmap.Save(file, ImageFormat.Png);
                }
            }
            isProcessing = false;
        }

        private async Task<Bitmap> GenerateBitmap(Color[] palette, int tileSize, int colorsPerRow)
        {
            var length = palette.Length;
            if (length < 1)
                return null;
            var width = length < colorsPerRow ? length * tileSize : colorsPerRow * tileSize;
            var height = length < colorsPerRow ? tileSize : length / colorsPerRow * tileSize;
            if (length % colorsPerRow > 0)
                height += tileSize;
            var image = new Bitmap(width, height);
            await Task.Run(() => {
                var context = Graphics.FromImage(image);
                var positionX = 0;
                var positionY = 0;
                var colorsPlaced = 0;
                for (var i = 0; i < length; i++)
                {
                    var color = palette[i];
                    var brush = new SolidBrush(color);
                    context.FillRectangle(brush, positionX, positionY, tileSize, tileSize);
                    colorsPlaced++;
                    if (colorsPlaced >= colorsPerRow)
                    {
                        positionX = colorsPlaced = 0;
                        positionY += tileSize;
                    }
                    else
                        positionX += tileSize;
                }
            });
            return image;
        }

        private void ValidateTextIsWithinNumericRange1To16(object sender, TextCompositionEventArgs e)
        {
            if (isProcessing)
                e.Handled = true;
            else
            {
                var textBox = e.Source as TextBox;
                var updatedText = textBox.SelectedText?.Length > 0 ? textBox.Text?.Replace(textBox.SelectedText, e.Text) : textBox.Text + e.Text;
                var matched = oneToSixteenTest.IsMatch(updatedText);
                e.Handled = !matched;
            }
        }

        private void ValidateTextIsWithinNumericRange1To500(object sender, TextCompositionEventArgs e)
        {
            if (isProcessing)
                e.Handled = true;
            else
            {
                var textBox = e.Source as TextBox;
                var updatedText = textBox.SelectedText?.Length > 0 ? textBox.Text?.Replace(textBox.SelectedText, e.Text) : textBox.Text + e.Text;
                var matched = oneToFiveHundredTwelveTest.IsMatch(updatedText);
                e.Handled = !matched;
            }
        }

        private void ValidateTextIsWithinNumericRange1To512(object sender, TextCompositionEventArgs e)
        {
            if (isProcessing)
                e.Handled = true;
            else
            {
                var textBox = e.Source as TextBox;
                var updatedText = textBox.SelectedText?.Length > 0 ? textBox.Text?.Replace(textBox.SelectedText, e.Text) : textBox.Text + e.Text;
                var matched = oneToFiveHundredTwelveTest.IsMatch(updatedText);
                e.Handled = !matched;
            }
        }

        private void ChangeSortMethod(object sender, RoutedEventArgs e)
        {
            var button = sender as RadioButton;
            switch (button.Content?.ToString().ToLowerInvariant())
            {
                case "hue":
                    sortBy = (a, b) => a.GetHue().CompareTo(b.GetHue());
                    break;
                case "brightness":
                    sortBy = (a, b) => a.GetBrightness().CompareTo(b.GetBrightness());
                    break;
                case "saturation":
                    sortBy = (a, b) => a.GetSaturation().CompareTo(b.GetSaturation());
                    break;
            }
        }

        private void ClearFiles(object sender, RoutedEventArgs e)
        {
            viewModel.Files?.Clear();
        }
    }
}

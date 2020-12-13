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
        private readonly Regex greaterThanZeroTest = new Regex("^[1-9][0-9]*$");
        private readonly Regex zeroToOneHundredTest = new Regex("^(?:100|[1-9]?[0-9])$");
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

        private async void OpenFiles(object sender, RoutedEventArgs e)
        {
            if (isProcessing)
                return;
            isProcessing = true;
            await Task.Run(() => {
                var fileDialog = new OpenFileDialog();
                fileDialog.Multiselect = true;
                fileDialog.DefaultExt = "png";
                fileDialog.Filter = fileFilter;
                if (fileDialog.ShowDialog() == true)
                {
                    var files = fileDialog.FileNames?.Select(file => new FileInfo(file)).Where(file => validExtensions.Contains(file.Extension.ToLowerInvariant()));
                    viewModel.Files = new ObservableCollection<FileInfo>(files);
                }
            });
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
            isProcessing = true;
            await paletteBuilder.Calculate(viewModel.Files, sortBy, viewModel.MaxColors);
            viewModel.Image = await paletteBuilder.ToBitmap(viewModel.TileSize, viewModel.ColorsPerRow);
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
            fileDialog.FileName = $"Palette {DateTime.Now:MMddyyyy-HHmmss}.png";
            fileDialog.DefaultExt = "png";
            fileDialog.Filter = "PNG Files (*.png)|*.png;";
            if (fileDialog.ShowDialog() == true)
                file = fileDialog.FileName;
            if (!string.IsNullOrWhiteSpace(file))
                await paletteBuilder.Export(file, ImageFormat.Png, viewModel.TileSize, viewModel.ColorsPerRow);
            isProcessing = false;
        }

        private void ValidateTextIsNumeric(object sender, TextCompositionEventArgs e)
        {
            if (isProcessing)
                e.Handled = true;
            else
            {
                var textBox = e.Source as TextBox;
                var updatedText = textBox.SelectedText?.Length > 0 ? textBox.Text?.Replace(textBox.SelectedText, e.Text) : textBox.Text + e.Text;
                var matched = greaterThanZeroTest.IsMatch(updatedText);
                e.Handled = !matched;
            }
        }

        private void ValidateTextIsWithinNumericRange0To100(object sender, TextCompositionEventArgs e)
        {
            if (isProcessing)
                e.Handled = true;
            else
            {
                var textBox = e.Source as TextBox;
                var updatedText = textBox.SelectedText?.Length > 0 ? textBox.Text?.Replace(textBox.SelectedText, e.Text) : textBox.Text + e.Text;
                var matched = zeroToOneHundredTest.IsMatch(updatedText);
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
    }
}

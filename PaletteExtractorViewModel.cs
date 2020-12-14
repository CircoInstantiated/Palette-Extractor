using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace PaletteExtractor
{
    class PaletteExtractorViewModel : ViewModel
    {
        protected const int defaultTileSize = 4;
        protected const int defaultColorsPerRow = 16;
        protected const int defaultMaxColors = 512;
        protected const int defaultMaxIterations = 200;
        protected readonly BitmapComparer comparer = new BitmapComparer();
        protected int tileSize = defaultTileSize;
        protected int colorsPerRow = defaultColorsPerRow;
        protected int maxColors = defaultMaxColors;
        protected int maxIterations = defaultMaxIterations;
        protected float progress = 0f;
        protected ObservableCollection<FileInfo> files = new ObservableCollection<FileInfo>();
        protected Bitmap image = null;

        public Bitmap Image { get => image; set { 
                if (SetProperty(ref image, value, comparer: comparer)) 
                    OnPropertyChanged("PaletteSource"); 
            } }
        public BitmapSource PaletteSource { get => image?.ToBitmapSource(); }
        public int TileSize { get => tileSize; set => SetProperty(ref tileSize, value); }
        public int ColorsPerRow { get => colorsPerRow; set => SetProperty(ref colorsPerRow, value); }
        public int MaxColors { get => maxColors; set => SetProperty(ref maxColors, value); }
        public int MaxIterations { get => maxIterations; set => SetProperty(ref maxIterations, value); }
        public ObservableCollection<FileInfo> Files { get => files; set => SetProperty(ref files, value); }
        public float Progress { get => progress; set => SetProperty(ref progress, value); }
    }
}

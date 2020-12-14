using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PaletteExtractor
{
    sealed class PaletteBuilder
    {
        //https://github.com/xanderlewis/colour-palettes
        private static readonly Random random = new Random();
        private int tasksCompleted = 0;
        private float totalTasks = 0;
        private Comparison<Color> sortByHue = (a, b) => a.GetHue().CompareTo(b.GetHue());
        public Color[] Palette { get; private set; }

        public async Task Calculate(IEnumerable<FileInfo> files, Comparison<Color> sortBy = null, int maxColors = 0, int maxKMeansInterations = 200, IProgress<float> progress = null)
        {
            totalTasks = files.Count() + maxKMeansInterations + 1;
            await Task.Run(() => {
                var argbValues = new List<int>();
                tasksCompleted = 0;
                foreach (var file in files)
                {
                    using (var image = new Bitmap(file.OpenRead()))
                    {
                        argbValues.AddRange(ExtractPalette(image));
                        tasksCompleted++;
                        progress?.Report(tasksCompleted / totalTasks);
                    }
                }
                argbValues.Sort();
                var palette = argbValues.Distinct().Select((argb) => Color.FromArgb(argb)).ToArray();
                if (maxColors == 1)
                {
                    var color = AveragedColor(palette);
                    palette = new Color[] { color };
                    progress?.Report(100f);
                }
                else if (maxColors > 0 && maxColors < palette.Length)
                {
                    var clusters = CalculateKMeanClusters(palette, maxColors, maxKMeansInterations, centroids: GetCentroidsByUsage(argbValues, maxColors), progress: progress);
                    palette = AveragePalette(clusters);
                }
                var p = new List<Color>(palette);
                p.Sort(sortBy ?? sortByHue);
                progress?.Report(100f);
                Palette = p.ToArray();
            });
        }

        private IEnumerable<int> ExtractPalette(Bitmap bitmap)
        {
            var palette = new List<int>();
            for (var x = 0; x < bitmap.Width; x++)
                for (var y = 0; y < bitmap.Height; y++)
                    palette.Add(bitmap.GetPixel(x, y).ToArgb());
            return palette;
        }

        private Color AveragedColor(params Color[] colors)
        {
            if (colors?.Length < 1)
                return Color.Transparent;
            else if (colors.Length < 2)
                return colors[0];
            var alphaSquared = new double[colors.Length];
            var redsSquared = new double[colors.Length];
            var greensSquared = new double[colors.Length];
            var bluesSquared = new double[colors.Length];
            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                alphaSquared[i] = Math.Pow(color.A, 2);
                redsSquared[i] = Math.Pow(color.R, 2);
                greensSquared[i] = Math.Pow(color.G, 2);
                bluesSquared[i] = Math.Pow(color.B, 2);
            }
            var alpha = (int)Math.Sqrt(alphaSquared.Sum() / alphaSquared.Length);
            var red = (int)Math.Sqrt(redsSquared.Sum() / redsSquared.Length);
            var green = (int)Math.Sqrt(greensSquared.Sum() / greensSquared.Length);
            var blue = (int)Math.Sqrt(bluesSquared.Sum() / bluesSquared.Length);
            return Color.FromArgb(alpha, red, green, blue);
        }

        private class ReverseDuplicateKeyComparer<T> : IComparer<T> where T : IComparable
        {
            public int Compare(T x, T y)
            {
                var result = x?.CompareTo(y) ?? 0;
                return result == 0 ? -1 : -result;
            }
        }

        private Color[] GetCentroidsByUsage(List<int> sortedArgbInts, int max)
        {
            var colorCounts = new SortedList<int, int>(new ReverseDuplicateKeyComparer<int>());
            var processedARGBInts = new HashSet<int>();
            for (var i = 0; i < sortedArgbInts.Count;)
            {
                var argb = sortedArgbInts[i];
                if (processedARGBInts.Contains(argb))
                    continue;
                var lastIndex = sortedArgbInts.FindLastIndex(sortedArgbInts.Count - 1, sortedArgbInts.Count - i, (a) => a == argb);
                var count = lastIndex + 1 - i;
                colorCounts.Add(count, argb);
                processedARGBInts.Add(argb);
                i += count;
            }
            return colorCounts.Values.Top(max).Select(i => Color.FromArgb(i)).ToArray();
        }

        private List<Color>[] CalculateKMeanClusters(Color[] palette, int maxColors, int maxIterations, int iterations = 0, Color[] centroids = null, IProgress<float> progress = null)
        {
            var currentClusters = default(List<Color>[]);
            var previousCluster = default(List<Color>[]);
            centroids ??= RandomizeCentroids(palette, maxColors);
            while (iterations < maxIterations)
            {
                previousCluster = currentClusters;
                currentClusters = GetClustedDataPoints(palette, centroids);
                iterations++;
                tasksCompleted++;
                progress?.Report(tasksCompleted / totalTasks);
                if (currentClusters.Any((c) => c.Count < 1))
                {
                    currentClusters = null;
                    previousCluster = null;
                    centroids = null;
                    return CalculateKMeanClusters(palette, maxColors, maxIterations, iterations, null, progress);
                }
                if (ClustersMatch(currentClusters, previousCluster))
                {
                    progress?.Report(100f);
                    break;
                }
                centroids = AveragePalette(currentClusters);
            }
            return currentClusters;
        }

        private bool ClustersMatch(List<Color>[] source, List<Color>[] evaluee)
        {
            if (source == null || evaluee == null || source.Length != evaluee.Length)
                return false;
            for (var i = 0; i < source.Length; i++)
            {
                var s = source[i];
                var e = evaluee[i];
                if (s.Count != e.Count)
                    return false;
                for (var x = 0; x < s.Count; x++)
                {
                    if (!s[x].ToArgb().Equals(e[x].ToArgb()))
                        return false;
                }
            }
            return true;
        }

        private Color[] RandomizeCentroids(Color[] palette, int maxColors)
        {
            var centroids = new Color[maxColors];
            var usedIndices = new List<int>();
            for (var i = 0; i < maxColors; i++)
            {
                var index = NextIndex(palette.Length, usedIndices);
                centroids[i] = palette[index];
                usedIndices.Add(index);
            }
            return centroids;
        }

        private int NextIndex(int maxIndex, IEnumerable<int> usedIndices)
        {
            int i;
            do
                i = random.Next(0, maxIndex);
            while
                (usedIndices.Contains(i));
            return i;
        }

        private float CalculateEuclidianDistance(ref Color source, ref Color evaluee)
        {
            var distance = MathF.Pow(evaluee.A - source.A, 2);
            distance += MathF.Pow(evaluee.R - source.R, 2);
            distance += MathF.Pow(evaluee.G - source.G, 2);
            distance += MathF.Pow(evaluee.B - source.B, 2);
            return MathF.Sqrt(distance);
        }

        private Color[] AveragePalette(List<Color>[] clusters)
        {
            var centroids = new Color[clusters.Length];
            for (var i = 0; i < centroids.Length; i++)
            {
                var cluster = clusters[i];
                centroids[i] = AveragedColor(cluster.ToArray());
            }
            return centroids;
        }

        private List<Color>[] GetClustedDataPoints(Color[] palette, Color[] paletteCentroids)
        {
            if (palette?.Length < 1)
                throw new ArgumentException("Cannot cluster data points from invalid palette.");
            if (paletteCentroids?.Length < 1)
                throw new ArgumentException("Cannot cluster data points from invalid centroids.");
            var clusteredDataPoints = new List<Color>[paletteCentroids.Length];
            for (var i = 0; i < clusteredDataPoints.Length; i++)
                clusteredDataPoints[i] = new List<Color>();
            for (var i = 0; i < palette.Length; i++)
            {
                var color = palette[i];
                var nearestCentroid = paletteCentroids[0];
                var index = 0;
                for (var x = 1; x < paletteCentroids.Length; x++)
                {
                    var centroid = paletteCentroids[x];
                    if (CalculateEuclidianDistance(ref color, ref centroid) < CalculateEuclidianDistance(ref color, ref nearestCentroid))
                    {
                        nearestCentroid = centroid;
                        index = x;
                    }
                }
                clusteredDataPoints[index].Add(color);
            }
            return clusteredDataPoints;
        }
    }
}

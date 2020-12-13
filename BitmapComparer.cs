using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace PaletteExtractor
{
    class BitmapComparer : EqualityComparer<Bitmap>
    {
        public override bool Equals(Bitmap x, Bitmap y)
        {
            if (x == null || y == null || object.Equals(x, y) || !x.Size.Equals(y.Size) || !x.PixelFormat.Equals(y.PixelFormat))
                return false;
            var dataLength = x.Width * x.Height * (Image.GetPixelFormatSize(x.PixelFormat) / 8);
            var xData = x.GetBytes(dataLength);
            var yData = y.GetBytes(dataLength);
            for (var i = 0; i < xData.Length; i++)
                if (xData[i] != yData[i])
                    return false;
            return true;
        }

        public override int GetHashCode([DisallowNull] Bitmap obj)
        {
            return obj.GetHashCode();
        }
    }
}

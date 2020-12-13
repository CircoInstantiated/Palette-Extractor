using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace PaletteExtractor
{
    static class BitmapExtensions
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            if (bitmap == null)
                return null;
            var handle = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }

        public static byte[] GetBytes(this Bitmap bitmap, int dataLength = 0)
        {
            if (dataLength < 1)
                dataLength = bitmap.Width * bitmap.Height * (Image.GetPixelFormatSize(bitmap.PixelFormat) / 8);
            var data = new byte[dataLength];
            var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            try
            {
                Marshal.Copy(bitmapData.Scan0, data, 0, dataLength);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
            return data;
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// An 8-bit grayscale image held as a plain byte array (row-major, 0 = black, 255 =
    /// white). Everything in the engine works on arrays like this one; System.Drawing is
    /// used only at the edges to get pixels in and to draw debug pictures out.
    /// </summary>
    public sealed class GrayImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Pixels;

        public GrayImage(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Empty image.");
            Width = width; Height = height;
            Pixels = new byte[width * height];
        }

        public byte this[int x, int y]
        {
            get { return Pixels[y * Width + x]; }
            set { Pixels[y * Width + x] = value; }
        }

        /// <summary>
        /// Reads any Bitmap as grayscale (Rec.601 weights). Locks the bits once and copies
        /// them out in one call; GetPixel per pixel is ~50x slower and was the dominant
        /// cost in the old CROMS preprocessing (measured 2026-09-02).
        /// </summary>
        public static GrayImage FromBitmap(Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var gray = new GrayImage(w, h);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                int absStride = Math.Abs(stride);
                var buf = new byte[absStride * h];
                Marshal.Copy(data.Scan0, buf, 0, buf.Length);

                for (int y = 0; y < h; y++)
                {
                    int row = stride >= 0 ? y * absStride : (h - 1 - y) * absStride;
                    int o = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x * 4;
                        int b = buf[i], g = buf[i + 1], r = buf[i + 2], a = buf[i + 3];
                        if (a < 255)   // composite over white, as a printer would
                        {
                            r = (r * a + 255 * (255 - a)) / 255;
                            g = (g * a + 255 * (255 - a)) / 255;
                            b = (b * a + 255 * (255 - a)) / 255;
                        }
                        gray.Pixels[o + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return gray;
        }

        public Bitmap ToBitmap()
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = data.Stride;
                var buf = new byte[Math.Abs(stride) * Height];
                for (int y = 0; y < Height; y++)
                {
                    int row = y * Math.Abs(stride);
                    for (int x = 0; x < Width; x++)
                    {
                        byte v = Pixels[y * Width + x];
                        int i = row + x * 4;
                        buf[i] = v; buf[i + 1] = v; buf[i + 2] = v; buf[i + 3] = 255;
                    }
                }
                Marshal.Copy(buf, 0, data.Scan0, buf.Length);
            }
            finally { bmp.UnlockBits(data); }
            return bmp;
        }
    }
}

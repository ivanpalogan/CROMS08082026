using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// A black-and-white page: <see cref="Ink"/> is 1 where there is ink, 0 where there is
    /// paper. Held as bytes rather than bits so every later stage can index it directly.
    /// </summary>
    public sealed class BinaryImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Ink;

        public BinaryImage(int width, int height)
        {
            Width = width; Height = height;
            Ink = new byte[width * height];
        }

        public BinaryImage(int width, int height, byte[] ink)
        {
            if (ink.Length != width * height) throw new ArgumentException("Size mismatch.");
            Width = width; Height = height; Ink = ink;
        }

        public bool IsInk(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && Ink[y * Width + x] != 0;
        }

        public BinaryImage Clone()
        {
            return new BinaryImage(Width, Height, (byte[])Ink.Clone());
        }

        public int InkCount()
        {
            int n = 0;
            for (int i = 0; i < Ink.Length; i++) if (Ink[i] != 0) n++;
            return n;
        }

        /// <summary>Ink as black on white, for looking at.</summary>
        public Bitmap ToBitmap()
        {
            var bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, Width, Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                var buf = new byte[stride * Height];
                for (int y = 0; y < Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < Width; x++)
                    {
                        byte v = Ink[y * Width + x] != 0 ? (byte)0 : (byte)255;
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

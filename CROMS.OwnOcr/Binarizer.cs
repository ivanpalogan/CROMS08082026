using System;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Turns a grayscale photograph into ink / paper.
    /// <para/>
    /// A single global threshold fails on photographed certificates: tinted security paper
    /// and uneven lighting make one cut-off erase half the form (measured on the office's
    /// marriage scan, 2026-09-02). So each pixel is compared with the average of ITS OWN
    /// neighbourhood instead (Bradley and Roth, "Adaptive thresholding using the integral
    /// image"): a pixel is ink when it is clearly darker than the paper around it.
    /// </summary>
    public static class Binarizer
    {
        /// <param name="window">Neighbourhood width in pixels; 0 picks one from the image size.</param>
        /// <param name="t">How much darker than the local average counts as ink (0.12 = 12%).</param>
        /// <param name="minContrast">
        /// Also require this many gray levels of difference, otherwise the faint texture of
        /// blank paper (where 12% of a bright average is only a few levels) turns into ink.
        /// </param>
        public static BinaryImage Adaptive(GrayImage g, int window = 0, double t = 0.12, int minContrast = 14)
        {
            int w = g.Width, h = g.Height;
            if (window <= 0) window = AutoWindow(w, h);
            int r = Math.Max(2, window / 2);

            // Integral image: sum[y+1][x+1] = sum of all pixels above and left, so the sum
            // of any rectangle is four lookups no matter how large the window is.
            int iw = w + 1;
            var sum = new long[iw * (h + 1)];
            for (int y = 0; y < h; y++)
            {
                long rowSum = 0;
                int o = y * w, io = (y + 1) * iw;
                for (int x = 0; x < w; x++)
                {
                    rowSum += g.Pixels[o + x];
                    sum[io + x + 1] = sum[io - iw + x + 1] + rowSum;
                }
            }

            var ink = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int y0 = Math.Max(0, y - r), y1 = Math.Min(h - 1, y + r);
                for (int x = 0; x < w; x++)
                {
                    int x0 = Math.Max(0, x - r), x1 = Math.Min(w - 1, x + r);
                    long count = (long)(x1 - x0 + 1) * (y1 - y0 + 1);
                    long s = sum[(y1 + 1) * iw + x1 + 1] - sum[y0 * iw + x1 + 1]
                           - sum[(y1 + 1) * iw + x0] + sum[y0 * iw + x0];
                    double mean = (double)s / count;
                    int p = g.Pixels[y * w + x];
                    if (p < mean * (1.0 - t) && mean - p >= minContrast) ink[y * w + x] = 1;
                }
            }
            return new BinaryImage(w, h, ink);
        }

        /// <summary>
        /// Wide enough to span a stroke plus the paper beside it, narrow enough to follow a
        /// shadow. For a one-line crop that is about twice its height; for a whole page,
        /// about an eighth of its width.
        /// </summary>
        public static int AutoWindow(int w, int h)
        {
            int win = Math.Min(w / 8, h * 2);
            return Math.Max(15, win) | 1;   // odd, never tiny
        }
    }
}

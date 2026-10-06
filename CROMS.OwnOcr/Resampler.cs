using System;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Bicubic (Catmull-Rom) resizing of a grayscale image, written out rather than taken
    /// from GDI+ so the engine's behaviour does not depend on how a given Windows build
    /// resamples.
    /// <para/>
    /// It exists because the field crops are tiny: glyphs are 9-17 px tall on the office's
    /// scans, and thresholding a glyph that small gives a blocky shape no classifier can
    /// tell from its neighbours. Enlarging the GRAY crop first, then thresholding, lets
    /// the threshold fall along a smooth edge instead of along pixel steps.
    /// </summary>
    public static class Resampler
    {
        public static GrayImage Bicubic(GrayImage src, int newWidth, int newHeight)
        {
            if (newWidth <= 0 || newHeight <= 0) throw new ArgumentException("Empty target size.");
            int sw = src.Width, sh = src.Height;
            if (newWidth == sw && newHeight == sh) return src;

            // Separable: stretch along x into a float buffer, then along y.
            var tmp = new float[newWidth * sh];
            var wx = new double[4];
            for (int x = 0; x < newWidth; x++)
            {
                double sx = (x + 0.5) * sw / newWidth - 0.5;
                int ix = (int)Math.Floor(sx);
                double fx = sx - ix;
                for (int k = -1; k <= 2; k++) wx[k + 1] = Kernel(k - fx);
                for (int y = 0; y < sh; y++)
                {
                    double acc = 0;
                    int row = y * sw;
                    for (int k = -1; k <= 2; k++)
                    {
                        int xi = ix + k;
                        if (xi < 0) xi = 0; else if (xi >= sw) xi = sw - 1;
                        acc += src.Pixels[row + xi] * wx[k + 1];
                    }
                    tmp[y * newWidth + x] = (float)acc;
                }
            }

            var dst = new GrayImage(newWidth, newHeight);
            var wy = new double[4];
            for (int y = 0; y < newHeight; y++)
            {
                double sy = (y + 0.5) * sh / newHeight - 0.5;
                int iy = (int)Math.Floor(sy);
                double fy = sy - iy;
                for (int k = -1; k <= 2; k++) wy[k + 1] = Kernel(k - fy);
                for (int x = 0; x < newWidth; x++)
                {
                    double acc = 0;
                    for (int k = -1; k <= 2; k++)
                    {
                        int yi = iy + k;
                        if (yi < 0) yi = 0; else if (yi >= sh) yi = sh - 1;
                        acc += tmp[yi * newWidth + x] * wy[k + 1];
                    }
                    int v = (int)Math.Round(acc);
                    dst.Pixels[y * newWidth + x] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
                }
            }
            return dst;
        }

        // Catmull-Rom spline (a = -0.5): sharper than bilinear, and unlike plain cubic with
        // a = -1 it does not ring visibly around a high-contrast edge such as ink on paper.
        private static double Kernel(double t)
        {
            t = Math.Abs(t);
            if (t <= 1) return 1.5 * t * t * t - 2.5 * t * t + 1;
            if (t < 2) return -0.5 * t * t * t + 2.5 * t * t - 4 * t + 2;
            return 0;
        }
    }
}

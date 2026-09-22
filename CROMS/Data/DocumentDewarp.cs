using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CROMS.Data
{
    /// <summary>
    /// Straightens a PHOTOGRAPHED certificate whose page is not square to the camera — the
    /// case DocLayouts.PageFit refuses outright (2026-09-06: no single scale+offset can
    /// place every anchor on a perspective-skewed shot, so the scan falls through to the
    /// label-only path and reads almost nothing). Detects the document's four corners from
    /// a brightness mask and resamples the page through a homography, the same two-step
    /// approach already proven and shipped on the mobile scanner
    /// (ORCMobile_Application/src/app/services/document-scan.service.ts, 2026-07-29) — ported
    /// here because the desktop pipeline never received it.
    /// <para/>
    /// ONLY EVER CALLED AS A FALLBACK. <see cref="DocumentAI.Analyze"/> runs the ordinary
    /// pipeline first and only reaches this when it produced nothing usable (the template
    /// was rejected). A wrong or wasted corner guess here can only cost a little time, never
    /// regress an already-working scan — the caller keeps whichever attempt actually read
    /// more fields.
    /// </summary>
    internal static class DocumentDewarp
    {
        private struct Quad
        {
            public PointF TL, TR, BR, BL;
        }

        /// <summary>
        /// Detect the page and return it straightened, or null when no confident
        /// quadrilateral was found — a full-frame scan with no background to separate
        /// against, or a shape too degenerate to be a document. Never throws.
        /// </summary>
        public static Bitmap TryStraighten(Bitmap src)
        {
            try
            {
                if (src == null || src.Width < 40 || src.Height < 40) return null;
                Quad? q = DetectQuad(src);
                return q == null ? null : Warp(src, q.Value);
            }
            catch { return null; }
        }

        // ---- corner detection -------------------------------------------------

        private static Quad? DetectQuad(Bitmap src)
        {
            int w = src.Width, h = src.Height;
            int longest = Math.Max(w, h);
            double scale = longest > 520 ? 520.0 / longest : 1.0;
            int sw = Math.Max(8, (int)Math.Round(w * scale));
            int sh = Math.Max(8, (int)Math.Round(h * scale));

            byte[] gray;
            using (var small = new Bitmap(sw, sh, PixelFormat.Format24bppRgb))
            {
                using (Graphics g = Graphics.FromImage(small))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                    g.DrawImage(src, 0, 0, sw, sh);
                }
                gray = ToGray(small);
            }

            int threshold = OtsuThreshold(gray);

            // Robust bbox first (1st/99th percentile of bright-pixel coordinates), so a few
            // stray bright specks off in a corner cannot drag a corner point onto them —
            // the same guard the mobile analyser uses before taking extremes.
            var xs = new System.Collections.Generic.List<int>();
            var ys = new System.Collections.Generic.List<int>();
            int brightCount = 0;
            for (int y = 0; y < sh; y++)
            {
                int row = y * sw;
                for (int x = 0; x < sw; x++)
                {
                    if (gray[row + x] > threshold)
                    {
                        xs.Add(x); ys.Add(y); brightCount++;
                    }
                }
            }

            int total = sw * sh;
            if (brightCount < total * 0.12 || brightCount > total * 0.94) return null; // no separable page

            xs.Sort(); ys.Sort();
            int loX = Percentile(xs, 0.01), hiX = Percentile(xs, 0.99);
            int loY = Percentile(ys, 0.01), hiY = Percentile(ys, 0.99);
            if (hiX - loX < sw * 0.25 || hiY - loY < sh * 0.25) return null;

            // Within that bbox, the document's corners are the extremes of x+y (top-left,
            // bottom-right) and x-y (top-right, bottom-left) — works for a rotated or
            // perspective-skewed rectangle, not just an axis-aligned one.
            int bestTL = int.MaxValue, bestBR = int.MinValue, bestTR = int.MinValue, bestBL = int.MaxValue;
            Point tl = default, tr = default, br = default, bl = default;
            bool any = false;

            for (int y = loY; y <= hiY; y++)
            {
                int row = y * sw;
                for (int x = loX; x <= hiX; x++)
                {
                    if (gray[row + x] <= threshold) continue;
                    any = true;
                    int s = x + y, d = x - y;
                    if (s < bestTL) { bestTL = s; tl = new Point(x, y); }
                    if (s > bestBR) { bestBR = s; br = new Point(x, y); }
                    if (d > bestTR) { bestTR = d; tr = new Point(x, y); }
                    if (d < bestBL) { bestBL = d; bl = new Point(x, y); }
                }
            }
            if (!any) return null;

            double area = Math.Abs(ShoelaceArea(tl, tr, br, bl));
            if (area < total * 0.15) return null; // too small/degenerate to trust

            double inv = 1.0 / scale;
            return new Quad
            {
                TL = new PointF((float)(tl.X * inv), (float)(tl.Y * inv)),
                TR = new PointF((float)(tr.X * inv), (float)(tr.Y * inv)),
                BR = new PointF((float)(br.X * inv), (float)(br.Y * inv)),
                BL = new PointF((float)(bl.X * inv), (float)(bl.Y * inv))
            };
        }

        private static int Percentile(System.Collections.Generic.List<int> sorted, double p)
        {
            if (sorted.Count == 0) return 0;
            int i = (int)(p * (sorted.Count - 1));
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, i))];
        }

        private static double ShoelaceArea(Point a, Point b, Point c, Point d)
        {
            double sum = a.X * b.Y - b.X * a.Y
                       + b.X * c.Y - c.X * b.Y
                       + c.X * d.Y - d.X * c.Y
                       + d.X * a.Y - a.X * d.Y;
            return sum / 2.0;
        }

        private static byte[] ToGray(Bitmap rgb)
        {
            int w = rgb.Width, h = rgb.Height;
            BitmapData bd = rgb.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = bd.Stride;
                var buf = new byte[stride * h];
                Marshal.Copy(bd.Scan0, buf, 0, buf.Length);
                var gray = new byte[w * h];
                for (int y = 0; y < h; y++)
                {
                    int row = y * stride, outRow = y * w;
                    for (int x = 0; x < w; x++)
                    {
                        int p = row + x * 3;
                        gray[outRow + x] = (byte)(buf[p + 2] * 0.299 + buf[p + 1] * 0.587 + buf[p] * 0.114);
                    }
                }
                return gray;
            }
            finally { rgb.UnlockBits(bd); }
        }

        /// <summary>Otsu's method: the threshold that best separates the histogram into two classes.</summary>
        private static int OtsuThreshold(byte[] gray)
        {
            var hist = new int[256];
            for (int i = 0; i < gray.Length; i++) hist[gray[i]]++;
            int total = gray.Length;

            double sumAll = 0;
            for (int t = 0; t < 256; t++) sumAll += t * hist[t];

            double sumB = 0; int wB = 0;
            double bestVar = -1; int bestT = 127;

            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];
                if (wB == 0) continue;
                int wF = total - wB;
                if (wF == 0) break;

                sumB += t * hist[t];
                double mB = sumB / wB;
                double mF = (sumAll - sumB) / wF;
                double between = (double)wB * wF * (mB - mF) * (mB - mF);
                if (between > bestVar) { bestVar = between; bestT = t; }
            }
            return bestT;
        }

        // ---- perspective warp --------------------------------------------------

        private static Bitmap Warp(Bitmap src, Quad q)
        {
            double topW = Dist(q.TL, q.TR), botW = Dist(q.BL, q.BR);
            double leftH = Dist(q.TL, q.BL), rightH = Dist(q.TR, q.BR);
            double outW = Math.Max(topW, botW), outH = Math.Max(leftH, rightH);
            if (outW < 40 || outH < 40) return null;

            const int MaxLong = 2600;
            double longest = Math.Max(outW, outH);
            if (longest > MaxLong)
            {
                double f = MaxLong / longest;
                outW *= f; outH *= f;
            }
            int W = Math.Max(40, (int)Math.Round(outW));
            int H = Math.Max(40, (int)Math.Round(outH));

            // Homography mapping the flat OUTPUT rectangle's corners onto the SOURCE quad —
            // so sampling forward from (u,v) lands exactly where that value sits on the
            // original photo. Solved by the standard 4-point DLT: 8 unknowns (h33 fixed to
            // 1), 2 equations per correspondence.
            double[] hMat = SolveHomography(
                new PointF(0, 0), new PointF(W, 0), new PointF(W, H), new PointF(0, H),
                q.TL, q.TR, q.BR, q.BL);
            if (hMat == null) return null;

            var outBmp = new Bitmap(W, H, PixelFormat.Format24bppRgb);
            BitmapData srcData = null, outData = null;
            try
            {
                srcData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                outData = outBmp.LockBits(new Rectangle(0, 0, W, H),
                    ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                int sStride = srcData.Stride, oStride = outData.Stride;
                var sBuf = new byte[sStride * src.Height];
                Marshal.Copy(srcData.Scan0, sBuf, 0, sBuf.Length);
                var oBuf = new byte[oStride * H];

                double h11 = hMat[0], h12 = hMat[1], h13 = hMat[2];
                double h21 = hMat[3], h22 = hMat[4], h23 = hMat[5];
                double h31 = hMat[6], h32 = hMat[7];

                for (int v = 0; v < H; v++)
                {
                    int oRow = v * oStride;
                    for (int u = 0; u < W; u++)
                    {
                        double denom = h31 * u + h32 * v + 1.0;
                        if (Math.Abs(denom) < 1e-9) continue;
                        double sx = (h11 * u + h12 * v + h13) / denom;
                        double sy = (h21 * u + h22 * v + h23) / denom;

                        BilinearSample(sBuf, sStride, src.Width, src.Height, sx, sy,
                            out byte bB, out byte bG, out byte bR);

                        int p = oRow + u * 3;
                        oBuf[p] = bB; oBuf[p + 1] = bG; oBuf[p + 2] = bR;
                    }
                }
                Marshal.Copy(oBuf, 0, outData.Scan0, oBuf.Length);
            }
            finally
            {
                if (srcData != null) src.UnlockBits(srcData);
                if (outData != null) outBmp.UnlockBits(outData);
            }
            return outBmp;
        }

        private static double Dist(PointF a, PointF b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void BilinearSample(byte[] buf, int stride, int w, int h,
            double x, double y, out byte b, out byte g, out byte r)
        {
            if (x < 0) x = 0; if (x > w - 1.001) x = w - 1.001;
            if (y < 0) y = 0; if (y > h - 1.001) y = h - 1.001;

            int x0 = (int)x, y0 = (int)y;
            int x1 = Math.Min(w - 1, x0 + 1), y1 = Math.Min(h - 1, y0 + 1);
            double fx = x - x0, fy = y - y0;

            int p00 = y0 * stride + x0 * 3, p10 = y0 * stride + x1 * 3;
            int p01 = y1 * stride + x0 * 3, p11 = y1 * stride + x1 * 3;

            b = Lerp2(buf[p00], buf[p10], buf[p01], buf[p11], fx, fy);
            g = Lerp2(buf[p00 + 1], buf[p10 + 1], buf[p01 + 1], buf[p11 + 1], fx, fy);
            r = Lerp2(buf[p00 + 2], buf[p10 + 2], buf[p01 + 2], buf[p11 + 2], fx, fy);
        }

        private static byte Lerp2(byte v00, byte v10, byte v01, byte v11, double fx, double fy)
        {
            double top = v00 + (v10 - v00) * fx;
            double bot = v01 + (v11 - v01) * fx;
            double v = top + (bot - top) * fy;
            return (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
        }

        /// <summary>
        /// Direct linear transform for a homography from 4 point correspondences. Builds the
        /// 8x8 system for [h11,h12,h13,h21,h22,h23,h31,h32] (h33 = 1) and solves it by
        /// Gaussian elimination with partial pivoting. Returns null when the system is
        /// singular (degenerate/collinear quad) rather than dividing by ~zero.
        /// </summary>
        private static double[] SolveHomography(
            PointF u0, PointF u1, PointF u2, PointF u3,
            PointF x0, PointF x1, PointF x2, PointF x3)
        {
            var us = new[] { u0, u1, u2, u3 };
            var xs = new[] { x0, x1, x2, x3 };

            var a = new double[8, 9];
            for (int i = 0; i < 4; i++)
            {
                double u = us[i].X, v = us[i].Y, x = xs[i].X, y = xs[i].Y;
                int r0 = i * 2, r1 = i * 2 + 1;

                a[r0, 0] = u; a[r0, 1] = v; a[r0, 2] = 1; a[r0, 3] = 0; a[r0, 4] = 0; a[r0, 5] = 0;
                a[r0, 6] = -u * x; a[r0, 7] = -v * x; a[r0, 8] = x;

                a[r1, 0] = 0; a[r1, 1] = 0; a[r1, 2] = 0; a[r1, 3] = u; a[r1, 4] = v; a[r1, 5] = 1;
                a[r1, 6] = -u * y; a[r1, 7] = -v * y; a[r1, 8] = y;
            }

            return GaussianSolve(a, 8);
        }

        private static double[] GaussianSolve(double[,] a, int n)
        {
            for (int col = 0; col < n; col++)
            {
                int pivot = col;
                double best = Math.Abs(a[col, col]);
                for (int r = col + 1; r < n; r++)
                {
                    double v = Math.Abs(a[r, col]);
                    if (v > best) { best = v; pivot = r; }
                }
                if (best < 1e-12) return null; // singular

                if (pivot != col)
                    for (int c = col; c <= n; c++)
                    { double t = a[col, c]; a[col, c] = a[pivot, c]; a[pivot, c] = t; }

                double pv = a[col, col];
                for (int c = col; c <= n; c++) a[col, c] /= pv;

                for (int r = 0; r < n; r++)
                {
                    if (r == col) continue;
                    double f = a[r, col];
                    if (f == 0) continue;
                    for (int c = col; c <= n; c++) a[r, c] -= f * a[col, c];
                }
            }

            var result = new double[n];
            for (int i = 0; i < n; i++) result[i] = a[i, n];
            return result;
        }
    }
}

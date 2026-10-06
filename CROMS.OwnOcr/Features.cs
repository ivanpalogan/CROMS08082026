using System;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Turns a normalised 20x20 character picture (plus where it sat on its line) into the
    /// vector the classifier compares.
    /// <para/>
    /// Three kinds of evidence, each normalised on its own so none can drown the others:
    ///  - ZONING: the picture averaged down to 10x10 - where the ink is, coarsely. Tolerant
    ///    of a stroke being a pixel thicker or shifted, which is what blur and bloom do.
    ///  - GRADIENTS: a histogram of stroke directions (horizontal, vertical, two diagonals...)
    ///    in a 4x4 grid. Two letters can put ink in the same places and still differ in how
    ///    the strokes run (an O against a D, a 5 against an S); directions tell them apart.
    ///  - GEOMETRY: width, and where the box sat against the line. A full stop, a comma and a
    ///    hyphen are nearly the same few pixels; their height on the line is what separates them.
    /// </summary>
    public static class Features
    {
        public const int ZoneDim = 100;    // 10 x 10
        public const int HogDim = 128;     // 4 x 4 cells x 8 directions
        public const int GeomDim = 5;
        public const int Dim = ZoneDim + HogDim + GeomDim;

        // Relative weight of each block in the distance between two characters. Tuned on Day 4.
        public static float ZoneWeight = 1.0f;
        public static float HogWeight = 1.0f;
        public static float GeomWeight = 1.0f;

        public static float[] FromCell(GlyphCell c)
        {
            return Extract(c.Image, c.RelW, c.RelTop, c.RelBottom, c.Aspect);
        }

        public static float[] Extract(float[] img, float relW, float relTop, float relBottom, float aspect)
        {
            const int N = GlyphCell.Size;
            var v = new float[Dim];

            // ---- zoning: 2x2 average -> 10x10, then unit length ----------------------------
            double zn = 0;
            for (int zy = 0; zy < N / 2; zy++)
                for (int zx = 0; zx < N / 2; zx++)
                {
                    float s = img[(zy * 2) * N + zx * 2] + img[(zy * 2) * N + zx * 2 + 1]
                            + img[(zy * 2 + 1) * N + zx * 2] + img[(zy * 2 + 1) * N + zx * 2 + 1];
                    s *= 0.25f;
                    v[zy * (N / 2) + zx] = s;
                    zn += s * s;
                }
            Scale(v, 0, ZoneDim, zn, ZoneWeight);

            // ---- gradient histogram: 4x4 cells, 8 unsigned directions ------------------------
            double hn = 0;
            const int cell = N / 4;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float gx = Px(img, x + 1, y) - Px(img, x - 1, y);
                    float gy = Px(img, x, y + 1) - Px(img, x, y - 1);
                    float mag = (float)Math.Sqrt(gx * gx + gy * gy);
                    if (mag < 1e-4f) continue;
                    double ang = Math.Atan2(gy, gx);          // -pi..pi
                    if (ang < 0) ang += Math.PI;              // unsigned: a stroke has no "direction"
                    if (ang >= Math.PI) ang -= Math.PI;
                    int bin = Math.Min(7, (int)(ang / Math.PI * 8));
                    v[ZoneDim + ((y / cell) * 4 + (x / cell)) * 8 + bin] += mag;
                }
            }
            for (int i = ZoneDim; i < ZoneDim + HogDim; i++) hn += v[i] * v[i];
            Scale(v, ZoneDim, HogDim, hn, HogWeight);

            // ---- geometry --------------------------------------------------------------------
            double ink = 0;
            for (int i = 0; i < img.Length; i++) ink += img[i];
            int g = ZoneDim + HogDim;
            v[g + 0] = Clamp(relW, 0f, 2f) * GeomWeight;
            v[g + 1] = Clamp(relTop, -1f, 1.5f) * GeomWeight;
            v[g + 2] = Clamp(relBottom, -1f, 1f) * GeomWeight;
            v[g + 3] = Clamp(aspect, 0f, 3f) * 0.5f * GeomWeight;
            v[g + 4] = (float)(ink / img.Length) * 2f * GeomWeight;
            return v;
        }

        private static float Px(float[] img, int x, int y)
        {
            const int N = GlyphCell.Size;
            if (x < 0) x = 0; else if (x >= N) x = N - 1;
            if (y < 0) y = 0; else if (y >= N) y = N - 1;
            return img[y * N + x];
        }

        private static void Scale(float[] v, int from, int len, double sumSq, float weight)
        {
            if (sumSq <= 1e-12) return;
            float k = weight / (float)Math.Sqrt(sumSq);
            for (int i = from; i < from + len; i++) v[i] *= k;
        }

        private static float Clamp(float x, float lo, float hi)
        {
            return x < lo ? lo : x > hi ? hi : x;
        }
    }
}

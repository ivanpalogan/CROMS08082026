using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// Everything the engine has worked out about one image so far. Each stage's output is
    /// kept, not just the last one, because the debug picture and the benchmark both need
    /// to show where a bad reading went wrong: was it the threshold, the rule removal, or
    /// the character split?
    /// </summary>
    public sealed class Analysis
    {
        public GrayImage Gray;
        public BinaryImage Ink;              // after thresholding
        public RuleResult Rules;             // printed table lines found
        public BinaryImage Cleaned;          // ink with the rules taken out
        public Labeling Blobs;               // connected pieces of the cleaned ink

        /// <summary>Pieces big enough to be part of a character (everything that is not speckle).</summary>
        public List<Blob> Glyphs = new List<Blob>();
        /// <summary>Specks too small to be a letter or a punctuation mark.</summary>
        public List<Blob> Noise = new List<Blob>();
        /// <summary>
        /// Pieces much wider or taller than a typical character: two letters that touch, or a
        /// letter still joined to something it should not be. These are what the character
        /// splitter has to work on.
        /// </summary>
        public List<Blob> Suspicious = new List<Blob>();

        /// <summary>The typical character height in pixels (0 when nothing was found).</summary>
        public double MedianHeight;

        /// <summary>How much the crop was enlarged before thresholding (1 = not at all).</summary>
        public double Scale = 1.0;
        /// <summary>The line of text chosen as the value (null when nothing usable was found).</summary>
        public LineInfo Line;
        /// <summary>Candidate characters of that line, left to right, each normalised for the classifier.</summary>
        public List<GlyphCell> Cells = new List<GlyphCell>();
    }

    /// <summary>
    /// The engine. Day 1 covers the front half of the pipeline:
    /// grayscale, adaptive threshold, ruled-line removal and connected components.
    /// </summary>
    public static class OwnOcrEngine
    {
        /// <summary>
        /// Height, in pixels, that a typical character is enlarged to before it is read. The
        /// office's crops carry glyphs only 9-17 px tall; at that size one pixel of
        /// threshold noise changes a letter's shape, at about 40 px it does not.
        /// </summary>
        public const double TargetGlyphHeight = 40.0;
        public const double MaxScale = 5.0;

        public static Analysis Analyze(Bitmap bmp)
        {
            GrayImage gray = GrayImage.FromBitmap(bmp);

            // First look at the native size, only to learn how big the characters are.
            double scale = 1.0;
            {
                BinaryImage ink0 = Binarizer.Adaptive(gray);
                RuleResult rules0 = LineRemover.Remove(ink0);
                Labeling lab0 = ConnectedComponents.Label(rules0.Cleaned.Ink, ink0.Width, ink0.Height);
                double h0 = TypicalHeight(lab0);
                if (h0 > 0 && h0 < TargetGlyphHeight * 0.8)
                    scale = Math.Min(MaxScale, TargetGlyphHeight / h0);
            }

            // Enlarge the GRAY crop (smoothly) and only then threshold, so the threshold
            // follows a smooth edge instead of the staircase of the original pixels.
            if (scale > 1.15)
                gray = Resampler.Bicubic(gray, (int)Math.Round(gray.Width * scale),
                                               (int)Math.Round(gray.Height * scale));
            else
                scale = 1.0;

            var a = new Analysis { Scale = scale, Gray = gray };
            a.Ink = Binarizer.Adaptive(a.Gray);
            a.Rules = LineRemover.Remove(a.Ink);
            a.Cleaned = a.Rules.Cleaned;
            a.Blobs = ConnectedComponents.Label(a.Cleaned.Ink, a.Cleaned.Width, a.Cleaned.Height);
            Classify(a);
            Segmenter.Segment(a);
            return a;
        }

        // Median height of the pieces that can plausibly be letters.
        private static double TypicalHeight(Labeling lab)
        {
            var hs = lab.Blobs.Where(b => b.Height >= 6 && b.Area >= 10)
                              .Select(b => b.Height).OrderBy(v => v).ToList();
            return hs.Count == 0 ? 0 : hs[hs.Count / 2];
        }

        private static void Classify(Analysis a)
        {
            // Speckle first: a handful of pixels is paper grain or a fleck of the rule that
            // survived, never a character. (A full stop is bigger than this at any scale the
            // pages are read at.)
            var real = new List<Blob>();
            foreach (Blob b in a.Blobs.Blobs)
            {
                if (b.Area < 6 || (b.Width < 3 && b.Height < 3)) a.Noise.Add(b);
                else real.Add(b);
            }

            // The typical character height is the median over pieces tall enough to be a
            // letter. Heights of full stops and commas would drag it down, so they are left
            // out of the sample (but kept as glyphs: punctuation is part of a value).
            var heights = real.Where(b => b.Height >= 8 && b.Area >= 12)
                              .Select(b => b.Height).OrderBy(v => v).ToList();
            a.MedianHeight = heights.Count == 0 ? 0 : heights[heights.Count / 2];

            foreach (Blob b in real)
            {
                a.Glyphs.Add(b);
                if (a.MedianHeight > 0 &&
                    (b.Width > a.MedianHeight * 1.8 || b.Height > a.MedianHeight * 1.8))
                    a.Suspicious.Add(b);
            }
        }
    }
}

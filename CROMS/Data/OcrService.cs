using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;

namespace CROMS.Data
{
    /// <summary>Result of an OCR pass: extracted text and mean confidence (0-100).</summary>
    public class OcrResult
    {
        public string Text { get; set; }
        public int Confidence { get; set; }
    }

    /// <summary>
    /// Tesseract OCR wrapper. Based on the team's old system (grayscale + contrast +
    /// binarize + upscale before recognition), improved to also return the engine's
    /// mean confidence and to fall back to a bundled tessdata folder if the
    /// Tesseract-OCR install isn't present. Native libs (x86/x64) are copied next to
    /// the exe by the project; language data is "eng".
    /// </summary>
    public static class OcrService
    {
        /// <summary>True if the OCR engine's language data can be located.</summary>
        public static bool IsAvailable()
        {
            return File.Exists(Path.Combine(TessDataPath(), "eng.traineddata"));
        }

        private static string TessDataPath()
        {
            const string installed = @"C:\Program Files\Tesseract-OCR\tessdata";
            if (File.Exists(Path.Combine(installed, "eng.traineddata")))
                return installed;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public static OcrResult Run(Bitmap source)
        {
            using (Bitmap prepared = Preprocess(source))
            using (var engine = new TesseractEngine(TessDataPath(), "eng", EngineMode.Default))
            {
                engine.SetVariable("user_defined_dpi", "300");
                using (var ms = new MemoryStream())
                {
                    prepared.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                    using (var page = engine.Process(pix, PageSegMode.Auto))
                    {
                        string text = page.GetText();
                        return new OcrResult
                        {
                            Text = string.IsNullOrEmpty(text) ? "" : text.Trim(),
                            Confidence = (int)Math.Round(page.GetMeanConfidence() * 100)
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Upscale (bicubic) + grayscale + contrast stretch + binarize. Faded old
        /// registry-book pages read much better after this.
        /// </summary>
        private static Bitmap Preprocess(Bitmap src)
        {
            const int scale = 2;
            var big = new Bitmap(src.Width * scale, src.Height * scale, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(big))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, big.Width, big.Height);
            }

            for (int y = 0; y < big.Height; y++)
            {
                for (int x = 0; x < big.Width; x++)
                {
                    Color p = big.GetPixel(x, y);
                    int gray = (p.R + p.G + p.B) / 3;
                    gray = (int)((gray - 128) * 1.5 + 128);
                    if (gray < 0) gray = 0;
                    if (gray > 255) gray = 255;
                    big.SetPixel(x, y, gray < 150 ? Color.Black : Color.White);
                }
            }
            return big;
        }
    }
}

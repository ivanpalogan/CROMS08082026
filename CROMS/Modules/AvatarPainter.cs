using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace CROMS.Modules
{
    /// <summary>
    /// Draws a circular user avatar: the person's uploaded profile photo (cover-fit, cropped
    /// to the circle) when one is on file, otherwise their initials on a navy circle. Shared
    /// by every place a user's avatar appears — the header chip, My Profile, Edit Profile —
    /// so a photo (or its absence) looks identical everywhere.
    /// </summary>
    public static class AvatarPainter
    {
        public static void Draw(Graphics g, Rectangle rect, byte[] photoBytes, string fullName)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (photoBytes != null && photoBytes.Length > 0 && TryDrawPhoto(g, rect, photoBytes))
                return;

            using (var navy = new SolidBrush(UiTheme.Navy))
                g.FillEllipse(navy, rect);

            string initials = Initials(fullName);
            if (initials.Length == 0) return;

            using (var font = new Font("Segoe UI", rect.Width * 0.34F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.White))
            {
                var sz = g.MeasureString(initials, font);
                var pt = new PointF(rect.X + (rect.Width - sz.Width) / 2f, rect.Y + (rect.Height - sz.Height) / 2f);
                g.DrawString(initials, font, textBrush, pt);
            }
        }

        private static bool TryDrawPhoto(Graphics g, Rectangle rect, byte[] photoBytes)
        {
            try
            {
                using (var ms = new MemoryStream(photoBytes))
                using (var img = Image.FromStream(ms))
                using (var clip = new GraphicsPath())
                {
                    clip.AddEllipse(rect);
                    Region oldClip = g.Clip;
                    g.SetClip(clip, CombineMode.Intersect);

                    float scale = Math.Max((float)rect.Width / img.Width, (float)rect.Height / img.Height);
                    float w = img.Width * scale, h = img.Height * scale;
                    float x = rect.X + (rect.Width - w) / 2f;
                    float y = rect.Y + (rect.Height - h) / 2f;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(img, x, y, w, h);

                    g.SetClip(oldClip, CombineMode.Replace);
                    return true;
                }
            }
            catch
            {
                return false;   // corrupt/undecodable bytes — fall back to initials
            }
        }

        private static string Initials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }
    }
}

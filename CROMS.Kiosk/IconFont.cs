using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Loads the embedded Phosphor Light icon font (MIT, phosphor-icons/web -- see
    /// Resources/Phosphor-LICENSE.txt) and draws single glyphs from it. Phosphor Light was
    /// chosen over Material Icons Outlined because its stroke weight matches the approved
    /// mockup's thin outline style; Material Outlined ships only one (heavier) static weight.
    /// Loaded from memory (PrivateFontCollection) so it never needs installing on the kiosk PC
    /// and works fully offline. <see cref="IsAvailable"/> is false if the embedded resource is
    /// ever missing (e.g. a stripped build) -- callers should fall back to their own drawing.
    /// </summary>
    public static class IconFont
    {
        private static readonly PrivateFontCollection _fonts = new PrivateFontCollection();
        private static FontFamily _family;

        public static bool IsAvailable => _family != null;

        static IconFont()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string resName = Array.Find(asm.GetManifestResourceNames(),
                    n => n.EndsWith("Phosphor-Light.ttf", StringComparison.OrdinalIgnoreCase));
                if (resName == null) return;

                using (Stream s = asm.GetManifestResourceStream(resName))
                {
                    if (s == null) return;
                    byte[] data = new byte[s.Length];
                    s.Read(data, 0, data.Length);

                    // AddMemoryFont needs an unmanaged buffer that outlives the font -- intentionally
                    // never freed (the font must stay loaded for the app's whole lifetime).
                    IntPtr mem = Marshal.AllocCoTaskMem(data.Length);
                    Marshal.Copy(data, 0, mem, data.Length);
                    _fonts.AddMemoryFont(mem, data.Length);
                    if (_fonts.Families.Length > 0) _family = _fonts.Families[0];
                }
            }
            catch { _family = null; }
        }

        /// <summary>
        /// Draws one glyph (a single-character codepoint string) centered in the given rect.
        /// Uses GDI+'s Graphics.DrawString, NOT TextRenderer -- GDI (which TextRenderer wraps)
        /// fails to resolve this font's Private-Use-Area glyphs and silently renders the
        /// ".notdef" placeholder box instead; GDI+ renders it correctly. Confirmed by rendering
        /// both paths to a bitmap and comparing pixel output.
        /// </summary>
        public static void Draw(Graphics g, RectangleF rect, string glyph, Color color, float sizePx)
        {
            if (!IsAvailable) return;
            TextRenderingHint prevHint = g.TextRenderingHint;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            using (var f = new Font(_family, sizePx, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color))
            using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(glyph, f, brush, rect, fmt);
            }
            g.TextRenderingHint = prevHint;
        }
    }
}

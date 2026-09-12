using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Smooth hover-state animation for owner-drawn controls — same helper as the main CROMS
    /// app's Modules/HoverFade.cs, copied here because CROMS.Kiosk is a separate deployable
    /// .exe with no project reference to CROMS.exe. <see cref="Attach"/> gives an eased 0..1
    /// progress value that animates toward 1 on MouseEnter and back to 0 on MouseLeave.
    /// </summary>
    public static class HoverFade
    {
        public static void Attach(Control target, int durationMs, Action<float> onProgress)
        {
            float progress = 0f;
            bool hovering = false;
            var timer = new Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                float step = 15f / durationMs;
                progress = hovering ? Math.Min(1f, progress + step) : Math.Max(0f, progress - step);
                onProgress(Ease(progress));
                if ((hovering && progress >= 1f) || (!hovering && progress <= 0f)) timer.Stop();
            };
            target.MouseEnter += (s, e) => { hovering = true; if (!timer.Enabled) timer.Start(); };
            target.MouseLeave += (s, e) => { hovering = false; if (!timer.Enabled) timer.Start(); };
            target.Disposed += (s, e) => timer.Dispose();
        }

        /// <summary>Ease-out quad — starts fast, settles gently; reads smoother than linear.</summary>
        private static float Ease(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Linear-interpolates between two colors at t in [0,1].</summary>
        public static Color Lerp(Color a, Color b, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return Color.FromArgb(
                a.A + (int)((b.A - a.A) * t),
                a.R + (int)((b.R - a.R) * t),
                a.G + (int)((b.G - a.G) * t),
                a.B + (int)((b.B - a.B) * t));
        }

        /// <summary>Linear-interpolates between two floats at t in [0,1].</summary>
        public static float Lerp(float a, float b, float t)
        {
            t = t < 0f ? 0f : t > 1f ? 1f : t;
            return a + (b - a) * t;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Smooth hover-state animation for owner-drawn controls. <see cref="Attach"/> gives you an
    /// eased 0..1 progress value that animates toward 1 on MouseEnter and back to 0 on
    /// MouseLeave over <c>durationMs</c>, repainting the control each frame — this is the
    /// "modern hover fade" used by the Launcher's option cards. Reusable as-is for MainForm's
    /// sidebar/nav buttons and other owner-drawn cards as they get the same treatment.
    /// </summary>
    public static class HoverFade
    {
        /// <summary>
        /// Wires MouseEnter/MouseLeave on <paramref name="target"/> to an animation timer that
        /// calls <paramref name="onProgress"/> every frame with the eased 0..1 hover amount.
        /// The caller's callback should just Invalidate() the control and read the progress
        /// value from its own Paint handler (see LauncherForm.AddOption for the pattern).
        /// </summary>
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

        /// <summary>
        /// Same eased 0..1 animation as <see cref="Attach"/>, but driven by keyboard FOCUS
        /// (Enter/Leave) instead of the mouse — for the "border lights up while this field is
        /// focused" pattern (rounded field hosts on the Login screen, etc).
        /// </summary>
        public static void AttachFocus(Control focusTarget, int durationMs, Action<float> onProgress)
        {
            float progress = 0f;
            bool active = false;
            var timer = new Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                float step = 15f / durationMs;
                progress = active ? Math.Min(1f, progress + step) : Math.Max(0f, progress - step);
                onProgress(Ease(progress));
                if ((active && progress >= 1f) || (!active && progress <= 0f)) timer.Stop();
            };
            focusTarget.Enter += (s, e) => { active = true; if (!timer.Enabled) timer.Start(); };
            focusTarget.Leave += (s, e) => { active = false; if (!timer.Enabled) timer.Start(); };
            focusTarget.Disposed += (s, e) => timer.Dispose();
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

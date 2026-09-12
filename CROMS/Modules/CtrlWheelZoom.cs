using System;
using System.Drawing;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Ctrl + mouse wheel zoom for a document view, the way every browser and PDF reader does
    /// it - plain wheel still scrolls.
    /// <para/>
    /// A message filter rather than a MouseWheel handler, because the wheel never reaches the
    /// host: the Crystal viewer and PrintPreviewControl hand it to inner child windows that
    /// scroll and swallow it. Filtering at the application level sees it first. The wheel's
    /// lParam is the pointer position in SCREEN coordinates, and only a wheel over the target
    /// is taken, so a second open window is never zoomed by accident.
    /// </summary>
    internal sealed class CtrlWheelZoom : IMessageFilter, IDisposable
    {
        private const int WM_MOUSEWHEEL = 0x020A;
        private readonly Control _target;
        private readonly Action<int> _step;
        private bool _attached;

        /// <param name="step">+1 to zoom in one notch, -1 to zoom out.</param>
        public CtrlWheelZoom(Control target, Action<int> step)
        {
            _target = target;
            _step = step;
            Application.AddMessageFilter(this);
            _attached = true;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL) return false;
            if ((Control.ModifierKeys & Keys.Control) == 0) return false;
            if (_target.IsDisposed || !_target.IsHandleCreated || !_target.Visible) return false;

            long lp = m.LParam.ToInt64();
            var screen = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
            if (!_target.RectangleToScreen(_target.ClientRectangle).Contains(screen)) return false;

            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            if (delta == 0) return false;
            _step(delta > 0 ? 1 : -1);
            return true;   // consumed: the view must not also scroll on a zoom gesture
        }

        public void Dispose()
        {
            if (!_attached) return;
            Application.RemoveMessageFilter(this);
            _attached = false;
        }

        /// <summary>The next zoom level, ~15% per notch, snapped to 5% and kept in range.</summary>
        public static int Next(int current, int direction, int min = 10, int max = 400)
        {
            double v = direction > 0 ? current * 1.15 : current / 1.15;
            int snapped = (int)Math.Round(v / 5.0) * 5;
            if (snapped == current) snapped += direction > 0 ? 5 : -5;
            return Math.Max(min, Math.Min(max, snapped));
        }
    }
}

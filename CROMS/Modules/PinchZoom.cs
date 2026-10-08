using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Two-finger pinch zoom and one/two-finger drag pan for a scroll host, on a touchscreen.
    /// <para/>
    /// Windows delivers touch as WM_GESTURE to the window under the fingers. That message is
    /// SENT, not posted, so an application message filter never sees it - the window procedure
    /// of each control has to be subclassed (NativeWindow), which is what <see cref="Hook"/>
    /// does for every control passed in. A precision touchpad's pinch arrives as Ctrl + wheel
    /// instead and is handled by <see cref="CtrlWheelZoom"/>, not here.
    /// </summary>
    internal sealed class PinchZoom : IDisposable
    {
        private const int WM_GESTURE = 0x0119;
        private const int GID_ZOOM = 3;
        private const int GID_PAN = 4;
        private const int GF_BEGIN = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTS { public short x; public short y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct GESTUREINFO
        {
            public int cbSize;
            public int dwFlags;
            public int dwID;
            public IntPtr hwndTarget;
            public POINTS ptsLocation;
            public int dwInstanceID;
            public int dwSequenceID;
            public long ullArguments;
            public int cbExtraArgs;
        }

        [DllImport("user32.dll")]
        private static extern bool GetGestureInfo(IntPtr hGestureInfo, ref GESTUREINFO pGestureInfo);

        [DllImport("user32.dll")]
        private static extern bool CloseGestureInfoHandle(IntPtr hGestureInfo);

        private readonly Action<float, Point> _zoomAt;   // factor, centre in screen coordinates
        private readonly Action<int, int> _panBy;        // dx, dy in pixels (content follows the fingers)
        private readonly Hook[] _hooks;
        private long _lastDistance;
        private Point _lastPan;

        public PinchZoom(Action<float, Point> zoomAt, Action<int, int> panBy, params Control[] targets)
        {
            _zoomAt = zoomAt;
            _panBy = panBy;
            _hooks = new Hook[targets.Length];
            for (int i = 0; i < targets.Length; i++) _hooks[i] = new Hook(this, targets[i]);
        }

        public void Dispose()
        {
            foreach (var h in _hooks) h.Detach();
        }

        private bool OnGesture(ref Message m)
        {
            var gi = new GESTUREINFO { cbSize = Marshal.SizeOf(typeof(GESTUREINFO)) };
            if (!GetGestureInfo(m.LParam, ref gi)) return false;

            bool begin = (gi.dwFlags & GF_BEGIN) != 0;
            bool handled = false;
            var at = new Point(gi.ptsLocation.x, gi.ptsLocation.y);

            if (gi.dwID == GID_ZOOM)
            {
                // ullArguments is the distance between the two fingers; the ratio of
                // consecutive distances is the zoom step.
                if (begin) _lastDistance = gi.ullArguments;
                else if (_lastDistance > 0 && gi.ullArguments > 0)
                {
                    float factor = (float)gi.ullArguments / _lastDistance;
                    _lastDistance = gi.ullArguments;
                    if (Math.Abs(factor - 1f) > 0.001f) _zoomAt(factor, at);
                }
                handled = true;
            }
            else if (gi.dwID == GID_PAN)
            {
                if (begin) _lastPan = at;
                else
                {
                    int dx = at.X - _lastPan.X, dy = at.Y - _lastPan.Y;
                    _lastPan = at;
                    if (dx != 0 || dy != 0) _panBy(dx, dy);
                }
                handled = true;
            }

            if (handled) CloseGestureInfoHandle(m.LParam);   // we own the handle once we take the message
            return handled;
        }

        /// <summary>Subclasses one control's window procedure, surviving handle re-creation.</summary>
        private sealed class Hook : NativeWindow
        {
            private readonly PinchZoom _owner;
            private readonly Control _c;

            public Hook(PinchZoom owner, Control c)
            {
                _owner = owner;
                _c = c;
                c.HandleCreated += OnCreated;
                c.HandleDestroyed += OnDestroyed;
                if (c.IsHandleCreated) AssignHandle(c.Handle);
            }

            private void OnCreated(object s, EventArgs e) { if (Handle == IntPtr.Zero) AssignHandle(_c.Handle); }
            private void OnDestroyed(object s, EventArgs e) { if (Handle != IntPtr.Zero) ReleaseHandle(); }

            public void Detach()
            {
                _c.HandleCreated -= OnCreated;
                _c.HandleDestroyed -= OnDestroyed;
                if (Handle != IntPtr.Zero) ReleaseHandle();
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_GESTURE && _owner.OnGesture(ref m))
                {
                    m.Result = IntPtr.Zero;
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}

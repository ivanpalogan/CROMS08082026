using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;

namespace CROMS.Modules
{
    /// <summary>
    /// The WYSIWYG page surface of the certificate Template Designer. Draws the page as
    /// actual paper (white rectangle at <see cref="Zoom"/> pixels per point) and lets the
    /// operator select, drag and resize elements directly on it — no coordinates typed
    /// unless they choose to, via the properties panel this control does not own.
    /// <para/>
    /// Purely a canvas: it knows how to show and manipulate a <see cref="CertTemplate"/>,
    /// nothing about saving, undo history or the toolbox. The host form wires those up
    /// through this control's events.
    /// </summary>
    public class TemplateCanvas : Panel
    {
        public CertTemplate Template { get; set; }
        public IDictionary<string, string> Values { get; set; }
        public Func<int, Image> LoadTemplateImage { get; set; }
        public bool ReadOnly { get; set; }

        private float _zoom = 1.25f;
        public float Zoom
        {
            get => _zoom;
            set { _zoom = value; UpdateCanvasSize(); Invalidate(); }
        }

        private string _selectedId;
        public string SelectedId
        {
            get => _selectedId;
            set { _selectedId = value; Invalidate(); SelectionChanged?.Invoke(this, EventArgs.Empty); }
        }

        public TemplateElement Selected =>
            Template?.Elements.FirstOrDefault(e => e.Id == _selectedId);

        /// <summary>Fired once, right before a drag/nudge/delete begins mutating the
        /// template, so the host can capture an undo snapshot of the state as it was.</summary>
        public event EventHandler BeforeMutate;
        public event EventHandler SelectionChanged;
        /// <summary>Fired after a move/resize/nudge/delete completes — the host uses this
        /// to refresh the properties panel and mark the template dirty.</summary>
        public event EventHandler ElementsChanged;

        private enum DragMode { None, Move, Resize }
        private DragMode _drag = DragMode.None;
        private int _handle = -1;
        private PointF _dragStartMouse;
        private RectangleF _dragStartRect;
        private bool _mutateSnapshotTaken;

        public TemplateCanvas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(230, 233, 238);
            TabStop = true;
            AutoScroll = true;
        }

        public void UpdateCanvasSize()
        {
            if (Template == null) return;
            AutoScrollMinSize = new Size(
                (int)(Template.PageWidth * _zoom) + 80,
                (int)(Template.PageHeight * _zoom) + 80);
        }

        private RectangleF PageRect()
        {
            if (Template == null) return RectangleF.Empty;
            float pw = Template.PageWidth * _zoom, ph = Template.PageHeight * _zoom;
            float cx = Math.Max((ClientSize.Width - pw) / 2f, 20);
            float cy = Math.Max((ClientSize.Height - ph) / 2f, 20);
            return new RectangleF(cx - AutoScrollPosition.X, cy - AutoScrollPosition.Y, pw, ph);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Template == null) return;
            Graphics g = e.Graphics;
            RectangleF page = PageRect();

            using (var shadow = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                g.FillRectangle(shadow, page.X + 4, page.Y + 4, page.Width, page.Height);
            g.FillRectangle(Brushes.White, page);
            using (var pen = new Pen(Color.FromArgb(200, 203, 210)))
                g.DrawRectangle(pen, page.X, page.Y, page.Width, page.Height);

            GraphicsState gs = g.Save();
            g.TranslateTransform(page.X, page.Y);
            g.ScaleTransform(_zoom, _zoom);
            g.SetClip(new RectangleF(0, 0, Template.PageWidth, Template.PageHeight));
            TemplateRenderer.Draw(g, Template, Values, true, _selectedId, LoadTemplateImage);
            g.Restore(gs);
        }

        // ================================================================ point mapping

        private PointF ToPoints(Point screen)
        {
            RectangleF page = PageRect();
            return new PointF((screen.X - page.X) / _zoom, (screen.Y - page.Y) / _zoom);
        }

        private static bool HitsElement(TemplateElement el, PointF p)
        {
            if (el.Kind == "Line")
            {
                PointF a = new PointF(el.X, el.Y), b = new PointF(el.X + el.Width, el.Y + el.Height);
                return DistanceToSegment(p, a, b) <= 4f;
            }
            var r = new RectangleF(el.X, el.Y, Math.Max(el.Width, 2), Math.Max(el.Height, 2));
            return r.Contains(p);
        }

        private static float DistanceToSegment(PointF p, PointF a, PointF b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len2 = dx * dx + dy * dy;
            if (len2 < 0.001f) return Dist(p, a);
            float t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));
            var proj = new PointF(a.X + t * dx, a.Y + t * dy);
            return Dist(p, proj);
        }
        private static float Dist(PointF a, PointF b) =>
            (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

        // ================================================================ mouse

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (Template == null || ReadOnly) return;
            PointF p = ToPoints(e.Location);

            TemplateElement sel = Selected;
            if (sel != null && !sel.Locked)
            {
                var r = new RectangleF(sel.X, sel.Y, sel.Width, sel.Height);
                int h = TemplateRenderer.HandleAt(r, p, 6f);
                if (h >= 0 && (sel.Kind != "Line" || h == 0 || h == 4))
                {
                    _drag = DragMode.Resize; _handle = h;
                    _dragStartMouse = p; _dragStartRect = r;
                    _mutateSnapshotTaken = false;
                    return;
                }
            }

            TemplateElement hit = Template.Elements.OrderByDescending(x => x.ZIndex)
                .FirstOrDefault(x => HitsElement(x, p));
            if (hit != null)
            {
                SelectedId = hit.Id;
                if (!hit.Locked)
                {
                    _drag = DragMode.Move;
                    _dragStartMouse = p;
                    _dragStartRect = new RectangleF(hit.X, hit.Y, hit.Width, hit.Height);
                    _mutateSnapshotTaken = false;
                }
            }
            else
            {
                SelectedId = null;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_drag == DragMode.None || Template == null) return;
            TemplateElement sel = Selected;
            if (sel == null) return;

            if (!_mutateSnapshotTaken)
            {
                BeforeMutate?.Invoke(this, EventArgs.Empty);
                _mutateSnapshotTaken = true;
            }

            PointF p = ToPoints(e.Location);
            float dx = p.X - _dragStartMouse.X, dy = p.Y - _dragStartMouse.Y;

            if (_drag == DragMode.Move)
            {
                sel.X = Snap(_dragStartRect.X + dx);
                sel.Y = Snap(_dragStartRect.Y + dy);
            }
            else if (_drag == DragMode.Resize)
            {
                RectangleF r = _dragStartRect;
                float left = r.Left, top = r.Top, right = r.Right, bottom = r.Bottom;
                if (sel.Kind == "Line")
                {
                    if (_handle == 0) { sel.X = Snap(r.Left + dx); sel.Y = Snap(r.Top + dy); sel.Width = r.Right - sel.X; sel.Height = r.Bottom - sel.Y; }
                    else { sel.Width = Snap(r.Width + dx); sel.Height = Snap(r.Height + dy); }
                    return;
                }
                switch (_handle)
                {
                    case 0: left = r.Left + dx; top = r.Top + dy; break;
                    case 1: top = r.Top + dy; break;
                    case 2: right = r.Right + dx; top = r.Top + dy; break;
                    case 3: right = r.Right + dx; break;
                    case 4: right = r.Right + dx; bottom = r.Bottom + dy; break;
                    case 5: bottom = r.Bottom + dy; break;
                    case 6: left = r.Left + dx; bottom = r.Bottom + dy; break;
                    case 7: left = r.Left + dx; break;
                }
                float w = right - left, h = bottom - top;
                if (w < 6) { if (_handle == 0 || _handle == 6 || _handle == 7) left = right - 6; w = 6; }
                if (h < 6) { if (_handle == 0 || _handle == 1 || _handle == 2) top = bottom - 6; h = 6; }
                sel.X = Snap(left); sel.Y = Snap(top); sel.Width = Snap(w); sel.Height = Snap(h);
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_drag != DragMode.None)
            {
                _drag = DragMode.None;
                if (_mutateSnapshotTaken) ElementsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static float Snap(float v) => (float)Math.Round(v);

        // ================================================================ keyboard

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up ||
            keyData == Keys.Down || (keyData & Keys.KeyCode) == Keys.Delete ||
            base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Template == null || ReadOnly) return;
            TemplateElement sel = Selected;
            if (sel == null || sel.Locked) return;

            if (e.KeyCode == Keys.Delete)
            {
                BeforeMutate?.Invoke(this, EventArgs.Empty);
                Template.Elements.Remove(sel);
                SelectedId = null;
                ElementsChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
                e.Handled = true;
                return;
            }

            float step = e.Shift ? 8f : 1f;
            float dx = 0, dy = 0;
            if (e.KeyCode == Keys.Left) dx = -step;
            else if (e.KeyCode == Keys.Right) dx = step;
            else if (e.KeyCode == Keys.Up) dy = -step;
            else if (e.KeyCode == Keys.Down) dy = step;
            else return;

            BeforeMutate?.Invoke(this, EventArgs.Empty);
            sel.X += dx; sel.Y += dy;
            ElementsChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            e.Handled = true;
        }

        // ================================================================ element ops the host uses

        public void AddElement(TemplateElement el)
        {
            BeforeMutate?.Invoke(this, EventArgs.Empty);
            el.ZIndex = Template.Elements.Count == 0 ? 0 : Template.Elements.Max(x => x.ZIndex) + 1;
            Template.Elements.Add(el);
            SelectedId = el.Id;
            ElementsChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void Reorder(TemplateElement el, int direction)
        {
            // direction: -1000/+1000 = to back/front, -1/+1 = one step
            List<TemplateElement> ordered = Template.Elements.OrderBy(x => x.ZIndex).ToList();
            int idx = ordered.IndexOf(el);
            if (idx < 0) return;
            int target = Math.Max(0, Math.Min(ordered.Count - 1, idx + Math.Sign(direction) * (Math.Abs(direction) >= 1000 ? ordered.Count : 1)));
            if (target == idx) return;
            BeforeMutate?.Invoke(this, EventArgs.Empty);
            ordered.RemoveAt(idx);
            ordered.Insert(target, el);
            for (int i = 0; i < ordered.Count; i++) ordered[i].ZIndex = i;
            ElementsChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void NotifyExternalEdit()
        {
            Invalidate();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Step 1 of 2 — the client taps one or more service cards, then Next. Selections live on
    /// the shared <see cref="KioskSession"/> so Step 2 can read them (and Back can return here
    /// with them still highlighted). DialogResult.OK = go to Step 2; the flow controller in
    /// Program re-shows a fresh Step 1 otherwise.
    /// </summary>
    public partial class ServiceSelectForm : Form, IMessageFilter
    {
        private readonly KioskSession _session;
        private Timer _idle;
        private Timer _availability;
        private Action _resetIdle;
        // Set by every DELIBERATE close (Next / Back / idle / submit) so OnFormClosing can tell
        // our own navigation apart from the operator really quitting the kiosk. CloseReason is
        // NOT usable for this: a programmatic Close() also reports UserClosing, and WinForms
        // auto-assigns DialogResult.Cancel when the X is clicked, so neither one discriminates.
        private bool _navigating;


        // Per-card animation/enable state, keyed by service code.
        private readonly Dictionary<string, float> _cardHoverT = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _cardAvailable = new Dictionary<string, bool>();

        // Card metrics, scaled from the approved mockup (210px-wide card: 11px radius,
        // 34px icon, 12.5px label, 16px check badge inset 8px), but as PROPORTIONS of the
        // card's own size rather than fixed pixel counts — the compact one-row layout gives
        // every section its own card size, computed at runtime, so a card can end up far
        // smaller than the original 348x200 mockup card. Card.Paint below derives the actual
        // pixel metrics from card.Width/card.Height each time it draws.
        private const int Inset = 3;
        private const float CardRadiusFrac = 0.09f;   // 18 / 200
        private const float IconBoxFrac = 0.22f;      // shrunk vs. the mockup's 61/200 to leave
        private const float IconGapFrac = 0.05f;      // more room for a label that now wraps to
        private const float LabelBoxFrac = 0.32f;     // 2-3 lines on a compact card.
        private const float LabelPtFrac = 0.065f;
        private const float BadgeFrac = 0.16f;        // 26 / (avg 348x200 short side)
        private const float MinLabelPt = 6.5f;

        // Section layout: every section (Registration / Certification / Marriage & Family /
        // Certificates & Copies / Petitions & Legal / Other Services) sits in its own bordered
        // box, all boxes laid out in ONE HORIZONTAL ROW so nothing scrolls — the whole catalogue
        // must be visible at once. Card size is not fixed: LayoutSections() solves it from the
        // panel's actual size each time, so the grid always fits the screen it is running on
        // instead of relying on one hand-picked resolution.
        private const int OuterMarginX = 20, OuterMarginY = 10;
        private const int CardGap = 8;
        private const int SectionGap = 14;
        // Tall enough for a category name to wrap to TWO lines when a narrow column forces it
        // (e.g. "Marriage & Family") — TopLeft alignment below means a wrap grows DOWN inside
        // this box instead of spilling upward past the header's own bounds.
        private const int SectionHeaderH = 36;
        private const int SectionPadTop = SectionHeaderH + 8;   // header + gap, inside a section box
        private const int SectionPadSide = 10;
        private const int SectionPadBottom = 10;
        private const int MinCardW = 78, MinCardH = 58;

        private static readonly string[] SectionOrder =
        {
            KioskCore.SecRegistration, KioskCore.SecCertification, KioskCore.SecMarriageFamily,
            KioskCore.SecCertificates, KioskCore.SecPetitionsLegal, KioskCore.SecOther,
        };

        private readonly List<Panel> _sectionBoxes = new List<Panel>();

        public ServiceSelectForm(KioskSession session)
        {
            _session = session;
            InitializeComponent();
            panelStep1.AutoScroll = false;   // one-row layout is solved to always fit — never scroll

            foreach (Control control in _svcGrid.Controls)
                if (control is Panel card) RegisterCard(card);
            foreach (Panel card in _cards.Values) SetupCard(card);
            // Every card is reparented out of _svcGrid into the new section boxes below —
            // the empty panel itself would otherwise still sit at its Designer bounds (a
            // 1076x1064 OPAQUE rectangle) on top of the left half of the new layout.
            panelStep1.Controls.Remove(_svcGrid);
            LayoutSections();
            SetupNextButton();

            // Keep the footer buttons above the fill panel, and the offline overlay above all.
            _btnNext.BringToFront();
            _btnBack.BringToFront();
            _offlineOverlay.Bounds = ClientRectangle;
            _offlineOverlay.BringToFront();

            UpdateStepIndicator();

            Load += (s, e) => { RepaintAll(); UpdateAvailability(); };
            Shown += (s, e) => CenterServiceStep(panelStep1);

            _availability = new Timer { Interval = 4000 };
            _availability.Tick += (s, e) => UpdateAvailability();
            _availability.Start();

            // Idle: if the client wandered off mid-selection, drop all the way back to the
            // welcome screen rather than just clearing in place. The next person then has to
            // tap to start, so they can never inherit a half-made selection they didn't notice.
            _idle = new Timer { Interval = 1000 };
            int idleTicks = 0;
            _idle.Tick += (s, e) =>
            {
                idleTicks++;
                if (idleTicks >= KioskCore.IdleSecondsSelect)
                {
                    idleTicks = 0;
                    _session.Reset();
                    _navigating = true;
                    DialogResult = DialogResult.Cancel;   // Cancel => flow returns to Welcome
                    Close();
                }
            };
            _resetIdle = () => idleTicks = 0;
            _idle.Start();
            Application.AddMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == 0x0200 || m.Msg == 0x0201 || m.Msg == 0x0100 || m.Msg == 0x020A)
                _resetIdle?.Invoke();
            return false;
        }

        // ------------------------------------------------------ cards
        private void RegisterCard(Panel card)
        {
            if (card?.Tag is string code)
            {
                _cards[code] = card;
                _cardAvailable[code] = true;
            }
        }

        /// <summary>
        /// Wires one card's owner-draw (rounded card, vector icon, selected/hover/disabled
        /// states, no more BorderStyle.FixedSingle or emoji glyph), a smooth hover fade, and a
        /// quick tap-press flash (MouseEnter/Leave alone doesn't fire reliably on a touchscreen).
        /// </summary>
        private void SetupCard(Panel card)
        {
            string code = (string)card.Tag;
            _cardHoverT[code] = 0f;

            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(card, true);

            HoverFade.Attach(card, 160, t => { _cardHoverT[code] = t; card.Invalidate(); });

            bool pressed = false;
            card.MouseDown += (s, e) => { if (card.Enabled) { pressed = true; card.Invalidate(); } };
            card.MouseUp += (s, e) => { pressed = false; card.Invalidate(); };
            card.MouseLeave += (s, e) => { pressed = false; };

            card.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(card.Parent?.BackColor ?? KioskCore.Bg);

                // Metrics as fractions of THIS card's own size — the one-row layout gives every
                // section its own card size, so nothing here can be a fixed pixel count.
                int cardRadius = Math.Max(6, (int)(card.Height * CardRadiusFrac));
                float iconBox = card.Height * IconBoxFrac;
                float iconGap = card.Height * IconGapFrac;
                float labelBox = card.Height * LabelBoxFrac;
                float labelPt = Math.Max(MinLabelPt, card.Height * LabelPtFrac);
                int badgeSize = Math.Max(10, (int)(Math.Min(card.Width, card.Height) * BadgeFrac));
                int badgeInset = Math.Max(4, badgeSize / 2);

                bool available = _cardAvailable.TryGetValue(code, out bool av) && av;
                bool selected = _session.Selected.Contains(code);
                float hoverT = available && !selected ? _cardHoverT[code] : 0f;

                Color fill, border, iconColor, textColor;
                float borderW;
                if (!available)
                {
                    fill = Color.FromArgb(238, 240, 244);
                    border = KioskCore.Line;
                    borderW = 1.5f;
                    iconColor = Color.FromArgb(173, 181, 189);
                    textColor = iconColor;
                }
                else if (selected)
                {
                    // mockup .a-card.selected — #EAF1FE fill, accent border at full weight
                    fill = KioskCore.CardSelBg;
                    border = KioskCore.Accent;
                    borderW = 2.5f;
                    iconColor = KioskCore.Accent;
                    textColor = KioskCore.Accent;
                }
                else if (pressed)
                {
                    // mockup .a-card:active — #F3F7FF, snaps (no ease), shrinks slightly
                    fill = Color.FromArgb(243, 247, 255);
                    border = KioskCore.Accent;
                    borderW = 2f;
                    iconColor = KioskCore.Accent;
                    textColor = KioskCore.Accent;
                }
                else
                {
                    // mockup .a-card / :hover — #FFF -> #FAFBFE, border #E1E5EC -> #1D4ED8
                    fill = HoverFade.Lerp(KioskCore.CardBg, Color.FromArgb(250, 251, 254), hoverT);
                    border = HoverFade.Lerp(KioskCore.Line, KioskCore.Accent, hoverT);
                    borderW = HoverFade.Lerp(1.5f, 2f, hoverT);
                    iconColor = HoverFade.Lerp(KioskCore.Muted, KioskCore.Accent, hoverT);
                    textColor = HoverFade.Lerp(KioskCore.Ink, KioskCore.Accent, hoverT);
                }

                // The panel is 4px larger than the drawn card on every side, leaving room for
                // the hover drop-shadow to fall outside the card edge (mockup box-shadow).
                // Pressed shrinks the card by a further 2px (mockup scale(0.99)).
                float lift = pressed ? 0f : HoverFade.Lerp(0f, 2f, hoverT);
                int inset = pressed ? Inset + 2 : Inset;
                var rect = new Rectangle(inset, inset - (int)lift,
                    card.Width - inset * 2 - 1, card.Height - inset * 2 - 1);

                // Soft blue-tinted drop shadow, hover only (mockup: 0 6px 16px rgba(29,78,216,.14))
                if (hoverT > 0.02f && available && !selected)
                {
                    for (int i = 3; i >= 1; i--)
                    {
                        int a = (int)(14 * hoverT * (4 - i) / 3f);
                        if (a <= 0) continue;
                        var sr = new Rectangle(rect.X - i / 2, rect.Y + i, rect.Width + i, rect.Height + i);
                        using (var sp = RoundedRect(sr, cardRadius + 1))
                        using (var sb = new SolidBrush(Color.FromArgb(a, KioskCore.Accent)))
                            g.FillPath(sb, sp);
                    }
                }

                using (var path = RoundedRect(rect, cardRadius))
                {
                    using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                    using (var pen = new Pen(border, borderW)) g.DrawPath(pen, path);
                }

                // Icon + label as one vertically-centred block, proportioned off this card's
                // own size (so it still reads right however small the compact layout made it).
                float blockH = iconBox + iconGap + labelBox;
                float top = rect.Y + (rect.Height - blockH) / 2f;

                KioskIcons.For(code)(g, new RectangleF(rect.X, top, rect.Width, iconBox), iconColor);

                using (var f = new Font("Segoe UI", labelPt, FontStyle.Bold))
                using (var tb = new SolidBrush(textColor))
                using (var fmt = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisWord,
                })
                {
                    var textRect = new RectangleF(rect.X + 4, top + iconBox + iconGap, rect.Width - 8, labelBox);
                    g.DrawString(((Label)card.Controls[0]).Text, f, tb, textRect, fmt);
                }

                if (selected)
                {
                    // mockup .check — 16px circle inset 8px on a 210px card, scaled
                    var badge = new Rectangle(rect.Right - badgeInset - badgeSize, rect.Y + badgeInset, badgeSize, badgeSize);
                    using (var bb = new SolidBrush(KioskCore.Accent)) g.FillEllipse(bb, badge);
                    using (var wp = new Pen(Color.White, 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                        g.DrawLines(wp, new[]
                        {
                            new PointF(badge.X + badge.Width * 0.26f, badge.Y + badge.Height * 0.52f),
                            new PointF(badge.X + badge.Width * 0.44f, badge.Y + badge.Height * 0.70f),
                            new PointF(badge.X + badge.Width * 0.74f, badge.Y + badge.Height * 0.32f)
                        });
                }
            };

            // The Designer-placed name Label is kept only as a data holder (its .Text is what
            // card.Paint actually draws) — hidden so it can't double-render or intercept clicks
            // now that the whole card owner-draws itself.
            ((Label)card.Controls[0]).Visible = false;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            if (d <= 0 || d > r.Width || d > r.Height) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void Card_Click(object sender, EventArgs e)
        {
            Control c = (Control)sender;
            Panel card = c as Panel ?? c.Parent as Panel;
            if (card?.Tag is string code && card.Enabled) ToggleService(code);
        }

        private void ToggleService(string code)
        {
            if (_session.Selected.Contains(code)) _session.Selected.Remove(code);
            else _session.Selected.Add(code);
            PaintCard(code);
            UpdateNextButtonState();
            UpdateStepIndicator();
        }

        private void RepaintAll()
        {
            foreach (string code in _cards.Keys) PaintCard(code);
            UpdateNextButtonState();
            UpdateStepIndicator();
        }

        /// <summary>A PSA copy request (BREQS) inserts a third "PSA Document" step between this
        /// screen and Personal Info & Photo — see DetailsPhotoForm's ctor, which does the same
        /// check. Re-evaluated on every selection change since BREQS can be ticked/unticked
        /// before Next is pressed.</summary>
        private void UpdateStepIndicator()
        {
            _stepInd.Steps = _session.HasBreqs
                ? new[] { "Select Services", "PSA Document", "Personal Info & Photo" }
                : new[] { "Select Services", "Personal Info & Photo" };
            _stepInd.SetStep(0);
        }

        private void PaintCard(string code)
        {
            if (_cards.TryGetValue(code, out Panel card)) card.Invalidate();
        }

        // ------------------------------------------------------ sections
        /// <summary>
        /// Groups the catalogue into labeled sections (Registration / Certification /
        /// Marriage & Family / Certificates & Copies / Petitions & Legal / Other Services),
        /// each drawn as its own bordered box, and lays every box out in ONE HORIZONTAL ROW —
        /// no section stacks under another, and the panel never scrolls. Card size is solved
        /// from the panel's own ClientSize each call, so the whole catalogue always fits the
        /// screen it happens to be running on. Replaces the Designer's fixed per-card Location
        /// and Size — this is the single place both are decided now.
        /// </summary>
        private void LayoutSections()
        {
            // Reclaim every card from its current box (if any) before disposing the boxes —
            // a box's Dispose() also disposes its children, and cards are reused across calls.
            foreach (Panel card in _cards.Values) panelStep1.Controls.Add(card);
            foreach (Panel box in _sectionBoxes) { panelStep1.Controls.Remove(box); box.Dispose(); }
            _sectionBoxes.Clear();

            var sections = new List<(string Category, List<Service> Items, int Cols, int Rows)>();
            foreach (string category in SectionOrder)
            {
                var items = KioskCore.Catalogue.Where(s => s.Category == category && _cards.ContainsKey(s.Code)).ToList();
                if (items.Count == 0) continue;
                int cols = items.Count <= 2 ? 1 : items.Count <= 4 ? 2 : 3;
                int rows = (items.Count + cols - 1) / cols;
                sections.Add((category, items, cols, rows));
            }
            if (sections.Count == 0) return;

            // Card width: the row of section boxes (their padding + gaps + every card column
            // inside them) must fit the panel's width with no horizontal scroll.
            int availW = Math.Max(panelStep1.ClientSize.Width - OuterMarginX * 2, 200);
            int sumCols = sections.Sum(s => s.Cols);
            int gapWidthTotal = sections.Sum(s => (s.Cols - 1) * CardGap)
                + (sections.Count - 1) * SectionGap
                + sections.Count * (SectionPadSide * 2);
            int cardW = Math.Max(MinCardW, (availW - gapWidthTotal) / sumCols);

            // Card height: every box sits at the same Y, so the TALLEST section (most rows)
            // must still fit the panel's height with no vertical scroll.
            int topY = _svcHint.Bottom + 12;
            int availH = Math.Max(panelStep1.ClientSize.Height - topY - OuterMarginY, 140);
            int maxRows = sections.Max(s => s.Rows);
            int cardH = Math.Max(MinCardH,
                (availH - SectionPadTop - SectionPadBottom - (maxRows - 1) * CardGap) / maxRows);

            int x = OuterMarginX;
            foreach (var sec in sections)
            {
                int boxW = sec.Cols * cardW + (sec.Cols - 1) * CardGap + SectionPadSide * 2;
                int boxH = SectionPadTop + sec.Rows * cardH + (sec.Rows - 1) * CardGap + SectionPadBottom;

                Panel box = MakeSectionBox(boxW, boxH);
                box.Location = new Point(x, topY);

                Label header = MakeSectionHeader(sec.Category, boxW - SectionPadSide * 2);
                header.Location = new Point(SectionPadSide, 4);
                box.Controls.Add(header);

                int col = 0, row = 0;
                foreach (var svc in sec.Items)
                {
                    Panel card = _cards[svc.Code];
                    card.Size = new Size(cardW, cardH);
                    card.Location = new Point(
                        SectionPadSide + col * (cardW + CardGap),
                        SectionPadTop + row * (cardH + CardGap));
                    box.Controls.Add(card);
                    col++;
                    if (col == sec.Cols) { col = 0; row++; }
                }

                panelStep1.Controls.Add(box);
                box.BringToFront();
                foreach (Control c in box.Controls) c.BringToFront();
                _sectionBoxes.Add(box);

                x += boxW + SectionGap;
            }
        }

        /// <summary>One bordered, rounded card per section — "each group has its own clearly
        /// separated area/card."</summary>
        private static Panel MakeSectionBox(int width, int height)
        {
            var box = new Panel { Size = new Size(width, height), BackColor = KioskCore.Bg };
            box.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, box.Width - 1, box.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 10))
                {
                    using (var b = new SolidBrush(KioskCore.CardBg)) g.FillPath(b, path);
                    using (var pen = new Pen(KioskCore.Line, 1.2f)) g.DrawPath(pen, path);
                }
            };
            return box;
        }

        /// <summary>One style for every section heading — the point of grouping the catalogue
        /// is that every section reads the same, so this is the only place that style is set.</summary>
        private Label MakeSectionHeader(string text, int width) => new Label
        {
            AutoSize = false,
            Size = new Size(width, SectionHeaderH),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = KioskCore.Accent,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft,   // a wrap grows down, inside the header's own box
            UseMnemonic = false,   // category names carry a literal "&" (Marriage & Family, ...)
            Text = text,
        };

        // ------------------------------------------------- availability
        private void UpdateAvailability()
        {
            bool open = KioskCore.OfficeOnline();
            _offlineOverlay.Visible = !open;
            if (!open) _offlineOverlay.BringToFront();
            ApplyServiceAvailability();
        }

        private void ApplyServiceAvailability()
        {
            HashSet<string> avail = KioskCore.AvailableServiceCodes();
            foreach (var svc in KioskCore.Catalogue)
            {
                if (!_cards.TryGetValue(svc.Code, out Panel card)) continue;
                bool ok = avail.Contains(svc.Code);
                if (!ok && _session.Selected.Contains(svc.Code)) _session.Selected.Remove(svc.Code);
                _cardAvailable[svc.Code] = ok;
                card.Enabled = ok;
                card.Cursor = ok ? Cursors.Hand : Cursors.No;
                card.Invalidate();
            }
            UpdateNextButtonState();
        }

        // ------------------------------------------------- layout / nav
        // No scrolling: every resize just re-solves the one-row section layout for the new
        // size, rather than scrolling to or centring a fixed-size grid.
        private void PanelStep1_Resize(object sender, EventArgs e) => LayoutSections();

        private void CenterServiceStep(Control wrap) => LayoutSections();

        /// <summary>
        /// Styles Next through the shared <see cref="KioskButtons"/> treatment so Step 1 and
        /// Step 2 can't drift apart again (this button used to own a private copy of the
        /// owner-draw code, which is exactly how Step 2 ended up on a different palette).
        /// The arrow trails the label, so it reads as "forward".
        /// </summary>
        private void SetupNextButton()
        {
            KioskButtons.Style(_btnNext, KioskButtonKind.Primary, KioskCore.IconArrowRight,
                iconRight: true, backdrop: footer.BackColor);
            KioskButtons.Style(_btnBack, KioskButtonKind.Secondary, KioskCore.IconArrowLeft,
                backdrop: footer.BackColor);
            UpdateNextButtonState();
        }

        /// <summary>
        /// Back to the welcome screen. Clears the session first so nothing the client picked
        /// survives — leaving Step 1 is always a full reset, same as the idle timeout.
        /// </summary>
        private void BtnBack_Click(object sender, EventArgs e)
        {
            _session.Reset();
            _navigating = true;
            DialogResult = DialogResult.Cancel;   // Cancel => flow returns to Welcome
            Close();
        }

        private void UpdateNextButtonState()
        {
            _btnNext.Enabled = _session.Selected.Count > 0;
            _btnNext.Invalidate();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_session.Selected.Count == 0)
            {
                MessageBox.Show("Please tap at least one service to continue.", "Please check",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _navigating = true;
            DialogResult = DialogResult.OK;   // proceed to Step 2
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Application.RemoveMessageFilter(this);
            _idle?.Stop();
            _availability?.Stop();
            // A hard window close (Alt+F4 / X) quits the kiosk entirely.
            if (!_navigating && e.CloseReason == CloseReason.UserClosing)
                Environment.Exit(0);
            base.OnFormClosing(e);
        }
    }
}

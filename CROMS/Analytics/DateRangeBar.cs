using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Analytics
{
    /// <summary>
    /// The date-range strip that sits between the summary cards and the charts on every
    /// analytics tab: a preset combo plus two pickers that appear only for Custom.
    ///
    /// It filters the CHARTS only. The summary cards above it keep their own fixed periods
    /// (this month vs last month, this year vs last year), which is why each card states its
    /// period on its face — the two must never look like they disagree.
    /// </summary>
    public class DateRangeBar : UserControl
    {
        public const int BarHeight = 46;

        private readonly ComboBox _preset;
        private readonly DateTimePicker _from;
        private readonly DateTimePicker _to;
        private readonly Label _resolved;
        private readonly Label _scope;
        private bool _loading;

        /// <summary>Raised when the effective range changes. Hosts re-load their widgets.</summary>
        public event EventHandler RangeChanged;

        public DateRangeBar()
        {
            Height = BarHeight;
            Dock = DockStyle.Top;
            BackColor = UiTheme.PageBg;

            var caption = new Label
            {
                Text = "Charts show:",
                AutoSize = false,
                Location = new Point(0, 14),
                Size = new Size(78, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = UiTheme.Muted
            };

            _preset = new ComboBox
            {
                Location = new Point(82, 11),
                Size = new Size(150, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                FlatStyle = FlatStyle.Flat
            };
            foreach (RangePreset p in Enum.GetValues(typeof(RangePreset)))
                _preset.Items.Add(DateRange.Label(p));

            _from = new DateTimePicker
            {
                Location = new Point(240, 11),
                Size = new Size(118, 24),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Visible = false
            };

            _to = new DateTimePicker
            {
                Location = new Point(364, 11),
                Size = new Size(118, 24),
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 9F),
                Visible = false
            };

            _resolved = new Label
            {
                AutoSize = false,
                Location = new Point(240, 14),
                Size = new Size(280, 20),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = UiTheme.Faint
            };

            _scope = new Label
            {
                AutoSize = false,
                Location = new Point(528, 14),
                Size = new Size(420, 20),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Italic),
                ForeColor = UiTheme.Faint,
                Text = "Cards above use their own fixed periods."
            };

            Controls.Add(caption);
            Controls.Add(_preset);
            Controls.Add(_from);
            Controls.Add(_to);
            Controls.Add(_resolved);
            Controls.Add(_scope);

            _loading = true;
            _preset.SelectedIndex = (int)RangePreset.ThisYear;   // a useful default on this data
            _from.Value = DateTime.Today.AddMonths(-3);
            _to.Value = DateTime.Today;
            _loading = false;

            _preset.SelectedIndexChanged += OnChanged;
            _from.ValueChanged += OnChanged;
            _to.ValueChanged += OnChanged;

            UpdateVisibility();
        }

        /// <summary>The preset currently selected.</summary>
        public RangePreset Preset
        {
            get
            {
                int i = _preset.SelectedIndex;
                return i < 0 ? RangePreset.ThisYear : (RangePreset)i;
            }
        }

        /// <summary>The effective range the charts should use.</summary>
        public DateRange Range
        {
            get
            {
                return Preset == RangePreset.Custom
                    ? new DateRange(_from.Value, _to.Value)
                    : DateRange.Resolve(Preset);
            }
        }

        private void OnChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            UpdateVisibility();
            var h = RangeChanged;
            if (h != null) h(this, EventArgs.Empty);
        }

        private void UpdateVisibility()
        {
            bool custom = Preset == RangePreset.Custom;
            _from.Visible = custom;
            _to.Visible = custom;
            _resolved.Visible = !custom;
            if (!custom)
            {
                DateRange r = DateRange.Resolve(Preset);
                _resolved.Text = Preset == RangePreset.AllTime
                    ? "everything on record up to " + r.To.ToString("dd MMM yyyy")
                    : r.ToString();
            }
            _scope.Left = custom ? 492 : 528;
        }

        /// <summary>Sets the preset without raising RangeChanged (used when a tab restores state).</summary>
        public void SetPresetQuietly(RangePreset preset)
        {
            _loading = true;
            _preset.SelectedIndex = (int)preset;
            _loading = false;
            UpdateVisibility();
        }
    }
}

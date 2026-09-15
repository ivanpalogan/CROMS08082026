using System;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>
    /// "Certificate Templates" — the one place LCRO staff go to change how a certificate
    /// looks, without touching code, Crystal Reports, or the database. One card per form
    /// CROMS can lay out; each card opens the same visual designer
    /// (<see cref="TemplateDesignerForm"/>) that Preview/Edit both use.
    /// <para/>
    /// Content is entirely data-driven off <see cref="TemplateStore.KnownForms"/>, so this
    /// form is built in code rather than the Designer, the same way the other dynamic-list
    /// admin screens in this app are (Records Archive, Petitions' list side).
    /// </summary>
    public class TemplateManagementForm : Form, IRefreshable
    {
        private FlowLayoutPanel _flow;
        private Label _lblEmpty;

        public TemplateManagementForm()
        {
            Text = "Certificate Templates";
            BackColor = UiTheme.PageBg;
            AutoScroll = true;
            Font = new Font("Segoe UI", 9.5f);

            var title = new Label
            {
                Text = "Certificate Templates",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(24, 20),
            };
            var subtitle = new Label
            {
                Text = "Design how each certificate looks — logos, text, fields and lines — without touching code.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 52),
            };

            _lblEmpty = new Label
            {
                Text = "No certificate forms are registered for template editing yet.",
                ForeColor = UiTheme.Muted,
                AutoSize = true,
                Location = new Point(24, 100),
                Visible = false,
            };

            _flow = new FlowLayoutPanel
            {
                Location = new Point(20, 84),
                Size = new Size(1160, 700),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                AutoScroll = true,
                BackColor = UiTheme.PageBg,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
            };

            Controls.Add(_flow);
            Controls.Add(_lblEmpty);
            Controls.Add(subtitle);
            Controls.Add(title);

            BuildCards();
        }

        public void RefreshData() => BuildCards();

        private void BuildCards()
        {
            _flow.SuspendLayout();
            _flow.Controls.Clear();
            foreach (TemplateFormInfo info in TemplateStore.KnownForms)
                _flow.Controls.Add(BuildCard(info));
            _lblEmpty.Visible = TemplateStore.KnownForms.Count == 0;
            _flow.ResumeLayout();
        }

        private CardPanel BuildCard(TemplateFormInfo info)
        {
            var card = new CardPanel
            {
                Size = new Size(360, 190),
                Margin = new Padding(0, 0, 18, 18),
                Radius = 12,
            };

            var category = new Label
            {
                Text = info.Category.ToUpperInvariant(),
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UiTheme.Accent,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 16),
            };
            var name = new Label
            {
                Text = info.FormName,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(324, 44),
                Location = new Point(18, 36),
            };
            var status = new Label
            {
                Text = TemplateStore.HasCustomTemplate(info.FormCode)
                    ? "Custom layout saved"
                    : "Using the office's original layout",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.Muted,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 82),
            };

            var btnEdit = MakeButton("Edit Template", UiTheme.Accent, Color.White, 18, 110);
            var btnPreview = MakeButton("Preview", UiTheme.Chrome, UiTheme.Ink, 168, 110);
            var btnRestore = MakeButton("Restore Default", UiTheme.Chrome, UiTheme.Danger, 18, 148);

            btnEdit.Click += (s, e) =>
            {
                var confirm = new AdminVerificationForm();
                if (confirm.ShowDialog(this) != DialogResult.OK) return;
                OpenDesigner(info, startInEditMode: true);
            };
            btnPreview.Click += (s, e) => OpenDesigner(info, startInEditMode: false);
            btnRestore.Click += (s, e) => RestoreDefault(info);

            card.Controls.Add(btnRestore);
            card.Controls.Add(btnPreview);
            card.Controls.Add(btnEdit);
            card.Controls.Add(status);
            card.Controls.Add(name);
            card.Controls.Add(category);

            UiTheme.Polish(card);
            return card;
        }

        private static Button MakeButton(string text, Color back, Color fore, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(142, 32),
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
            };
        }

        private void OpenDesigner(TemplateFormInfo info, bool startInEditMode)
        {
            using (var dlg = new TemplateDesignerForm(info, startInEditMode))
                dlg.ShowDialog(this);
            BuildCards();
        }

        private void RestoreDefault(TemplateFormInfo info)
        {
            var confirm = new AdminVerificationForm();
            if (confirm.ShowDialog(this) != DialogResult.OK) return;

            DialogResult r = MessageBox.Show(this,
                "Restore \"" + info.FormName + "\" to the office's original layout?\n\n" +
                "Any custom positions, text, images or lines added to this template will be lost. " +
                "This does not change any saved civil registry record.",
                "Restore default layout", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;

            TemplateStore.RestoreDefault(info.FormCode, confirm.VerifiedUser?.Id);
            Audit.Write("Update", "certificate_templates", 0,
                "Restored default template for " + info.FormCode);
            BuildCards();
            MessageBox.Show(this, "Restored to the default layout.", "Restore default layout",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

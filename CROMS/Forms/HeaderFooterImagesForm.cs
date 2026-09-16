using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Manage the header/footer letterhead images (logo, badges, footer banner) for ONE
    /// Facts Certification form (1A/2A/3A) - reusable across all three instead of the
    /// Form-3A-only dialog this replaced. The caller supplies the form's own
    /// <see cref="CROMS.Data.OfficeAssets"/> form code, its display title, and which image
    /// slots that form's printed layout actually uses (Form 2A has no second right-side
    /// badge, so it is simply not offered one here).
    /// <para/>
    /// Every image is saved SCOPED TO THIS FORM CODE through <see cref="OfficeAssets.Save"/>
    /// - never as the office-wide default - so uploading a seal for Form 2A can never
    /// silently change what Form 1A or Form 3A print. A slot with nothing uploaded for this
    /// form still shows a preview of the office-wide default when one exists, and prints
    /// with that fallback; a slot with neither never blocks printing, it just prints blank.
    /// </summary>
    public class HeaderFooterImagesForm : Form
    {
        /// <summary>One image position this form's layout uses, and the caption shown
        /// beside it (e.g. "Header logo - left").</summary>
        public struct ImageFieldSpec
        {
            public AssetKind Kind;
            public string Caption;
            public ImageFieldSpec(AssetKind kind, string caption) { Kind = kind; Caption = caption; }
        }

        private sealed class Row
        {
            public AssetKind Kind;
            public string Caption;
            public PictureBox Picture;
            public Label Info;
            public Button BtnSave;
            public Button BtnRemove;
            public byte[] PendingBytes;
            public string PendingMime;
            public string PendingFileName;
        }

        private const int MaxBytes = 3 * 1024 * 1024;
        private const int RowHeight = 132;

        private readonly string _formCode;
        private readonly string _formTitle;
        private readonly List<Row> _rows = new List<Row>();
        private readonly Label _lblStatus = new Label();

        public HeaderFooterImagesForm(string formCode, string formTitle, IEnumerable<ImageFieldSpec> fields)
        {
            _formCode = formCode;
            _formTitle = formTitle;

            Text = formTitle + " - Header & Footer Images";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.PageBg;

            var title = new Label
            {
                Text = formTitle,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = UiTheme.Ink,
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var sub = new Label
            {
                Text = "These images print in the header and footer of this form only. " +
                       "A slot left empty here uses the office-wide default (Settings) if one exists, " +
                       "or simply prints blank - it never stops the certificate from printing.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.Muted,
                AutoSize = false,
                MaximumSize = new Size(660, 0),
                Location = new Point(20, 44)
            };
            Controls.Add(title);
            Controls.Add(sub);

            int y = 80;
            var list = new List<ImageFieldSpec>(fields);
            foreach (ImageFieldSpec spec in list)
            {
                AddRow(spec.Kind, spec.Caption, y);
                y += RowHeight;
            }

            _lblStatus.SetBounds(20, y + 6, 560, 34);
            _lblStatus.Font = new Font("Segoe UI", 9f);
            _lblStatus.ForeColor = UiTheme.Muted;
            Controls.Add(_lblStatus);

            var close = new Button
            {
                Text = "Close",
                Location = new Point(600, y),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            close.Click += (s, e) => Close();
            Controls.Add(close);

            ClientSize = new Size(710, y + 60);

            foreach (Row row in _rows) LoadRow(row);
            UiTheme.Polish(this);
        }

        private void AddRow(AssetKind kind, string caption, int y)
        {
            var row = new Row { Kind = kind, Caption = caption };

            var group = new GroupBox
            {
                Text = caption,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, y),
                Size = new Size(670, RowHeight - 10),
                BackColor = Color.Transparent
            };

            row.Picture = new PictureBox
            {
                Location = new Point(14, 26),
                Size = new Size(150, 84),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = UiTheme.Surface,
                BorderStyle = BorderStyle.FixedSingle
            };

            row.Info = new Label
            {
                Location = new Point(178, 26),
                Size = new Size(340, 60),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = UiTheme.Muted,
                AutoEllipsis = false
            };

            var btnChoose = new Button
            {
                Text = "Choose / Replace...",
                Location = new Point(178, 92),
                Size = new Size(150, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnChoose.Click += (s, e) => Choose(row);

            row.BtnSave = new Button
            {
                Text = "Save",
                Location = new Point(334, 92),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Enabled = false
            };
            row.BtnSave.Click += (s, e) => Save(row);

            row.BtnRemove = new Button
            {
                Text = "Remove",
                Location = new Point(430, 92),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Enabled = false
            };
            row.BtnRemove.Click += (s, e) => Remove(row);

            group.Controls.Add(row.Picture);
            group.Controls.Add(row.Info);
            group.Controls.Add(btnChoose);
            group.Controls.Add(row.BtnSave);
            group.Controls.Add(row.BtnRemove);
            Controls.Add(group);
            _rows.Add(row);
        }

        // ===================================================================
        // Load - shows the image saved for THIS form, else the office-wide
        // fallback (preview only, so the operator can see what will actually
        // print here before uploading one specific to this form).
        // ===================================================================

        private void LoadRow(Row row)
        {
            row.Picture.Image?.Dispose();
            row.Picture.Image = null;
            row.PendingBytes = null;
            row.PendingMime = null;
            row.PendingFileName = null;
            row.BtnSave.Enabled = false;

            bool exact = false;
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT name, image, created_at, uploaded_by FROM office_assets " +
                    "WHERE asset_kind = @kind AND is_active = 1 AND form_code = @form " +
                    "ORDER BY created_at DESC LIMIT 1",
                    new MySqlParameter("@kind", row.Kind.ToString()),
                    new MySqlParameter("@form", _formCode));

                if (dt.Rows.Count > 0)
                {
                    exact = true;
                    DataRow r = dt.Rows[0];
                    byte[] bytes = r["image"] == DBNull.Value ? null : (byte[])r["image"];
                    row.Picture.Image = OfficeAssets.FromBytes(bytes);
                    row.Info.Text = (r["name"] as string ?? row.Caption) + "\n" +
                        (bytes == null ? 0 : bytes.Length / 1024) + " KB - " +
                        Convert.ToDateTime(r["created_at"]).ToString("dd MMM yyyy") +
                        " - set for " + _formTitle + " by " + r["uploaded_by"];
                    row.Info.ForeColor = UiTheme.Ink;
                    row.BtnRemove.Enabled = true;
                }
            }
            catch (MySqlException)
            {
                row.Info.Text = "Could not read images for this form. " +
                                 "Run the branding/form-identity migration if this is a new install.";
                row.Info.ForeColor = UiTheme.Warning;
                return;
            }

            if (!exact)
            {
                byte[] fallback = OfficeAssets.GetBytes(row.Kind, _formCode);
                if (fallback != null)
                {
                    row.Picture.Image = OfficeAssets.FromBytes(fallback);
                    row.Info.Text = "Not set for this form - using the office-wide default (" +
                                     (fallback.Length / 1024) + " KB). Upload one here to use a " +
                                     "different image just for " + _formTitle + ".";
                }
                else
                {
                    row.Info.Text = "Not set. This space will print blank until an image is uploaded - " +
                                     "that never stops the certificate from printing.";
                }
                row.Info.ForeColor = UiTheme.Muted;
                row.BtnRemove.Enabled = false;
            }
        }

        // ===================================================================
        // Choose / validate / save / remove
        // ===================================================================

        private void Choose(Row row)
        {
            using (var dlg = new OpenFileDialog
            {
                Title = "Choose an image for " + row.Caption,
                Filter = "PNG or JPEG images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                CheckFileExists = true
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var fi = new FileInfo(dlg.FileName);
                if (fi.Length > MaxBytes)
                {
                    MessageBox.Show(this,
                        "That file is " + (fi.Length / 1024 / 1024) + " MB. Please use an image " +
                        "under 3 MB - a larger file is re-read on every certificate printed and " +
                        "slows printing down without looking any better.",
                        "Image too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                byte[] data = File.ReadAllBytes(dlg.FileName);
                using (Image probe = OfficeAssets.FromBytes(data))
                {
                    if (probe == null)
                    {
                        MessageBox.Show(this, "That file could not be read as an image.",
                            "Not an image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string ext = fi.Extension.TrimStart('.').ToLowerInvariant();
                row.PendingBytes = data;
                row.PendingMime = "image/" + (ext == "jpg" ? "jpeg" : ext);
                row.PendingFileName = fi.Name;

                row.Picture.Image?.Dispose();
                row.Picture.Image = OfficeAssets.FromBytes(data);
                row.Info.Text = fi.Name + "\n" + (data.Length / 1024) + " KB - not saved yet";
                row.Info.ForeColor = UiTheme.Ink;
                row.BtnSave.Enabled = true;
                Say("Image chosen for " + row.Caption + ". Press Save to store it.");
            }
        }

        private void Save(Row row)
        {
            if (row.PendingBytes == null)
            {
                MessageBox.Show(this, "Choose an image first.", row.Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                OfficeAssets.Save(row.Kind, row.Caption + " (" + _formTitle + ")",
                    row.PendingBytes, _formCode, row.PendingMime);
                Audit.Write("Update", "office_assets", 0,
                    "Saved " + row.Kind + " for " + _formCode);
                Say(row.Caption + " saved for " + _formTitle +
                    ". It will appear on the next certificate printed.");
                LoadRow(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save: " + ex.Message, row.Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Deactivates rather than deletes - a certificate already issued carried
        /// that image, and the record should still be able to show which one it was.</summary>
        private void Remove(Row row)
        {
            if (MessageBox.Show(this,
                    "Stop using this " + row.Caption.ToLowerInvariant() + " image for " +
                    _formTitle + "?\n\n" +
                    "Certificates printed from now on will use the office-wide default instead " +
                    "(if one exists), or print this space blank. The image stays in the records " +
                    "so a previously issued copy can still be explained.",
                    "Remove image", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            try
            {
                Db.Push(
                    "UPDATE office_assets SET is_active = 0 " +
                    "WHERE asset_kind = @kind AND is_active = 1 AND form_code = @form",
                    new MySqlParameter("@kind", row.Kind.ToString()),
                    new MySqlParameter("@form", _formCode));
                OfficeAssets.ClearCache();
                Audit.Write("Update", "office_assets", 0,
                    "Removed " + row.Kind + " for " + _formCode);
                Say(row.Caption + " removed for " + _formTitle + ".");
                LoadRow(row);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not remove: " + ex.Message, row.Caption,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Say(string message)
        {
            _lblStatus.Text = message;
            _lblStatus.ForeColor = UiTheme.Success;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            foreach (Row row in _rows) row.Picture.Image?.Dispose();
        }
    }
}

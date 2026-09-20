using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Manage the office's branding and its own details: the LOGO, the STAMP, and the
    /// office profile that a certificate's header and signature block are drawn from.
    /// <para/>
    /// Logo and stamp are handled INDEPENDENTLY — separate pickers, separate previews,
    /// separate save and clear — because the office replaces them on different occasions
    /// and a form revision may carry its own stamp while sharing the office seal. Each can
    /// be stored office-wide (the default, used by every form) or scoped to one form.
    /// <para/>
    /// A code-built modal dialog, following the same convention as WindowEditDialog and
    /// MarriageEntryForm: it is opened from Settings, not embedded as a module surface.
    /// <para/>
    /// UI layout lives in OfficeAssetsForm.Designer.cs; this file holds the data/logic.
    /// </summary>
    internal partial class OfficeAssetsForm : Form
    {
        private byte[] _logoBytes, _stampBytes;
        private string _logoMime, _stampMime;

        /// <summary>Max size accepted for a logo or stamp. Anything larger is almost
        /// certainly a full photograph rather than a seal, and a multi-megabyte blob is
        /// re-read on every certificate print.</summary>
        private const int MaxBytes = 3 * 1024 * 1024;

        public OfficeAssetsForm()
        {
            InitializeComponent();
        }

        // ===================================================================
        // Load
        // ===================================================================

        /// <summary>Office-wide first, then one entry per form in the catalog.</summary>
        private void LoadScopes()
        {
            _cboScope.Items.Clear();
            _cboScope.Items.Add(new ScopeItem(null, "All forms (office-wide default)"));
            foreach (FormDefinition d in FormCatalog.All)
                _cboScope.Items.Add(new ScopeItem(d.FormCode,
                    d.FormName + " — Municipal Form No. " + d.MunicipalFormNo +
                    ", " + d.Revision));
            // 1A/2A/3A Facts Certification letters (Data\Form3ACert/3BCert/3CCert) are a
            // separate catalog from FormCatalog.All (those are registry certificates). Death's
            // real letterhead uses a different municipal seal than marriage/birth's, so each
            // needs its own scope option to be reachable at all.
            _cboScope.Items.Add(new ScopeItem(Form3BCert.FormCode, Form3BCert.FormName + " (Civil Registry Form No. 1A)"));
            _cboScope.Items.Add(new ScopeItem(Form3CCert.FormCode, Form3CCert.FormName + " (Form 2A)"));
            _cboScope.Items.Add(new ScopeItem(Form3ACert.FormCode, Form3ACert.FormName + " (FORM 3A)"));
            _cboScope.SelectedIndex = 0;
        }

        private string Scope => (_cboScope.SelectedItem as ScopeItem)?.FormCode;

        private void Scope_Changed(object sender, EventArgs e) => LoadAssets();

        /// <summary>
        /// Show what is stored for the current scope. Deliberately queries the exact scope
        /// (form_code IS NULL, or = this form) rather than going through
        /// <see cref="OfficeAssets.Get"/>, whose fall-back-to-default behaviour would make
        /// an inherited office logo look like one saved against this form.
        /// </summary>
        private void LoadAssets()
        {
            _logoBytes = _stampBytes = null;
            _logoMime = _stampMime = null;

            Fill(AssetKind.Logo, _picLogo, _lblLogoInfo, ref _logoBytes);
            Fill(AssetKind.Stamp, _picStamp, _lblStampInfo, ref _stampBytes);
        }

        private void Fill(AssetKind kind, PictureBox box, Label info, ref byte[] slot)
        {
            box.Image?.Dispose();
            box.Image = null;

            try
            {
                DataTable dt = Db.Pull(
                    "SELECT name, image, created_at, uploaded_by FROM office_assets " +
                    "WHERE asset_kind = @kind AND is_active = 1 " +
                    "  AND ((form_code IS NULL AND @form IS NULL) OR form_code = @form) " +
                    "ORDER BY created_at DESC LIMIT 1",
                    new MySqlParameter("@kind", kind.ToString()),
                    new MySqlParameter("@form", (object)Scope ?? DBNull.Value));

                if (dt.Rows.Count == 0)
                {
                    info.Text = Scope == null
                        ? "No " + kind.ToString().ToLowerInvariant() + " saved."
                        : "None for this form — the office-wide " +
                          kind.ToString().ToLowerInvariant() + " will be used.";
                    return;
                }

                DataRow r = dt.Rows[0];
                slot = r["image"] == DBNull.Value ? null : (byte[])r["image"];
                box.Image = OfficeAssets.FromBytes(slot);
                info.Text = r["name"] + "  ·  " +
                            (slot == null ? "0" : (slot.Length / 1024).ToString()) + " KB  ·  " +
                            Convert.ToDateTime(r["created_at"]).ToString("dd MMM yyyy") +
                            "  ·  " + r["uploaded_by"];
            }
            catch (MySqlException)
            {
                info.Text = "Run database migration 26_form_identity.sql to enable this.";
            }
        }

        private void LoadProfile()
        {
            OfficeAssets.ReloadProfile();
            OfficeProfile p = OfficeAssets.Profile;
            _txtOffice.Text = p.OfficeName;
            _txtMunicipality.Text = p.Municipality;
            _txtProvince.Text = p.Province;
            _txtRegistrar.Text = p.RegistrarName;
            _txtTitle.Text = p.RegistrarTitle;
        }

        // ===================================================================
        // Pick / save / remove — logo and stamp handled separately throughout
        // ===================================================================

        private void ChooseLogo_Click(object sender, EventArgs e)
        {
            if (Pick(out byte[] bytes, out string mime, out string name))
            {
                _logoBytes = bytes; _logoMime = mime;
                _picLogo.Image?.Dispose();
                _picLogo.Image = OfficeAssets.FromBytes(bytes);
                _lblLogoInfo.Text = name + "  ·  " + (bytes.Length / 1024) +
                                    " KB  ·  not saved yet";
                Say("Logo chosen. Press Save Logo to store it.");
            }
        }

        private void ChooseStamp_Click(object sender, EventArgs e)
        {
            if (Pick(out byte[] bytes, out string mime, out string name))
            {
                _stampBytes = bytes; _stampMime = mime;
                _picStamp.Image?.Dispose();
                _picStamp.Image = OfficeAssets.FromBytes(bytes);
                _lblStampInfo.Text = name + "  ·  " + (bytes.Length / 1024) +
                                     " KB  ·  not saved yet";
                Say("Stamp chosen. Press Save Stamp to store it.");
            }
        }

        /// <summary>
        /// Pick an image file. A transparent PNG is what a stamp usually wants — it is
        /// drawn over the certificate — so PNG leads the filter.
        /// </summary>
        private bool Pick(out byte[] bytes, out string mime, out string name)
        {
            bytes = null; mime = null; name = null;
            using (var dlg = new OpenFileDialog
            {
                Title = "Choose an image",
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|" +
                         "*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                CheckFileExists = true
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return false;

                var fi = new FileInfo(dlg.FileName);
                if (fi.Length > MaxBytes)
                {
                    MessageBox.Show(
                        "That file is " + (fi.Length / 1024 / 1024) + " MB. A logo or " +
                        "stamp should be under 3 MB — a larger image is re-read on every " +
                        "certificate printed and will slow printing down without looking " +
                        "any better.\n\nSave a smaller copy and try again.",
                        "Image too large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                byte[] data = File.ReadAllBytes(dlg.FileName);
                // Confirm it really decodes before storing it — a renamed non-image would
                // otherwise sit in the database and fail silently at print time.
                using (Image probe = OfficeAssets.FromBytes(data))
                {
                    if (probe == null)
                    {
                        MessageBox.Show("That file could not be read as an image.",
                            "Not an image", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                }

                bytes = data;
                name = fi.Name;
                string ext = fi.Extension.TrimStart('.').ToLowerInvariant();
                mime = "image/" + (ext == "jpg" ? "jpeg" : ext);
                return true;
            }
        }

        private void SaveLogo_Click(object sender, EventArgs e) =>
            SaveAsset(AssetKind.Logo, _logoBytes, _logoMime);

        private void SaveStamp_Click(object sender, EventArgs e) =>
            SaveAsset(AssetKind.Stamp, _stampBytes, _stampMime);

        private void SaveAsset(AssetKind kind, byte[] bytes, string mime)
        {
            if (bytes == null)
            {
                MessageBox.Show("Choose an image first.", kind.ToString(),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                string label = Scope == null
                    ? "Office " + kind
                    : kind + " for " + Scope;
                OfficeAssets.Save(kind, label, bytes, Scope, mime);
                Audit.Write("Update", "office_assets", 0,
                    "Saved " + kind + " for " + (Scope ?? "all forms"));
                LoadAssets();
                Say(kind + " saved for " + (Scope ?? "all forms") +
                    ". It will appear on the next certificate printed.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save: " + ex.Message, kind.ToString(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveLogo_Click(object sender, EventArgs e) => Remove(AssetKind.Logo);
        private void RemoveStamp_Click(object sender, EventArgs e) => Remove(AssetKind.Stamp);

        /// <summary>
        /// Stop using an asset. The row is DEACTIVATED, never deleted — a certificate
        /// already issued carried that seal, and a registry should still be able to show
        /// which one it was.
        /// </summary>
        private void Remove(AssetKind kind)
        {
            if (MessageBox.Show(
                    "Stop using this " + kind.ToString().ToLowerInvariant() + " for " +
                    (Scope ?? "all forms") + "?\n\n" +
                    "Certificates printed from now on will not carry it. The image is kept " +
                    "in the records so a previously issued copy can still be explained.",
                    "Remove " + kind, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
                != DialogResult.Yes) return;

            try
            {
                Db.Push(
                    "UPDATE office_assets SET is_active = 0 " +
                    "WHERE asset_kind = @kind AND is_active = 1 " +
                    "  AND ((form_code IS NULL AND @form IS NULL) OR form_code = @form)",
                    new MySqlParameter("@kind", kind.ToString()),
                    new MySqlParameter("@form", (object)Scope ?? DBNull.Value));
                OfficeAssets.ClearCache();
                Audit.Write("Update", "office_assets", 0,
                    "Removed " + kind + " for " + (Scope ?? "all forms"));
                LoadAssets();
                Say(kind + " removed for " + (Scope ?? "all forms") + ".");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not remove: " + ex.Message, kind.ToString(),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveProfile_Click(object sender, EventArgs e)
        {
            if (_txtOffice.Text.Trim().Length == 0)
            {
                MessageBox.Show("The office name is printed on every certificate — it " +
                                "cannot be blank.", "Office details",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtOffice.Focus();
                return;
            }
            try
            {
                Db.Push(
                    "UPDATE office_profile SET office_name = @n, municipality = @m, " +
                    "province = @p, registrar_name = @r, registrar_title = @t WHERE id = 1",
                    new MySqlParameter("@n", _txtOffice.Text.Trim()),
                    new MySqlParameter("@m", _txtMunicipality.Text.Trim()),
                    new MySqlParameter("@p", _txtProvince.Text.Trim()),
                    new MySqlParameter("@r", _txtRegistrar.Text.Trim()),
                    new MySqlParameter("@t", _txtTitle.Text.Trim()));
                OfficeAssets.ReloadProfile();
                Audit.Write("Update", "office_profile", 1, "Updated office details");
                Say("Office details saved.");
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(
                    "Could not save. If the table is missing, run database migration " +
                    "26_form_identity.sql.\n\n" + ex.Message, "Office details",
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
            _picLogo.Image?.Dispose();
            _picStamp.Image?.Dispose();
        }

        /// <summary>An entry in the "Applies to" list: office-wide, or one form.</summary>
        private class ScopeItem
        {
            public readonly string FormCode;
            private readonly string _label;
            public ScopeItem(string formCode, string label)
            { FormCode = formCode; _label = label; }
            public override string ToString() => _label;
        }
    }
}

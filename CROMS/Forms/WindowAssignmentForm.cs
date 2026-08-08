using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Shown right after login (from Program.cs). The operator picks the service
    /// window they will occupy and the transaction types they will handle. On
    /// confirm the window is marked Online (current_operator = this user, heartbeat
    /// now), locked to this operator, and its transaction set is written to
    /// `window_transactions`. A window already occupied by another online operator
    /// cannot be selected. An operator who is not manning a window (e.g. an admin)
    /// can Skip.
    /// <para/>
    /// UI layout lives in WindowAssignmentForm.Designer.cs; this file holds only the
    /// logic. The window radio list and per-service checkboxes are data / catalogue
    /// driven, so they are built here at runtime into the Designer's container panels.
    /// </summary>
    public partial class WindowAssignmentForm : Form
    {
        /// <summary>The kiosk service catalogue — code + friendly label.</summary>
        public static readonly (string Code, string Label)[] Catalogue =
        {
            ("NEWREG",   "New Registration"),
            ("CTC",      "Certified True Copy (CTC)"),
            ("MARRIAGE", "Marriage Certificate"),
            ("DEATH",    "Death Certificate"),
            ("PETITION", "Petition (Correction)"),
            ("VERIFY",   "Verification / Others"),
            ("CLAIM",    "Claim / Pickup Document"),
        };

        /// <summary>Minutes without a heartbeat before a window is treated as Offline.</summary>
        public const int StaleMinutes = 2;

        private readonly List<RadioButton> _windowRadios = new List<RadioButton>();
        private readonly List<CheckBox> _txnBoxes = new List<CheckBox>();

        public WindowAssignmentForm()
        {
            InitializeComponent();

            lblSubtitle.Text = (Session.User != null ? Session.User.FullName + " — " : "") +
                               "choose your window and the transactions you will handle.";

            BuildWindowChoices();
            BuildTransactionChoices();
        }

        // ---- data / catalogue-driven UI (built at runtime, not in the Designer) ----

        private void BuildTransactionChoices()
        {
            // The Priority checkbox (chkAll) already sits at row 0 spanning both columns.
            // Each service flows into the next free cell → two balanced columns, and each
            // cell auto-sizes to its label so long names (e.g. "Certified True Copy (CTC)")
            // are never clipped.
            foreach (var svc in Catalogue)
            {
                var cb = new CheckBox
                {
                    Text = svc.Label,
                    Tag = svc.Code,
                    Font = new Font("Segoe UI", 10F),
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Margin = new Padding(20, 4, 12, 6)
                };
                _txnBoxes.Add(cb);
                txnPanel.Controls.Add(cb);
            }
        }

        private void BuildWindowChoices()
        {
            _windowRadios.Clear();
            winPanel.Controls.Clear();

            DataTable wins = Db.Pull(
                "SELECT id, window_name, current_operator, operator_name, " +
                "(current_operator IS NOT NULL AND last_heartbeat > (NOW() - INTERVAL " + StaleMinutes +
                " MINUTE)) AS online " +
                "FROM windows WHERE status = 'Active' ORDER BY display_order, id");

            if (wins.Rows.Count == 0)
            {
                winPanel.Controls.Add(new Label
                {
                    Text = "No active windows. Ask an admin to add one in Service Windows.",
                    AutoSize = true, ForeColor = Color.FromArgb(108, 117, 125),
                    Margin = new Padding(8)
                });
                return;
            }

            int myId = Session.User?.Id ?? -1;
            foreach (DataRow w in wins.Rows)
            {
                int id = Convert.ToInt32(w["id"]);
                bool online = w["online"] != DBNull.Value && Convert.ToInt32(w["online"]) == 1;
                int op = w["current_operator"] == DBNull.Value ? -1 : Convert.ToInt32(w["current_operator"]);
                bool takenByOther = online && op != myId;

                var rb = new RadioButton
                {
                    Text = w["window_name"] +
                           (takenByOther ? "   — occupied by " + w["operator_name"] : online ? "   — you" : ""),
                    Tag = id,
                    AutoSize = true,
                    Enabled = !takenByOther,
                    ForeColor = takenByOther ? Color.FromArgb(173, 181, 189) : Color.FromArgb(33, 37, 41),
                    Font = new Font("Segoe UI", 10F),
                    Margin = new Padding(8, 4, 8, 4)
                };
                _windowRadios.Add(rb);
                winPanel.Controls.Add(rb);
            }
        }

        // ---- event handlers ----

        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var b in _txnBoxes) { b.Enabled = !chkAll.Checked; if (chkAll.Checked) b.Checked = false; }
        }

        private void btnSkip_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;   // enter the app with no window claimed
            Close();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            RadioButton chosen = _windowRadios.Find(r => r.Checked);
            if (chosen == null) { lblMsg.Text = "Select a window, or press Skip if you are not on a window."; return; }

            var codes = new List<string>();
            if (!chkAll.Checked)
                foreach (var b in _txnBoxes) if (b.Checked) codes.Add((string)b.Tag);
            if (!chkAll.Checked && codes.Count == 0)
            {
                lblMsg.Text = "Select at least one transaction, or tick Priority Window.";
                return;
            }

            int windowId = (int)chosen.Tag;

            // Re-check occupancy at the last moment (another operator may have taken it).
            if (IsOccupiedByOther(windowId, Session.User?.Id ?? -1))
            {
                lblMsg.Text = "This window is already occupied by another operator.";
                BuildWindowChoices();
                return;
            }

            try
            {
                // Ticking Priority makes this a Priority Window: handles ALL transactions,
                // keeps its client until every service is done, and is never auto-forwarded
                // (spec section 1). Otherwise it's a regular window with the chosen services.
                Db.Push(
                    "UPDATE windows SET current_operator = @op, operator_name = @nm, last_heartbeat = NOW(), " +
                    "is_priority = @pri WHERE id = @id",
                    new MySqlParameter("@op", Session.UserIdParam),
                    new MySqlParameter("@nm", (object)(Session.User?.FullName) ?? DBNull.Value),
                    new MySqlParameter("@pri", chkAll.Checked ? 1 : 0),
                    new MySqlParameter("@id", windowId));

                Db.Push("DELETE FROM window_transactions WHERE window_id = @id",
                    new MySqlParameter("@id", windowId));
                foreach (string code in codes)      // empty when Priority → handles everything
                    Db.Push("INSERT INTO window_transactions (window_id, service_code) VALUES (@w, @c)",
                        new MySqlParameter("@w", windowId), new MySqlParameter("@c", code));
            }
            catch (Exception ex)
            {
                lblMsg.Text = "Could not claim the window: " + ex.Message;
                return;
            }

            Session.WindowId = windowId;
            Session.WindowName = chosen.Text.Split('—')[0].Trim();
            Audit.Write(Audit.Update, "windows", windowId,
                "Signed in to " + Session.WindowName + " (" +
                (chkAll.Checked ? "Priority — all transactions" : string.Join(", ", codes)) + ")");

            DialogResult = DialogResult.OK;
            Close();
        }

        // ---- shared presence helpers (used by MainForm heartbeat / logout) ----

        private static bool IsOccupiedByOther(int windowId, int myUserId)
        {
            DataTable dt = Db.Pull(
                "SELECT current_operator FROM windows WHERE id = @id AND current_operator IS NOT NULL " +
                "AND current_operator <> @me AND last_heartbeat > (NOW() - INTERVAL " + StaleMinutes + " MINUTE)",
                new MySqlParameter("@id", windowId), new MySqlParameter("@me", myUserId));
            return dt.Rows.Count > 0;
        }

        /// <summary>Keeps the window's Online presence alive (called on a timer).</summary>
        public static void Heartbeat(int windowId)
        {
            if (windowId <= 0) return;
            try
            {
                Db.Push("UPDATE windows SET last_heartbeat = NOW() WHERE id = @id",
                    new MySqlParameter("@id", windowId));
            }
            catch { /* non-fatal */ }
        }

        /// <summary>Frees a window (sets it Offline) — on logout / app exit.</summary>
        public static void Release(int windowId)
        {
            if (windowId <= 0) return;
            try
            {
                Db.Push("UPDATE windows SET current_operator = NULL, operator_name = NULL, " +
                        "last_heartbeat = NULL WHERE id = @id",
                    new MySqlParameter("@id", windowId));
            }
            catch { /* non-fatal */ }
        }
    }
}

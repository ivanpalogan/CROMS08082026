using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using CROMS.Data;
using CROMS.Modules;
using MySql.Data.MySqlClient;

namespace CROMS.Forms
{
    /// <summary>
    /// Transactions — the master ledger. Every client visit is one transaction row
    /// (created by Certificate Request / registration, closed by Release &amp; Claim).
    /// This screen lists them all, searchable by client/code and filterable by status,
    /// so any visit can be traced end to end. Read-only. Controls in the Designer.
    /// </summary>
    public partial class TransactionsForm : Form, IRefreshable
    {
        public void RefreshData() => LoadTxns();

        public TransactionsForm()
        {
            InitializeComponent();
            cboStatus.Items.AddRange(new object[]
            {
                "All", "Queued", "Processing", "ForPayment", "ForRelease", "Released", "Cancelled"
            });
            cboStatus.SelectedItem = "All";
            cboStatus.SelectedIndexChanged += (s, e) => LoadTxns();
            txtSearch.TextChanged += (s, e) => LoadTxns();
            AddSlipButton();
            LoadTxns();
        }

        // Built in code (not the Designer) so a Designer regeneration can never drop it.
        private void AddSlipButton()
        {
            var btn = new Button
            {
                Text = "Print Service Slip",
                Font = new Font("Segoe UI", 9.75F),
                Size = new Size(140, btnRefresh.Height),
                Location = new Point(btnRefresh.Right + 8, btnRefresh.Top),
                Anchor = btnRefresh.Anchor
            };
            btn.Click += btnSlip_Click;
            btnRefresh.Parent.Controls.Add(btn);
            UiTheme.Polish(btn);
        }

        /// <summary>Client Service Slip for the selected transaction: same slip (and the same control
        /// number every time) the client is handed for a petition, so a tracked visit always carries one number.</summary>
        private void btnSlip_Click(object sender, EventArgs e)
        {
            if (dgvTxn.CurrentRow == null || !dgvTxn.Columns.Contains("id"))
            {
                MessageBox.Show(this, "Select a transaction first.", "Client Service Slip", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                int id = Convert.ToInt32(dgvTxn.CurrentRow.Cells["id"].Value);
                DataRow r = ((DataRowView)dgvTxn.CurrentRow.DataBoundItem).Row;
                string type = Convert.ToString(r["Type"]);
                string docType = type == "Birth" || type == "Marriage" || type == "Death" ? type : "Others";
                var s = new SlipRequest
                {
                    SourceTable = "transactions", SourceId = id,
                    RequesterName = Convert.ToString(r["Client"]),
                    DocumentOwner = Convert.ToString(r["Client"]),
                    DocType = docType,
                    DocTypeOther = docType == "Others" ? type : null,
                    TxnDate = Convert.ToDateTime(r["Created"]).Date,
                    AttendingStaff = Session.User != null ? Session.User.FullName : "",
                    Remarks = "Transaction " + Convert.ToString(r["Txn Code"]) + " - " + Convert.ToString(r["Status"])
                };
                ClientServiceSlip.Show(s, this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Client Service Slip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadTxns();

        private void LoadTxns()
        {
            string sql =
                "SELECT id, txn_code AS 'Txn Code', client_name AS Client, type AS Type, " +
                "status AS Status, created_at AS Created FROM transactions WHERE 1=1";

            var ps = new List<MySqlParameter>();

            if (cboStatus.SelectedItem != null && cboStatus.SelectedItem.ToString() != "All")
            {
                sql += " AND status = @st";
                ps.Add(new MySqlParameter("@st", cboStatus.SelectedItem.ToString()));
            }
            if (!string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                sql += " AND (client_name LIKE @q OR txn_code LIKE @q)";
                ps.Add(new MySqlParameter("@q", "%" + txtSearch.Text.Trim() + "%"));
            }
            sql += " ORDER BY id DESC";

            dgvTxn.DataSource = ps.Count == 0 ? Db.Pull(sql) : Db.Pull(sql, ps.ToArray());
            if (dgvTxn.Columns.Contains("id")) dgvTxn.Columns["id"].Visible = false;
        }
    }
}

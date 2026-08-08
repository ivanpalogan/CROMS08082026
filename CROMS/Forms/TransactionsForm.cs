using System;
using System.Collections.Generic;
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
            LoadTxns();
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadTxns();

        private void LoadTxns()
        {
            string sql =
                "SELECT txn_code AS 'Txn Code', client_name AS Client, type AS Type, " +
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
        }
    }
}

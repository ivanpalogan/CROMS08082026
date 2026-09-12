using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CROMS.Modules;

namespace CROMS.Analytics.Widgets
{
    /// <summary>
    /// Money collected at the counter, by day, with a toggle that breaks the same total down by
    /// service type.
    ///
    /// WHERE THE SERVICE TYPE COMES FROM. `payments` has no service column — it carries the
    /// amounts, the OR number and the transaction it belongs to. The service is a property of
    /// the TRANSACTION, so the split is `transactions.type` reached through
    /// `payments.transaction_id`. A payment whose transaction is missing is shown as
    /// "Unlinked" rather than dropped: it is real money the office took, and money that
    /// disappears from a collection report is the worst kind of missing.
    ///
    /// The figure is `net_amount`, which is what the same column the PSA monthly report
    /// totals — so this chart and that report cannot disagree.
    /// </summary>
    public class CertCollectionWidget : AnalyticsWidget
    {
        private const string Unlinked = "Unlinked";

        private readonly CheckBox _bySplit;

        public CertCollectionWidget()
            : base("Daily collection", "by payment date (payments.paid_at) · net_amount")
        {
            _bySplit = new CheckBox
            {
                Text = "By service type",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = UiTheme.Muted,
                BackColor = UiTheme.Surface,
                Size = new Size(112, 18)
            };
            _bySplit.CheckedChanged += (s, e) => { if (Range.To > DateTime.MinValue) Load(Range); };
            SetHeaderControl(_bySplit);
        }

        protected override void Render(DateRange range)
        {
            if (_bySplit.Checked) RenderSplit(range);
            else RenderTotal(range);
        }

        // ------------------------------------------------------------- daily total
        private void RenderTotal(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATE(paid_at) AS d, SUM(net_amount) AS amount, COUNT(*) AS n " +
                "FROM payments " +
                "WHERE paid_at >= @from AND paid_at < @to " +
                "GROUP BY d ORDER BY d", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No payments recorded in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            var labels = new List<string>();
            var amounts = new List<decimal>();
            decimal total = 0;
            int receipts = 0;
            decimal best = 0;
            string bestDay = "";

            foreach (DataRow r in dt.Rows)
            {
                DateTime day = Convert.ToDateTime(r["d"]);
                decimal amount = r["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["amount"]);
                labels.Add(day.ToString("dd MMM"));
                amounts.Add(amount);
                total += amount;
                receipts += AnalyticsData.Int(r, "n");
                if (amount > best) { best = amount; bestDay = day.ToString("dd MMM"); }
            }

            ResetChart();
            bool sparse = labels.Count < 3;
            Series s = sparse ? ChartStyle.Column("Collected", 2) : ChartStyle.Line("Collected", 2);
            for (int i = 0; i < labels.Count; i++) s.Points.AddXY(labels[i], (double)amounts[i]);
            Chart.Series.Add(s);

            ChartStyle.Axes(Chart, "Day", "Pesos collected");
            Money(Chart);
            if (labels.Count > 10) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildTotalCaption(total, receipts, labels.Count, best, bestDay, sparse));
        }

        // -------------------------------------------------------- split by service
        private void RenderSplit(DateRange range)
        {
            DataTable dt = AnalyticsData.Query(
                "SELECT DATE(p.paid_at) AS d, COALESCE(t.type, '') AS svc, SUM(p.net_amount) AS amount " +
                "FROM payments p " +
                "LEFT JOIN transactions t ON t.id = p.transaction_id " +
                "WHERE p.paid_at >= @from AND p.paid_at < @to " +
                "GROUP BY d, svc ORDER BY d", range);

            if (dt.Rows.Count == 0)
            {
                ShowEmpty(EmptyStatePanel.Kind.NoRows,
                    "No payments recorded in this period.",
                    "Widen the date range, or choose All Time.");
                return;
            }

            var days = new List<string>();
            var services = new List<string>();
            var cells = new Dictionary<string, Dictionary<string, decimal>>();
            var totals = new Dictionary<string, decimal>();
            decimal grand = 0;

            foreach (DataRow r in dt.Rows)
            {
                string day = Convert.ToDateTime(r["d"]).ToString("dd MMM");
                string svc = AnalyticsData.Str(r, "svc");
                if (svc.Length == 0) svc = Unlinked;
                decimal amount = r["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["amount"]);

                if (!days.Contains(day)) days.Add(day);
                if (!services.Contains(svc)) { services.Add(svc); totals[svc] = 0m; }
                if (!cells.ContainsKey(svc)) cells[svc] = new Dictionary<string, decimal>();
                if (!cells[svc].ContainsKey(day)) cells[svc][day] = 0m;

                cells[svc][day] += amount;
                totals[svc] += amount;
                grand += amount;
            }

            ResetChart();
            for (int i = 0; i < services.Count; i++)
            {
                Series s = ChartStyle.StackedColumn(services[i], i, false);
                foreach (string day in days)
                {
                    decimal v;
                    if (!cells[services[i]].TryGetValue(day, out v)) v = 0m;
                    s.Points.AddXY(day, (double)v);
                }
                Chart.Series.Add(s);
            }

            ChartStyle.Axes(Chart, "Day", "Pesos collected");
            Money(Chart);
            if (days.Count > 10) ChartStyle.AngleXLabels(Chart, -45);
            AutoLegend();

            SetCaption(BuildSplitCaption(services, totals, grand, days.Count));
        }

        /// <summary>Peso formatting on the value axis; the axis is money, not a count.</summary>
        private static void Money(Chart chart)
        {
            ChartArea a = chart.ChartAreas[0];
            a.AxisY.Minimum = 0;
            a.AxisY.LabelStyle.Format = "₱#,##0";
        }

        /// <summary>Deterministic string.Format over the same daily totals the chart was built from.</summary>
        private string BuildTotalCaption(decimal total, int receipts, int days,
                                         decimal best, string bestDay, bool sparse)
        {
            if (sparse)
            {
                return string.Format("₱{0:N2} collected over {1} day{2} on {3} receipt{4}. Too few days to describe a trend.",
                    total, days, days == 1 ? "" : "s", receipts, receipts == 1 ? "" : "s");
            }

            return string.Format(
                "₱{0:N2} collected over {1} days on {2} receipts — about ₱{3:N2} a day, and ₱{4:N2} per receipt. Best day was {5} at ₱{6:N2}.",
                total, days, receipts, total / days,
                receipts == 0 ? 0m : total / receipts, bestDay, best);
        }

        /// <summary>Deterministic string.Format over the same service totals the columns were built from.</summary>
        private string BuildSplitCaption(List<string> services, Dictionary<string, decimal> totals,
                                         decimal grand, int days)
        {
            string unlinked = !totals.ContainsKey(Unlinked) || totals[Unlinked] == 0m
                ? ""
                : string.Format(" ₱{0:N2} sits on payments with no transaction linked.", totals[Unlinked]);

            string lead = "";
            decimal best = -1m;
            foreach (KeyValuePair<string, decimal> kv in totals)
                if (kv.Key != Unlinked && kv.Value > best) { best = kv.Value; lead = kv.Key; }

            if (lead.Length == 0)
            {
                return string.Format("₱{0:N2} collected over {1} day{2}, none of it linked to a transaction type.{3}",
                    grand, days, days == 1 ? "" : "s", unlinked);
            }

            return string.Format(
                "₱{0:N2} collected over {1} day{2} across {3} service type{4}. {5} brings in the most at ₱{6:N2} ({7:0.#}%).{8}",
                grand, days, days == 1 ? "" : "s",
                services.Count, services.Count == 1 ? "" : "s",
                lead, best, AnalyticsData.Pct((double)best, (double)grand), unlinked);
        }
    }
}

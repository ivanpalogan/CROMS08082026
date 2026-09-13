using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>One fee on the office's schedule (the fee card).</summary>
    public sealed class FeeItem
    {
        public int Id;
        public string Code, Description, Category, CardNote;
        /// <summary>Null when the office has not stated an amount - the cashier types it.</summary>
        public decimal? Amount;
        public bool Active;
        public int Sort;

        public string Display
        {
            get { return Description + (Amount.HasValue ? "  -  PHP " + Amount.Value.ToString("#,0.00") : "  -  amount not set"); }
        }
        public override string ToString() { return Display; }
    }

    /// <summary>One fee line on an Official Receipt.</summary>
    public sealed class PaymentLine
    {
        public string FeeCode, Description;
        public int Quantity = 1;
        public decimal UnitAmount;
        public decimal LineAmount { get { return UnitAmount * Quantity; } }
    }

    /// <summary>A payment to record. Transaction, walk-in, or one recorded from another module.</summary>
    public sealed class PaymentEntry
    {
        public int? TransactionId;
        public string Source = PaymentService.SourceWalkIn;
        public string SourceTable;
        public int? SourceId;
        public string PayerName, Purpose, OrNumber, Method = "Cash", ReferenceNo, Remarks;
        public decimal Additional;
        /// <summary>Cash handed over; null when not captured (a payment recorded from another module).</summary>
        public decimal? Tendered;
        public DateTime? PaidAt;
        public List<PaymentLine> Lines = new List<PaymentLine>();

        public decimal Gross { get { return Lines.Sum(l => l.LineAmount); } }
        public decimal Total { get { return Gross + Additional; } }
    }

    /// <summary>
    /// Fees and payments. The office's collection is done by the Municipal Treasury (the fee card is
    /// headed "PLS. PAY AT TREASURY OFFICE"); CROMS records the Official Receipt it issued, what it
    /// covered and why. One log for every payment - a transaction at the cashier window, a walk-in
    /// collection that belongs to no module, a BREQS fee, a marriage licence - so the monthly
    /// collection report can be built from one place.
    /// </summary>
    public static class PaymentService
    {
        public const string SourceTransaction = "Transaction", SourceWalkIn = "Walk-in", SourceBreqs = "BREQS", SourceMarriage = "Marriage License";
        public static readonly string[] Methods = { "Cash", "GCash", "Bank Transfer" };

        /// <summary>Purposes a walk-in payment is commonly for - an editable suggestion list.</summary>
        public static readonly string[] Purposes =
        {
            "Certified copy", "Certification", "Annotation", "BREQS (PSA copy)", "Marriage application", "Marriage license",
            "Marriage solemnization", "Electronic endorsement", "Out of town registration", "Petition filing",
            "Burial permit", "Transfer of cadaver", "Others"
        };

        // ================================================================ fee schedule
        public static List<FeeItem> Fees(bool activeOnly)
        {
            DataTable t = Db.Pull("SELECT id, code, description, category, amount, card_note, sort_order, is_active FROM fees " +
                                  (activeOnly ? "WHERE is_active = 1 " : "") + "ORDER BY is_active DESC, sort_order, description");
            return t.AsEnumerable().Select(r => new FeeItem
            {
                Id = Convert.ToInt32(r["id"]), Code = r["code"].ToString(), Description = r["description"].ToString(),
                Category = r["category"] as string, CardNote = r["card_note"] as string,
                Amount = r["amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["amount"]),
                Sort = Convert.ToInt32(r["sort_order"]), Active = Convert.ToInt32(r["is_active"]) != 0
            }).ToList();
        }

        public static FeeItem Fee(string code)
        {
            return Fees(false).FirstOrDefault(f => string.Equals(f.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Change a fee's amount (the office changes fees by ordinance). Null clears it to "not set".
        /// Audited with the old and new amount - a fee change changes every receipt after it.
        /// </summary>
        public static void UpdateFee(string code, decimal? amount, bool active, int? userId)
        {
            FeeItem cur = Fee(code);
            if (cur == null) throw new InvalidOperationException("Fee " + code + " not found.");
            if (amount.HasValue && amount.Value < 0) throw new InvalidOperationException("A fee cannot be negative.");
            if (cur.Amount == amount && cur.Active == active) return;
            Db.Push("UPDATE fees SET amount = @a, is_active = @on WHERE code = @c",
                    P("@a", amount), P("@on", active ? 1 : 0), P("@c", code));
            Audit.Write(Audit.Update, "fees", cur.Id, code + ": " + Money(cur.Amount) + " -> " + Money(amount) +
                        (cur.Active != active ? (active ? ", reactivated" : ", deactivated") : ""));
        }

        /// <summary>What a certificate request costs: the CTC / certification fee for its type, times its copies.</summary>
        public static PaymentLine AssessCertificate(string certType, string recordType, int copies)
        {
            string code = certType == "Negative" ? "NEG-CERT" : "CTC-" + (recordType ?? "Birth").ToUpperInvariant();
            FeeItem f = Fee(code);
            return new PaymentLine
            {
                FeeCode = code,
                Description = f != null ? f.Description : code + " (fee not found)",
                Quantity = Math.Max(1, copies),
                UnitAmount = f != null && f.Amount.HasValue ? f.Amount.Value : 0m
            };
        }

        // ================================================================ recording
        public static List<string> Validate(PaymentEntry e)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(e.OrNumber)) errors.Add("Enter the Treasury Official Receipt number.");
            if (e.TransactionId == null && e.SourceId == null && string.IsNullOrWhiteSpace(e.PayerName)) errors.Add("Enter the payer's name.");
            if (e.TransactionId == null && e.SourceId == null && string.IsNullOrWhiteSpace(e.Purpose)) errors.Add("Enter the purpose of the payment.");
            if (e.Lines.Count == 0) errors.Add("Add at least one fee.");
            if (e.Lines.Any(l => l.Quantity < 1)) errors.Add("Every quantity must be at least 1.");
            if (e.Lines.Any(l => l.UnitAmount < 0) || e.Additional < 0) errors.Add("Amounts cannot be negative.");
            if (e.Lines.Any(l => string.IsNullOrWhiteSpace(l.Description))) errors.Add("Every fee line needs a description.");
            if (e.Total <= 0) errors.Add("The total must be more than zero - type the amount for a fee with no amount set.");
            bool cash = (e.Method ?? "Cash").StartsWith("Cash", StringComparison.OrdinalIgnoreCase);
            if (!cash && string.IsNullOrWhiteSpace(e.ReferenceNo)) errors.Add("Enter the reference number for the " + e.Method + " payment.");
            if (cash && e.Tendered.HasValue && e.Tendered.Value < e.Total) errors.Add("Amount tendered is less than the total.");
            return errors;
        }

        /// <summary>The payment an Official Receipt number is already recorded on, or null.</summary>
        public static DataRow FindByOr(string orNumber)
        {
            if (string.IsNullOrWhiteSpace(orNumber)) return null;
            DataTable t = Db.Pull("SELECT p.id, p.paid_at, p.net_amount, p.source, p.source_table, p.source_id, p.transaction_id, " +
                                  "COALESCE(p.payer_name, t.client_name) AS payer FROM payments p LEFT JOIN transactions t ON t.id = p.transaction_id " +
                                  "WHERE p.or_number = @o ORDER BY p.id LIMIT 1", P("@o", orNumber.Trim()));
            return t.Rows.Count == 0 ? null : t.Rows[0];
        }

        /// <summary>
        /// Record a payment and its fee lines in one database transaction. An Official Receipt is an
        /// accountable form issued once; the same O.R. recorded twice would count the collection twice,
        /// so a number already in the log is refused (one O.R. covering several fees is ONE payment
        /// with several lines).
        /// </summary>
        public static int Record(PaymentEntry e, int? userId)
        {
            List<string> errors = Validate(e);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            DataRow dup = FindByOr(e.OrNumber);
            if (dup != null)
                throw new InvalidOperationException("O.R. " + e.OrNumber.Trim() + " is already recorded (" + Convert.ToDateTime(dup["paid_at"]).ToString("dd MMM yyyy") +
                                                    ", PHP " + Convert.ToDecimal(dup["net_amount"]).ToString("#,0.00") + ", " + dup["payer"] + "). An O.R. is recorded once.");

            bool cash = (e.Method ?? "Cash").StartsWith("Cash", StringComparison.OrdinalIgnoreCase);
            decimal? change = cash && e.Tendered.HasValue && e.Tendered.Value > e.Total ? e.Tendered - e.Total : null;
            using (var conn = new MySqlConnection(ServerConfig.EffectiveConnectionString))
            {
                conn.Open();
                using (MySqlTransaction tx = conn.BeginTransaction())
                {
                    var cmd = new MySqlCommand(
                        "INSERT INTO payments (transaction_id, payer_name, purpose, source, source_table, source_id, or_number, reference_no, payment_method, " +
                        "gross_amount, additional_fee, net_amount, amount_tendered, change_amount, remarks, cashier_id, paid_at) VALUES " +
                        "(@t, @payer, @purpose, @src, @stab, @sid, @or, @ref, @m, @g, @add, @n, @tend, @chg, @rem, @by, @at)", conn, tx);
                    cmd.Parameters.AddRange(new[]
                    {
                        P("@t", e.TransactionId), P("@payer", T(e.PayerName)), P("@purpose", T(e.Purpose)), P("@src", e.Source ?? SourceWalkIn),
                        P("@stab", e.SourceTable), P("@sid", e.SourceId), P("@or", e.OrNumber.Trim()), P("@ref", T(e.ReferenceNo)), P("@m", e.Method ?? "Cash"),
                        P("@g", e.Gross), P("@add", e.Additional), P("@n", e.Total), P("@tend", e.Tendered), P("@chg", change), P("@rem", T(e.Remarks)),
                        P("@by", userId), P("@at", e.PaidAt ?? DateTime.Now)
                    });
                    cmd.ExecuteNonQuery();
                    int id = (int)cmd.LastInsertedId;
                    foreach (PaymentLine l in e.Lines)
                    {
                        var li = new MySqlCommand(
                            "INSERT INTO payment_items (payment_id, fee_id, fee_code, description, quantity, unit_amount, line_amount) " +
                            "VALUES (@p, (SELECT id FROM fees WHERE code = @c), @c, @d, @q, @u, @l)", conn, tx);
                        li.Parameters.AddRange(new[] { P("@p", id), P("@c", T(l.FeeCode)), P("@d", l.Description.Trim()), P("@q", l.Quantity), P("@u", l.UnitAmount), P("@l", l.LineAmount) });
                        li.ExecuteNonQuery();
                    }
                    tx.Commit();
                    Audit.Write(Audit.Create, "payments", id, e.Source + " O.R. " + e.OrNumber.Trim() + " PHP " + e.Total.ToString("#,0.00") +
                                (string.IsNullOrWhiteSpace(e.Purpose) ? "" : " - " + e.Purpose.Trim()));
                    return id;
                }
            }
        }

        /// <summary>
        /// Throws when an O.R. is already recorded for something other than this module record. Called
        /// BEFORE a module changes its own status, so a refused receipt leaves nothing half-recorded.
        /// A walk-in payment with the same O.R. and no link is fine - RecordForModule adopts it.
        /// </summary>
        public static void EnsureOrFree(string orNumber, string sourceTable, int sourceId)
        {
            DataRow dup = FindByOr(orNumber);
            if (dup == null) return;
            bool unlinked = dup["source_id"] == DBNull.Value && dup["transaction_id"] == DBNull.Value;
            bool same = dup["source_table"] as string == sourceTable && dup["source_id"] != DBNull.Value && Convert.ToInt32(dup["source_id"]) == sourceId;
            if (!unlinked && !same)
                throw new InvalidOperationException("O.R. " + orNumber.Trim() + " is already recorded for " + dup["payer"] + " (" + dup["source"] + ", " +
                                                    Convert.ToDateTime(dup["paid_at"]).ToString("dd MMM yyyy") + "). An O.R. is recorded once.");
        }

        /// <summary>
        /// A payment recorded from another module (BREQS, marriage licence). If the cashier already
        /// logged this O.R. as a walk-in, that record is linked to the module instead of a second
        /// payment being created; an O.R. already linked to something else is refused.
        /// </summary>
        public static int RecordForModule(PaymentEntry e, int? userId)
        {
            DataRow dup = FindByOr(e.OrNumber);
            if (dup == null) return Record(e, userId);
            bool unlinked = dup["source_id"] == DBNull.Value && dup["transaction_id"] == DBNull.Value;
            bool sameTarget = dup["source_table"] as string == e.SourceTable && dup["source_id"] != DBNull.Value && Convert.ToInt32(dup["source_id"]) == e.SourceId;
            if (sameTarget) return Convert.ToInt32(dup["id"]);
            if (!unlinked)
                throw new InvalidOperationException("O.R. " + e.OrNumber.Trim() + " is already recorded for " + dup["payer"] + " (" + dup["source"] + "). An O.R. is recorded once.");
            int id = Convert.ToInt32(dup["id"]);
            Db.Push("UPDATE payments SET source = @s, source_table = @t, source_id = @i WHERE id = @id",
                    P("@s", e.Source), P("@t", e.SourceTable), P("@i", e.SourceId), P("@id", id));
            Audit.Write(Audit.Update, "payments", id, "O.R. " + e.OrNumber.Trim() + " linked to " + e.SourceTable + " #" + e.SourceId);
            return id;
        }

        // ================================================================ log + report
        /// <summary>Every payment in a date range (inclusive), newest first, with what it covered.</summary>
        public static DataTable Log(DateTime from, DateTime to, string search)
        {
            string where = "p.paid_at >= @f AND p.paid_at < @t";
            var ps = new List<MySqlParameter> { P("@f", from.Date), P("@t", to.Date.AddDays(1)) };
            if (!string.IsNullOrWhiteSpace(search))
            {
                where += " AND (p.or_number LIKE @q OR COALESCE(p.payer_name, t.client_name) LIKE @q OR p.purpose LIKE @q OR t.txn_code LIKE @q)";
                ps.Add(P("@q", "%" + search.Trim() + "%"));
            }
            return Db.Pull(
                "SELECT p.id AS Id, p.paid_at AS `Paid at`, p.or_number AS `O.R. No.`, COALESCE(p.payer_name, t.client_name, '') AS Payer, " +
                "COALESCE(p.purpose, CONCAT('Transaction ', t.txn_code), '') AS Purpose, " +
                "COALESCE((SELECT GROUP_CONCAT(CONCAT(i.description, IF(i.quantity > 1, CONCAT(' x', i.quantity), '')) ORDER BY i.id SEPARATOR '; ') " +
                "          FROM payment_items i WHERE i.payment_id = p.id), '(not itemised)') AS `Fees`, " +
                "p.net_amount AS Amount, p.payment_method AS Method, p.source AS Source, COALESCE(u.full_name, u.username, '') AS Cashier " +
                "FROM payments p LEFT JOIN transactions t ON t.id = p.transaction_id LEFT JOIN users u ON u.id = p.cashier_id " +
                "WHERE " + where + " ORDER BY p.paid_at DESC, p.id DESC", ps.ToArray());
        }

        public sealed class MonthlyCollection
        {
            public int Year, Month, Payments;
            public decimal Total, Unitemised, Additional;
            public DataTable ByFee, BySource, ByMethod, Detail;
            public string Label { get { return new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture); } }
        }

        /// <summary>
        /// The month's collections: by fee (from the fee lines), by source, by method, and the list.
        /// Payments recorded before fee lines existed are counted in the total and shown as "not
        /// itemised" rather than being assigned a fee after the fact. Additional charges are their own
        /// row, so the fee rows plus those two always add up to the total.
        /// </summary>
        public static MonthlyCollection Monthly(int year, int month)
        {
            var from = new DateTime(year, month, 1);
            var ps = new[] { P("@f", from), P("@t", from.AddMonths(1)) };
            const string inMonth = "p.paid_at >= @f AND p.paid_at < @t";
            var m = new MonthlyCollection { Year = year, Month = month };
            DataTable tot = Db.Pull("SELECT COUNT(*) n, COALESCE(SUM(p.net_amount), 0) total, COALESCE(SUM(p.additional_fee), 0) addl, " +
                                    "COALESCE(SUM(CASE WHEN NOT EXISTS (SELECT 1 FROM payment_items i WHERE i.payment_id = p.id) THEN p.gross_amount ELSE 0 END), 0) unitem " +
                                    "FROM payments p WHERE " + inMonth, ps);
            m.Payments = Convert.ToInt32(tot.Rows[0]["n"]); m.Total = Convert.ToDecimal(tot.Rows[0]["total"]);
            m.Additional = Convert.ToDecimal(tot.Rows[0]["addl"]); m.Unitemised = Convert.ToDecimal(tot.Rows[0]["unitem"]);

            // Grouped by FEE CODE, named from the schedule: a fee whose description was reworded, or a line
            // written with slightly different wording by another module, is still one fee in the month.
            // A line with no code (the marriage licence O.R.) groups by its own description.
            m.ByFee = Db.Pull("SELECT COALESCE(MIN(i.fee_code), '-') AS Code, " +
                              "COALESCE((SELECT f.description FROM fees f WHERE f.code = MIN(i.fee_code)), MIN(i.description)) AS Fee, " +
                              "SUM(i.quantity) AS Quantity, COUNT(DISTINCT p.id) AS Receipts, SUM(i.line_amount) AS Amount " +
                              "FROM payment_items i JOIN payments p ON p.id = i.payment_id WHERE " + inMonth +
                              " GROUP BY COALESCE(i.fee_code, CONCAT('#', i.description)) " +
                              "ORDER BY COALESCE((SELECT f.sort_order FROM fees f WHERE f.code = MIN(i.fee_code)), 999), Fee", ps);
            if (m.Unitemised > 0) m.ByFee.Rows.Add("-", "Payments not itemised (recorded before fee lines)", DBNull.Value,
                Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payments p WHERE " + inMonth + " AND NOT EXISTS (SELECT 1 FROM payment_items i WHERE i.payment_id = p.id)", ps).Rows[0][0]), m.Unitemised);
            if (m.Additional > 0) m.ByFee.Rows.Add("-", "Additional charges", DBNull.Value,
                Convert.ToInt32(Db.Pull("SELECT COUNT(*) FROM payments p WHERE " + inMonth + " AND p.additional_fee > 0", ps).Rows[0][0]), m.Additional);

            m.BySource = Db.Pull("SELECT p.source AS Source, COUNT(*) AS Receipts, SUM(p.net_amount) AS Amount FROM payments p WHERE " + inMonth + " GROUP BY p.source ORDER BY Amount DESC", ps);
            m.ByMethod = Db.Pull("SELECT p.payment_method AS Method, COUNT(*) AS Receipts, SUM(p.net_amount) AS Amount FROM payments p WHERE " + inMonth + " GROUP BY p.payment_method ORDER BY Amount DESC", ps);
            m.Detail = Log(from, from.AddMonths(1).AddDays(-1), null);
            return m;
        }

        // ================================================================ helpers
        public static string Money(decimal? v) { return v.HasValue ? "PHP " + v.Value.ToString("#,0.00") : "not set"; }
        private static MySqlParameter P(string n, object v) { return new MySqlParameter(n, v ?? DBNull.Value); }
        private static string T(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
    }
}

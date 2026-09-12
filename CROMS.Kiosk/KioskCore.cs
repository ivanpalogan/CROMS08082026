using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace CROMS.Kiosk
{
    /// <summary>One selectable service. code + label are stored per ticket.</summary>
    public sealed class Service
    {
        public string Code;
        public string Label;
        public string Glyph;
        public string Category;
        public Service(string code, string label, string glyph, string category)
        {
            Code = code; Label = label; Glyph = glyph; Category = category;
        }
    }

    /// <summary>
    /// Shared kiosk logic used by both step forms: the service catalogue, the colour
    /// palette, the office-availability queries, and the database submit + printing +
    /// on-screen ticket. Keeping it here means the step forms only do UI + navigation.
    /// </summary>
    public static class KioskCore
    {
        // Section labels the kiosk groups the catalogue under (ServiceSelectForm draws one
        // header, styled identically, per section — see LayoutSections).
        public const string SecRegistration  = "Registration";
        public const string SecCertification = "Certification";
        public const string SecCertificates  = "Certificates & Copies";
        public const string SecMarriageFamily = "Marriage & Family";
        public const string SecPetitionsLegal = "Petitions & Legal";
        public const string SecOther         = "Other Services";

        // The service catalogue. Add a future service here — both step forms pick it up
        // (Step 1 must also add a matching card in its designer).
        // Certification groups Birth/Marriage/Death registration + CTC + Petition +
        // Verification-Other under one section regardless of what record type each one is
        // about — same reasoning as the desktop sidebar's Certification group: register the
        // event, then certify/issue it, is one intake window from the client's side of the
        // counter. Birth Registration was previously split into its own "Registration"
        // section while Marriage/Death Registration sat in Certification — an inconsistency,
        // not a deliberate distinction. SecRegistration is now unused (kept, not deleted, in
        // case a future service genuinely needs a pure-registration-only section).
        public static readonly Service[] Catalogue =
        {
            new Service("BIRTHREG", "Birth Registration", "", SecCertification),
            new Service("CTC", "Certified True Copy (CTC)", "", SecCertification),
            new Service("MARRIAGE_APP", "Marriage Application", "", SecCertification),
            new Service("MARRIAGE_REG", "Marriage Registration", "", SecCertification),
            new Service("DEATH", "Death Certificate", "", SecCertification),
            new Service("PETITION", "Petition (Correction)", "", SecCertification),
            new Service("VERIFY", "Verification / Others", "", SecCertification),
            new Service("SUPPLEMENTAL_REPORT", "Supplemental Report", "", SecCertificates),
            new Service("LEGITIMATION", "Legitimation", "", SecMarriageFamily),
            new Service("LEGITIMATION_RA9255", "Legitimation RA-9255", "", SecMarriageFamily),
            new Service("COURT_ORDER", "Court Order", "", SecPetitionsLegal),
            new Service("LEGAL_INSTRUMENTS", "Legal Instruments", "", SecPetitionsLegal),
            new Service("SUPPLEMENTAL", "Supplemental", "", SecPetitionsLegal),
            new Service("CLAIM", "Release & Claim (Pick-up)", "", SecOther),
            new Service("BREKS", "Breks", "", SecOther),
        };

        // Palette — Navy Blue (light), same tokens as CROMS/Forms/LoginForm.cs + LauncherForm.cs.
        public static readonly Color Bg          = Color.FromArgb(244, 246, 249);   // #F4F6F9
        public static readonly Color CardBg      = Color.White;
        public static readonly Color CardSelBg   = Color.FromArgb(234, 241, 254);   // accent tint #EAF1FE
        public static readonly Color Accent      = Color.FromArgb(29, 78, 216);     // #1D4ED8
        public static readonly Color AccentHover = Color.FromArgb(26, 68, 192);     // #1A44C0
        public static readonly Color Ink         = Color.FromArgb(23, 26, 36);      // #171B24
        public static readonly Color Muted       = Color.FromArgb(91, 100, 114);    // soft ink #5B6472
        public static readonly Color Line        = Color.FromArgb(225, 229, 236);   // host border #E1E5EC
        public static readonly Color Success     = Color.FromArgb(46, 148, 87);     // #2E9457 (commit action)
        public static readonly Color SuccessHover= Color.FromArgb(36, 120, 70);

        // Phosphor Light codepoints used outside the service cards (see KioskIcons for those).
        public const int IconArrowLeft   = 0xE058;
        public const int IconArrowRight  = 0xE06C;
        public const int IconPrinter     = 0xE3DC;
        public const int IconCamera      = 0xE10E;
        public const int IconRetake      = 0xE038;   // arrow-counter-clockwise
        public const int IconUser        = 0xE4C2;
        public const int IconWheelchair  = 0xE4E8;
        public const int IconBaby        = 0xE774;

        // Idle: clear + restart after this long with no input.
        // Step 2 keeps the longer window because someone may be mid-way through typing their
        // name; Step 1 holds no personal data, so it resets much sooner — that closes the gap
        // where the NEXT client walks up to the previous person's still-highlighted selections.
        public const int IdleSeconds = 90;
        public const int IdleSecondsSelect = 30;
        // Matches the staff app's window heartbeat window.
        public const int OfficeStaleMinutes = 2;

        // Government-issued IDs commonly accepted at Philippine government offices. Editable
        // combo (DropDown), so an ID not on the list can still be typed in. Same list as
        // ReleaseClaimForm.PopulateIdTypes on the staff side, kept separately since the kiosk
        // is a standalone project with no reference to CROMS.exe.
        public static readonly string[] IdTypes =
        {
            "Philippine National ID (PhilSys)",
            "Philippine Passport (DFA)",
            "Driver's License (LTO)",
            "UMID (Unified Multi-Purpose ID)",
            "SSS ID",
            "GSIS eCard",
            "PRC ID (Professional License)",
            "Voter's ID / COMELEC Certification",
            "Postal ID (PHLPost)",
            "PhilHealth ID",
            "TIN ID (BIR)",
            "Pag-IBIG Loyalty Card Plus",
            "Senior Citizen ID (OSCA)",
            "PWD ID",
            "Solo Parent ID",
            "Barangay ID / Certification (with photo)",
            "NBI Clearance",
            "Police Clearance",
            "OWWA ID / iDOLE",
            "Company / School ID",
            "Other",
        };

        public static Service Find(string code) => Catalogue.First(s => s.Code == code);

        // ---------------------------------------------- office availability
        /// <summary>True when ≥1 active window has an operator signed in with a fresh heartbeat.</summary>
        public static bool OfficeOnline()
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT COUNT(*) AS n FROM windows " +
                    "WHERE status = 'Active' AND current_operator IS NOT NULL " +
                    "AND last_heartbeat > (NOW() - INTERVAL " + OfficeStaleMinutes + " MINUTE)");
                return dt.Rows.Count > 0 && Convert.ToInt32(dt.Rows[0]["n"]) > 0;
            }
            catch { return false; }   // DB unreachable → treat the office as unavailable
        }

        /// <summary>
        /// Service codes at least one ONLINE, active window is assigned to handle. A window
        /// with no window_transactions rows (or a Priority window) handles ALL services.
        /// Empty set if the DB is unreachable.
        /// </summary>
        public static HashSet<string> AvailableServiceCodes()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DataTable online = Db.Pull(
                    "SELECT w.id, w.is_priority, " +
                    "(SELECT COUNT(*) FROM window_transactions wt WHERE wt.window_id = w.id) AS assigned " +
                    "FROM windows w WHERE w.status = 'Active' AND w.current_operator IS NOT NULL " +
                    "AND w.last_heartbeat > (NOW() - INTERVAL " + OfficeStaleMinutes + " MINUTE)");

                foreach (DataRow w in online.Rows)
                {
                    bool priority = w["is_priority"] != DBNull.Value && Convert.ToInt32(w["is_priority"]) == 1;
                    int assigned = Convert.ToInt32(w["assigned"]);
                    if (priority || assigned == 0)
                    {
                        foreach (var svc in Catalogue) result.Add(svc.Code);
                    }
                    else
                    {
                        DataTable codes = Db.Pull(
                            "SELECT service_code FROM window_transactions WHERE window_id = " + w["id"]);
                        foreach (DataRow c in codes.Rows) result.Add(c["service_code"].ToString());
                    }
                }
            }
            catch { /* DB unreachable → nothing available */ }
            // Existing counter assignments remain usable when upgrading from the old
            // broad registration/marriage cards. New tickets retain their distinct keys.
            if (result.Remove("NEWREG")) result.Add("BIRTHREG");
            if (result.Remove("MARRIAGE"))
            {
                result.Add("MARRIAGE_APP");
                result.Add("MARRIAGE_REG");
            }
            return result;
        }

        // ------------------------------------------------------- identity
        public static string FullName(KioskSession s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.First)) parts.Add(s.First.Trim());
            if (!string.IsNullOrWhiteSpace(s.Middle)) parts.Add(s.Middle.Trim());
            if (!string.IsNullOrWhiteSpace(s.Last)) parts.Add(s.Last.Trim());
            return string.Join(" ", parts);
        }

        /// <summary>The spouse's name (Marriage Application / Marriage Registration only).</summary>
        public static string FullName2(KioskSession s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.First2)) parts.Add(s.First2.Trim());
            if (!string.IsNullOrWhiteSpace(s.Middle2)) parts.Add(s.Middle2.Trim());
            if (!string.IsNullOrWhiteSpace(s.Last2)) parts.Add(s.Last2.Trim());
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Maps the three priority flags onto the single `priority` column. Strongest wins;
        /// order matches FIELD(priority,'Priority','PWD','Senior','Regular'). Pregnant → Priority.
        /// </summary>
        public static string PriorityValue(KioskSession s)
        {
            if (s.Pregnant) return "Priority";
            if (s.Pwd) return "PWD";
            if (s.Senior) return "Senior";
            return "Regular";
        }

        // -------------------------------------------------------- claim QR
        /// <summary>
        /// Creates the claim_requests row (once) and stores its token + ticket number on the
        /// session. Shown as the "Upload Your ID" QR on EVERY visit's Step 2 now, not only a
        /// Release &amp; Claim pickup — request_details states what this particular visit is
        /// actually for, so an office reading claim_requests later can still tell them apart.
        /// Best-effort — never blocks the queue ticket.
        /// </summary>
        public static void EnsureClaimRequest(KioskSession s)
        {
            if (s.ClaimQrToken != null) return;
            try
            {
                string token = Guid.NewGuid().ToString("N");
                string ticketNo = NextClaimNo();
                string details = s.Selected.Count > 0
                    ? string.Join(", ", s.Selected.Select(c => Find(c).Label))
                    : "Identity Verification";
                if (details.Length > 255) details = details.Substring(0, 255);
                Db.Push(
                    "INSERT INTO claim_requests " +
                    "(qr_token, claim_ticket_no, first_name, middle_name, last_name, request_details, status) " +
                    "VALUES (@t, @tk, @f, @m, @l, @d, 'Pending')",
                    new MySqlParameter("@t", token),
                    new MySqlParameter("@tk", ticketNo),
                    new MySqlParameter("@f", NullIfBlank(s.First)),
                    new MySqlParameter("@m", NullIfBlank(s.Middle)),
                    new MySqlParameter("@l", NullIfBlank(s.Last)),
                    new MySqlParameter("@d", details));
                s.ClaimQrToken = token;
                s.ClaimQrNo = ticketNo;
            }
            catch { /* leave token null → the UI shows a friendly note */ }
        }

        private static void FinalizeClaimRow(KioskSession s, long txnId, long queueTicketId)
        {
            if (s.ClaimQrToken == null) return;
            try
            {
                // queue_ticket_id links the claim to the kiosk ticket that holds the client's
                // face photo, so the Claim Form shows it even when no transaction is set yet.
                Db.Push(
                    "UPDATE claim_requests SET first_name = @f, middle_name = @m, last_name = @l, " +
                    "transaction_id = @txn, queue_ticket_id = @qt WHERE qr_token = @t",
                    new MySqlParameter("@f", NullIfBlank(s.First)),
                    new MySqlParameter("@m", NullIfBlank(s.Middle)),
                    new MySqlParameter("@l", NullIfBlank(s.Last)),
                    new MySqlParameter("@txn", txnId > 0 ? (object)txnId : DBNull.Value),
                    new MySqlParameter("@qt", queueTicketId > 0 ? (object)queueTicketId : DBNull.Value),
                    new MySqlParameter("@t", s.ClaimQrToken));
            }
            catch { /* best-effort */ }
        }

        private static string NextClaimNo()
        {
            string year = DateTime.Now.Year.ToString();
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(claim_ticket_no,'-',-1) AS UNSIGNED)),0)+1 AS n " +
                "FROM claim_requests WHERE claim_ticket_no LIKE 'CLM-" + year + "-%'");
            int n = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return "CLM-" + year + "-" + n.ToString("D4");
        }

        // ------------------------------------------------------- queue math
        /// <summary>
        /// Resolves a queue number the client typed (e.g. "Q-006", "006", "6") to the id of
        /// the PARKED transaction it belongs to (status WaitingToRelease / ForPrint). 0 if none.
        /// </summary>
        public static long ResolveParkedByQueue(string entered)
        {
            try
            {
                int n = 0;
                int.TryParse(new string(entered.Where(char.IsDigit).ToArray()), out n);
                DataTable dt = Db.Pull(
                    "SELECT t.id FROM queue_tickets qt " +
                    "JOIN transactions t ON t.id = qt.transaction_id " +
                    "WHERE ( qt.ticket_code = @raw OR (@n > 0 AND qt.number_queue = @n) ) " +
                    "AND t.status IN ('WaitingToRelease','ForPrint') " +
                    "ORDER BY qt.id DESC LIMIT 1",
                    new MySqlParameter("@raw", entered),
                    new MySqlParameter("@n", n));
                return dt.Rows.Count > 0 ? Convert.ToInt64(dt.Rows[0]["id"]) : 0;
            }
            catch { return 0; }
        }

        /// <summary>Next queue number — CONTINUOUS across all days (never resets).</summary>
        private static int NextNum()
        {
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(number_queue), 0) + 1 AS n FROM queue_tickets " +
                "WHERE ticket_code LIKE 'Q-%'");
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
        }

        /// <summary>Still-Waiting clients called before this ticket; -1 if unreadable.</summary>
        private static int AheadCount(long selfId, string priority, int num)
        {
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT COUNT(*) AS n FROM queue_tickets " +
                    "WHERE DATE(created_at) = CURDATE() AND status = 'Waiting' AND id <> @self " +
                    "AND ( FIELD(priority,'Priority','PWD','Senior','Regular') < " +
                    "        FIELD(@p,'Priority','PWD','Senior','Regular') " +
                    "   OR ( priority = @p AND number_queue < @num ) )",
                    new MySqlParameter("@self", selfId),
                    new MySqlParameter("@p", priority),
                    new MySqlParameter("@num", num));
                return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["n"]) : 0;
            }
            catch { return -1; }
        }

        // ---------------------------------------------------------- submit
        /// <summary>
        /// Writes the whole request to the shared database (one queue_tickets row + one
        /// queue_ticket_services row per service), handles Release &amp; Claim reclaim/QR, then
        /// prints the thermal ticket and shows the on-screen confirmation. Returns false (and
        /// re-shows nothing) on a validation miss the caller should surface; throws only on DB error.
        /// </summary>
        public static bool Submit(KioskSession s, out string error)
        {
            error = null;
            if (s.Selected.Count == 0) { error = "Please select at least one service."; return false; }
            if (string.IsNullOrWhiteSpace(s.First) || string.IsNullOrWhiteSpace(s.Last))
            { error = "Please enter your first and last name."; return false; }
            if (s.HasMarriage && (string.IsNullOrWhiteSpace(s.First2) || string.IsNullOrWhiteSpace(s.Last2)))
            { error = "Please enter the spouse's first and last name."; return false; }

            // Returning-client pickup: a typed queue number that maps to a parked request is
            // a reclaim — link the new ticket to that transaction and jump the queue.
            long returnTxnId = 0;
            if (s.HasClaim && !string.IsNullOrWhiteSpace(s.ClaimTicketEntry))
            {
                returnTxnId = ResolveParkedByQueue(s.ClaimTicketEntry.Trim());
                if (returnTxnId == 0)
                {
                    error = "That queue number was not found among held requests. Check the Q-number " +
                            "on your ticket, or leave it blank to start a new claim.";
                    return false;
                }
            }

            string priority = returnTxnId != 0 ? "Priority" : PriorityValue(s);
            string joined = TicketSummary(s.Selected.Select(c => Find(c).Label).ToList());
            string primary = Find(s.Selected[0]).Label;

            int num = NextNum();
            string code = "Q-" + num.ToString("D3");
            object contact = NullIfBlank(s.Contact);

            // Spouse fields are only meaningful for Marriage Application / Marriage
            // Registration — every other service leaves them NULL.
            object spouseName = s.HasMarriage ? (object)FullName2(s) : DBNull.Value;

            long ticketId = Db.Insert(
                "INSERT INTO queue_tickets (ticket_code, full_name, spouse_full_name, contact_no, " +
                "id_image, spouse_image, valid_id_type, number_queue, " +
                "date, time, status, document_type, type_label, priority) " +
                "VALUES (@code, @name, @sname, @contact, @img, @simg, @idtype, @num, @date, @time, " +
                "'Waiting', @doc, @label, @priority)",
                new MySqlParameter("@code", code),
                new MySqlParameter("@name", FullName(s)),
                new MySqlParameter("@sname", spouseName),
                new MySqlParameter("@contact", contact),
                ImageParam(s.Photo),
                ImageParam(s.HasMarriage ? s.Photo2 : null, "@simg"),
                new MySqlParameter("@idtype", NullIfBlank(s.IdType)),
                new MySqlParameter("@num", num),
                new MySqlParameter("@date", DateTime.Today),
                new MySqlParameter("@time", DateTime.Now.ToString("HH:mm")),
                new MySqlParameter("@doc", primary),
                new MySqlParameter("@label", joined),
                new MySqlParameter("@priority", priority));

            foreach (string c in s.Selected)
            {
                Service svc = Find(c);
                Db.Push(
                    "INSERT INTO queue_ticket_services (ticket_id, service_code, service_label) " +
                    "VALUES (@tid, @sc, @sl)",
                    new MySqlParameter("@tid", ticketId),
                    new MySqlParameter("@sc", svc.Code),
                    new MySqlParameter("@sl", svc.Label));
            }

            if (returnTxnId != 0)
                Db.Push("UPDATE queue_tickets SET transaction_id = @txn WHERE id = @tid",
                    new MySqlParameter("@txn", returnTxnId),
                    new MySqlParameter("@tid", ticketId));

            // Every visit shows the "Upload Your ID" QR on Step 2 (DetailsPhotoForm.Load already
            // called EnsureClaimRequest by the time we get here), so finalize it for all of
            // them — not only when CLAIM was the selected service.
            EnsureClaimRequest(s);
            string claimToken = s.ClaimQrToken, claimNo = s.ClaimQrNo;
            FinalizeClaimRow(s, returnTxnId, ticketId);

            var services = s.Selected.Select(c => Find(c).Label).ToList();
            int ahead = AheadCount(ticketId, priority, num);
            string spouseLine = s.HasMarriage ? FullName2(s) : null;
            PrintTicket(code, services, priority, FullName(s), spouseLine, ahead, claimToken, claimNo);
            ShowTicket(code, services, claimToken, claimNo);
            return true;
        }

        // type_label is VARCHAR(255). All services are stored in full in the child rows;
        // only this queue-list summary is shortened when a large selection exceeds it.
        private static string TicketSummary(List<string> labels)
        {
            string full = string.Join(", ", labels);
            if (full.Length <= 255) return full;
            for (int count = labels.Count - 1; count > 0; count--)
            {
                string summary = string.Join(", ", labels.Take(count)) +
                    " (+" + (labels.Count - count) + " more)";
                if (summary.Length <= 255) return summary;
            }
            return labels.Count + " services";
        }

        private static object NullIfBlank(string v) =>
            string.IsNullOrWhiteSpace(v) ? (object)DBNull.Value : v.Trim();

        private static MySqlParameter ImageParam(byte[] photo, string paramName = "@img")
        {
            var p = new MySqlParameter(paramName, MySqlDbType.LongBlob);
            p.Value = (photo == null || photo.Length == 0) ? (object)DBNull.Value : photo;
            return p;
        }

        // -------------------------------------------------------- printing
        private static void PrintTicket(string code, List<string> services, string priorityLane,
            string name, string spouseName, int ahead, string claimToken, string claimNo)
        {
            try
            {
                using (var doc = new PrintDocument())
                {
                    int h = 330 + services.Count * 24 + (priorityLane != "Regular" ? 36 : 0)
                            + (claimToken != null ? 210 : 0) + (!string.IsNullOrEmpty(spouseName) ? 18 : 0);
                    doc.DefaultPageSettings.PaperSize = new PaperSize("Q58", 228, h); // 58mm wide
                    doc.DefaultPageSettings.Margins = new Margins(8, 8, 10, 10);
                    doc.DocumentName = "Queue Ticket " + code;
                    doc.PrintPage += (s, e) => DrawTicket(e, code, services, priorityLane, name, spouseName, ahead, claimToken, claimNo);
                    doc.Print();
                }
            }
            catch { /* no printer / cancelled — on-screen ticket still shows the number */ }
        }

        private static void DrawTicket(PrintPageEventArgs e, string code, List<string> services,
            string priorityLane, string name, string spouseName, int ahead, string claimToken, string claimNo)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left;
            float width = e.MarginBounds.Width;
            float y = e.MarginBounds.Top;

            using (var fOffice = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fSub    = new Font("Segoe UI", 7.5F))
            using (var fLabel  = new Font("Segoe UI", 8F, FontStyle.Bold))
            using (var fNumber = new Font("Consolas", 30F, FontStyle.Bold))
            using (var fBody   = new Font("Segoe UI", 8.5F))
            using (var fSmall  = new Font("Segoe UI", 7.5F))
            using (var fBadge  = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var centre  = new StringFormat { Alignment = StringAlignment.Center })
            {
                Action<string, Font> mid = (text, font) =>
                {
                    SizeF sz = g.MeasureString(text, font, (int)width);
                    g.DrawString(text, font, Brushes.Black, new RectangleF(left, y, width, sz.Height), centre);
                    y += sz.Height + 2;
                };
                Action<string, Font> row = (text, font) =>
                {
                    SizeF sz = g.MeasureString(text, font, (int)width);
                    g.DrawString(text, font, Brushes.Black, new RectangleF(left, y, width, sz.Height));
                    y += sz.Height + 2;
                };
                Action rule = () =>
                {
                    using (var pen = new Pen(Color.Black)) g.DrawLine(pen, left, y + 2, left + width, y + 2);
                    y += 8;
                };

                if (priorityLane != "Regular")
                {
                    mid("★ PRIORITY LANE ★", fBadge);
                    mid("(" + priorityLane + ")", fSmall);
                }

                mid("CROMS — LCRO Peñablanca", fOffice);
                mid("Local Civil Registry Office", fSub);
                mid("Municipality of Peñablanca", fSub);
                rule();

                mid("QUEUE NUMBER", fLabel);
                mid(code, fNumber);
                rule();

                if (!string.IsNullOrEmpty(name)) row("Name:  " + name, fBody);
                if (!string.IsNullOrEmpty(spouseName)) row("Spouse:  " + spouseName, fBody);
                row("Date:  " + DateTime.Now.ToString("ddd, dd MMM yyyy"), fBody);
                row("Time:  " + DateTime.Now.ToString("hh:mm tt"), fBody);
                rule();

                row("Services requested:", fBody);
                int i = 1;
                foreach (string svc in services) { row("  " + i + ". " + svc, fBody); i++; }
                rule();

                if (ahead >= 0)
                {
                    mid("People ahead of you: " + ahead, fLabel);
                    rule();
                }

                mid("Please wait for your number", fSmall);
                mid("to be called. Keep this ticket.", fSmall);

                if (!string.IsNullOrEmpty(claimToken))
                {
                    rule();
                    mid("UPLOAD YOUR ID", fLabel);
                    if (!string.IsNullOrEmpty(claimNo)) mid("Ticket: " + claimNo, fSmall);
                    Bitmap qr = QrHelper.TryCreate(ClaimLink.Build(claimToken), 6);
                    if (qr != null)
                    {
                        float size = Math.Min(width, 150);
                        g.DrawImage(qr, left + (width - size) / 2, y, size, size);
                        y += size + 4;
                        qr.Dispose();
                    }
                    else
                    {
                        mid(claimToken, fSmall);
                    }
                    mid("Scan with your phone camera to upload your ID.", fSmall);
                }
            }
        }

        // ------------------------------------------------ on-screen ticket
        private static void ShowTicket(string code, List<string> services, string claimToken, string claimNo)
        {
            bool claim = !string.IsNullOrEmpty(claimToken);
            using (var dlg = new Form())
            {
                dlg.Text = claim ? "Upload Your ID + Queue Number" : "Your Queue Number";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(480, claim ? 720 : 420);
                dlg.BackColor = Color.FromArgb(17, 24, 39);

                var ok = new Button
                {
                    Text = "OK",
                    Dock = DockStyle.Bottom, Height = 48,
                    FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
                    BackColor = Color.FromArgb(13, 110, 253),
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold)
                };
                ok.Click += (s, e) => dlg.Close();

                var note = new Label
                {
                    Text = "Please take a seat and wait for your number to be called.",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 10F),
                    Dock = DockStyle.Bottom, Height = 44,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                Panel claimPanel = null;
                if (claim)
                {
                    claimPanel = new Panel { Dock = DockStyle.Bottom, Height = 300, BackColor = Color.White };
                    claimPanel.Controls.Add(new Label
                    {
                        Text = "SCAN WITH YOUR PHONE CAMERA TO UPLOAD YOUR ID" +
                               (string.IsNullOrEmpty(claimNo) ? "" : "\nClaim Ticket: " + claimNo),
                        Dock = DockStyle.Top, Height = 60, TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(33, 37, 41)
                    });
                    var pic = new PictureBox
                    {
                        Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
                        Image = QrHelper.TryCreate(ClaimLink.Build(claimToken), 10),
                        Padding = new Padding(8)
                    };
                    if (pic.Image == null)
                        claimPanel.Controls.Add(new Label
                        {
                            Text = "Token:\n" + claimToken, Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Consolas", 9F)
                        });
                    else claimPanel.Controls.Add(pic);
                }

                var list = new Label
                {
                    Text = "Services:\n" + string.Join("\n", services.Select(s => "•  " + s)),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 12F),
                    Dock = DockStyle.Top,
                    Height = 32 + services.Count * 28,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var serviceList = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                serviceList.Controls.Add(list);
                var big = new Label
                {
                    Text = code,
                    ForeColor = Color.FromArgb(96, 165, 250),
                    Font = new Font("Consolas", 60F, FontStyle.Bold),
                    Dock = DockStyle.Top, Height = 110,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var header = new Label
                {
                    Text = "YOUR QUEUE NUMBER",
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    Dock = DockStyle.Top, Height = 50,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                dlg.Controls.Add(serviceList);
                dlg.Controls.Add(ok);
                dlg.Controls.Add(note);
                if (claimPanel != null) dlg.Controls.Add(claimPanel);
                dlg.Controls.Add(big);
                dlg.Controls.Add(header);
                dlg.ShowDialog();
            }
        }
    }
}

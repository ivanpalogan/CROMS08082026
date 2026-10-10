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
        public const string SecMarriageFamily = "Marriage & Family";
        public const string SecCertificates  = "Copies & Pick-up";
        public const string SecPetitionsLegal = "Petitions & Legal";
        // Unused today. Kept, not deleted, so a future service that belongs to neither of the
        // four sections above has a home without re-deciding the whole grouping.
        public const string SecCertification = "Certification";
        public const string SecOther         = "Other Services";

        // The service catalogue. Add a future service here and Step 1 draws a card for it —
        // ServiceSelectForm builds every card FROM this array, so there is no second list to
        // keep in step.
        //
        // The grouping answers the client's own question at the counter, not the office's
        // internal module layout: REGISTER an event / apply for and record a MARRIAGE /
        // obtain or collect a COPY / file a correction or legal case. Each section is a
        // whole row of cards on screen, which is why the counts are kept even (3/3/3/4).
        public static readonly Service[] Catalogue =
        {
            new Service("BIRTHREG", "Birth Registration", "", SecRegistration),
            new Service("MARRIAGE_REG", "Marriage Registration", "", SecRegistration),
            // Code is DEATH, and it routes to the death REGISTRATION module (MainForm's
            // service map) — the old "Death Certificate" caption said the opposite of what
            // the card does, and sat beside two cards captioned "... Registration".
            new Service("DEATH", "Death Registration", "", SecRegistration),

            new Service("MARRIAGE_APP", "Marriage Application", "", SecMarriageFamily),
            new Service("LEGITIMATION", "Legitimation", "", SecMarriageFamily),
            new Service("LEGITIMATION_RA9255", "Legitimation RA-9255", "", SecMarriageFamily),

            new Service("CTC", "Certified True Copy (CTC)", "", SecCertificates),
            new Service("BREQS", "PSA Copy (BREQS)", "", SecCertificates),
            new Service("CLAIM", "Release & Claim (Pick-up)", "", SecCertificates),

            new Service("PETITION", "Petition (Correction)", "", SecPetitionsLegal),
            new Service("SUPPLEMENTAL_REPORT", "Supplemental Report", "", SecPetitionsLegal),
            new Service("LEGAL_INSTRUMENTS", "Legal Instruments", "", SecPetitionsLegal),
            new Service("COURT_ORDER", "Court Order", "", SecPetitionsLegal),
        };
        // DROPPED FROM THE KIOSK, both on 2026-09-19:
        //   VERIFY  "Verification / Others" — a catch-all the client cannot act on: it named
        //           no document and no outcome, so the ticket reached a window with nothing
        //           stated. Staff-side mappings for it are LEFT IN PLACE (MainForm,
        //           QueueManagementForm, WindowAssignmentForm) so tickets already issued
        //           under it still route and still resolve.
        //   SUPPLEMENTAL "Supplemental" — the same office case type as SUPPLEMENTAL_REPORT
        //           (migration 44's `SupplementalReport`), offered twice under two different
        //           sections. Both codes map to the petitions module; the report-named one is
        //           kept because it matches the petition type the desktop actually stores.

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
        public static bool OfficeOnline() => ComputeOfficeState().Open;

        /// <summary>
        /// Service codes at least one ONLINE, active window is assigned to handle. A window
        /// with no window_service_assignments rows (or a Priority window) handles ALL services.
        /// Empty set if the DB is unreachable.
        /// </summary>
        public static HashSet<string> AvailableServiceCodes() => ComputeOfficeState().Codes;

        /// <summary>Whether the office is open and which services it can take right now.</summary>
        public sealed class OfficeState
        {
            public bool Open;
            public HashSet<string> Codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly object _stateLock = new object();
        private static OfficeState _lastState;
        private static DateTime _lastStateAt = DateTime.MinValue;

        /// <summary>Last answer the database gave, or null before the first one. Reading it never
        /// touches the network, so a screen can paint from it the instant it opens.</summary>
        public static OfficeState LastKnownOfficeState => _lastState;

        /// <summary>
        /// Asks the database on a background thread and hands the answer back on <paramref name="ui"/>'s
        /// thread. The kiosk used to run these queries on the UI thread — on a Wi-Fi link to the server
        /// laptop every poll froze the screen for as long as the round trip took, which is exactly what
        /// a client feels as "I tapped and nothing happened".
        /// </summary>
        public static void RefreshOfficeStateAsync(Control ui, Action<OfficeState> done)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                OfficeState st = ComputeOfficeState();
                if (done == null || ui == null) return;
                try
                {
                    if (ui.IsHandleCreated && !ui.IsDisposed)
                        ui.BeginInvoke((Action)(() => { if (!ui.IsDisposed) done(st); }));
                }
                catch { /* form closed while the query was running */ }
            });
        }

        private static OfficeState ComputeOfficeState()
        {
            lock (_stateLock)
            {
                // Three screens poll on their own timers; one answer is good for a moment.
                if (_lastState != null && (DateTime.UtcNow - _lastStateAt).TotalMilliseconds < 1500)
                    return _lastState;

                var st = new OfficeState();
                try
                {
                    DataTable online = Db.Pull(
                        "SELECT w.id, w.is_priority, " +
                        "(SELECT COUNT(*) FROM window_service_assignments wt WHERE wt.window_id = w.id) AS assigned " +
                        "FROM windows w WHERE w.status = 'Active' AND w.current_operator IS NOT NULL " +
                        "AND w.last_heartbeat > (NOW() - INTERVAL " + OfficeStaleMinutes + " MINUTE)");
                    st.Open = online.Rows.Count > 0;

                    DataTable assignments = null;   // fetched once, and only if some window needs it
                    foreach (DataRow w in online.Rows)
                    {
                        bool priority = w["is_priority"] != DBNull.Value && Convert.ToInt32(w["is_priority"]) == 1;
                        int assigned = Convert.ToInt32(w["assigned"]);
                        if (priority || assigned == 0)
                        {
                            foreach (var svc in Catalogue) st.Codes.Add(svc.Code);
                        }
                        else
                        {
                            if (assignments == null)
                                assignments = Db.Pull("SELECT window_id, service_code FROM window_service_assignments");
                            foreach (DataRow c in assignments.Select("window_id = " + w["id"]))
                                st.Codes.Add(c["service_code"].ToString());
                        }
                    }
                }
                catch { st.Open = false; st.Codes.Clear(); }   // DB unreachable → closed, nothing available

                // Existing counter assignments remain usable when upgrading from the old
                // broad registration/marriage cards. New tickets retain their distinct keys.
                if (st.Codes.Remove("NEWREG")) st.Codes.Add("BIRTHREG");
                if (st.Codes.Remove("MARRIAGE"))
                {
                    st.Codes.Add("MARRIAGE_APP");
                    st.Codes.Add("MARRIAGE_REG");
                }
                _lastState = st;
                _lastStateAt = DateTime.UtcNow;
                return st;
            }
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


        // ------------------------------------------------------- queue math
        /// <summary>
        /// Resolves a queue number the client typed (e.g. "Q-006", "006", "6") to the id of
        /// the PARKED transaction it belongs to (status WaitingToRelease / ForPrint). 0 if none.
        /// Requires the name typed at THIS kiosk visit to plausibly match the parked
        /// transaction's own client_name — a bare number match is not ownership, and without
        /// this a guessed/leftover queue number could silently reclaim a stranger's parked
        /// request and jump them to Priority.
        /// </summary>
        public static long ResolveParkedByQueue(string entered, string clientName)
        {
            try
            {
                int n = 0;
                int.TryParse(new string(entered.Where(char.IsDigit).ToArray()), out n);
                DataTable dt = Db.Pull(
                    "SELECT t.id, t.client_name FROM queue_tickets qt " +
                    "JOIN transactions t ON t.id = qt.transaction_id " +
                    "WHERE ( qt.ticket_code = @raw OR (@n > 0 AND qt.number_queue = @n) ) " +
                    "AND t.status IN ('WaitingToRelease','ForPrint') " +
                    "ORDER BY qt.id DESC LIMIT 1",
                    new MySqlParameter("@raw", entered),
                    new MySqlParameter("@n", n));
                if (dt.Rows.Count == 0) return 0;
                if (!NamesPlausiblyMatch(clientName, Convert.ToString(dt.Rows[0]["client_name"])))
                    return 0;
                return Convert.ToInt64(dt.Rows[0]["id"]);
            }
            catch { return 0; }
        }

        /// <summary>
        /// Loose ownership check: every significant (3+ letter) word the client typed today
        /// must appear somewhere in the parked transaction's stored name, case/diacritic-
        /// insensitive. Deliberately loose (order-independent, no exact match required) so a
        /// genuine returning client isn't refused over "Dela Cruz" vs "dela cruz, Jose" — but a
        /// name typed with no real relation to the parked record's name is refused.
        /// </summary>
        private static bool NamesPlausiblyMatch(string typed, string onFile)
        {
            if (string.IsNullOrWhiteSpace(typed) || string.IsNullOrWhiteSpace(onFile)) return false;
            Func<string, string[]> words = s => s.ToUpperInvariant()
                .Split(new[] { ' ', ',', '.', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3).ToArray();
            string[] a = words(typed);
            string onFileUpper = onFile.ToUpperInvariant();
            if (a.Length == 0) return false;
            return a.All(w => onFileUpper.Contains(w));
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
            if (!Validate(s, out error)) return false;

            // Returning-client pickup: a typed queue number that maps to a parked request is
            // a reclaim — link the new ticket to that transaction and jump the queue.
            long returnTxnId = 0;
            if (s.HasClaim && !string.IsNullOrWhiteSpace(s.ClaimTicketEntry))
                returnTxnId = ResolveParkedByQueue(s.ClaimTicketEntry.Trim(), FullName(s));

            string priority = returnTxnId != 0 ? "Priority" : PriorityValue(s);
            string joined = TicketSummary(s.Selected.Select(c => Find(c).Label).ToList());
            string primary = Find(s.Selected[0]).Label;

            int num = NextNum();
            string code = "Q-" + num.ToString("D3");
            object contact = NullIfBlank(s.Contact);

            // Spouse fields are only meaningful for Marriage Application — every other
            // service (Marriage Registration included) leaves them NULL. Registration's
            // couple was already captured by the license photo / signed certificate; asking
            // again at the kiosk would just duplicate it (see KioskSession.HasMarriageApp).
            object spouseName = s.HasMarriageApp ? (object)FullName2(s) : DBNull.Value;

            long ticketId = Db.Insert(
                "INSERT INTO queue_tickets (ticket_code, full_name, spouse_full_name, contact_no, " +
                "id_image, spouse_image, marriage_license_image, submitted_by, submitted_by_org, valid_id_type, number_queue, " +
                "date, time, status, document_type, purpose, type_label, priority) " +
                "VALUES (@code, @name, @sname, @contact, @img, @simg, @limg, @subby, @suborg, @idtype, @num, @date, @time, " +
                "'Waiting', @doc, @purpose, @label, @priority)",
                new MySqlParameter("@code", code),
                new MySqlParameter("@name", FullName(s)),
                new MySqlParameter("@sname", spouseName),
                new MySqlParameter("@contact", contact),
                ImageParam(s.Photo),
                ImageParam(s.HasMarriageApp ? s.Photo2 : null, "@simg"),
                // The kiosk no longer photographs the Marriage License - staff capture it with
                // Mobile Capture at the window. The column stays for tickets issued before.
                ImageParam(null, "@limg"),
                // Who is submitting the Certificate of Marriage (migration 67) - Marriage
                // Registration only; MarriageEntryForm.PrepareForQueueTicket copies it to Form 97.
                new MySqlParameter("@subby", s.Selected.Contains("MARRIAGE_REG") ? NullIfBlank(s.SubmittedBy) : DBNull.Value),
                new MySqlParameter("@suborg", s.Selected.Contains("MARRIAGE_REG") && s.SubmittedBy == "Representative"
                    ? NullIfBlank(s.SubmittedByOrg) : DBNull.Value),
                new MySqlParameter("@idtype", NullIfBlank(s.IdType)),
                new MySqlParameter("@num", num),
                new MySqlParameter("@date", DateTime.Today),
                new MySqlParameter("@time", DateTime.Now.ToString("HH:mm")),
                new MySqlParameter("@doc", s.HasCtc ? (object)s.CtcDocumentType : primary),
                // One readable line for the screens that only have room for one (the live queue
                // grid, the Now Serving card). The full structured request is in kiosk_ctc_intake.
                new MySqlParameter("@purpose", NullIfBlank(
                    s.HasCtc ? CtcSummary(s)
                    : s.HasMarriageLicense ? "Has Marriage License - hand it to staff"
                    : null)),
                new MySqlParameter("@label", joined),
                new MySqlParameter("@priority", priority));

            // Marriage Application: keep both applicants' three name cells as typed so staff's
            // Form 90 window auto-fills them (migration 76). A separate UPDATE so the ticket
            // itself still saves on a database that has not applied the migration yet.
            if (s.HasMarriageApp)
            {
                try
                {
                    Db.Push(
                        "UPDATE queue_tickets SET app_h_first=@hf, app_h_middle=@hm, app_h_last=@hl, " +
                        "app_w_first=@wf, app_w_middle=@wm, app_w_last=@wl WHERE id=@id",
                        new MySqlParameter("@hf", NullIfBlank(s.First)), new MySqlParameter("@hm", NullIfBlank(s.Middle)),
                        new MySqlParameter("@hl", NullIfBlank(s.Last)),
                        new MySqlParameter("@wf", NullIfBlank(s.First2)), new MySqlParameter("@wm", NullIfBlank(s.Middle2)),
                        new MySqlParameter("@wl", NullIfBlank(s.Last2)),
                        new MySqlParameter("@id", ticketId));
                }
                catch (MySqlException ex) when (ex.Number == 1054) { /* migration 76 not applied */ }
            }

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

            if (s.HasBreqs) SaveBreqsRequest(s, ticketId, code);
            if (s.HasCtc) SaveCtcRequest(s, ticketId);

            var services = s.Selected.Select(c => Find(c).Label).ToList();
            int ahead = AheadCount(ticketId, priority, num);
            string spouseLine = s.HasMarriageApp ? FullName2(s) : null;
            PrintTicket(code, services, priority, FullName(s), spouseLine, ahead);
            ShowTicket(code, services);
            return true;
        }

        public static bool Validate(KioskSession s, out string error)
        {
            error = null;
            if (s.Selected.Count == 0) { error = "Please select at least one service."; return false; }
            if (string.IsNullOrWhiteSpace(s.First) || string.IsNullOrWhiteSpace(s.Last))
            { error = "Please enter your first and last name."; return false; }
            if (s.HasMarriageApp && (string.IsNullOrWhiteSpace(s.First2) || string.IsNullOrWhiteSpace(s.Last2)))
            { error = "Please enter the spouse's first and last name."; return false; }
            if (s.Selected.Contains("MARRIAGE_REG") && string.IsNullOrWhiteSpace(s.SubmittedBy))
            { error = "Please tell us who is submitting the Certificate of Marriage (Solemnizing Officer, Husband, Wife or Others)."; return false; }
            if (s.Selected.Contains("MARRIAGE_REG") && s.SubmittedBy == "Representative" && string.IsNullOrWhiteSpace(s.SubmittedByOrg))
            { error = "You chose Others - please specify who you are (for example: relative, wedding coordinator)."; return false; }
            // Checked BEFORE the ticket exists, so a half-filled PSA request never leaves a
            // ticket behind with no request for staff to find.
            if (s.HasBreqs && (error = BreqsProblem(s)) != null) return false;
            if (s.HasCtc && string.IsNullOrWhiteSpace(s.CtcDocumentType))
            { error = "Please choose the civil registry document for the Certified True Copy request."; return false; }
            if (s.HasCtc && (string.IsNullOrWhiteSpace(s.CtcOwnerFirst) || string.IsNullOrWhiteSpace(s.CtcOwnerLast)))
            { error = s.CtcDocumentType == "Marriage"
                  ? "Please enter the husband's first and last name."
                  : "Please enter the first and last name on the record you need a certified true copy of."; return false; }
            if (s.HasCtc && s.CtcDocumentType == "Marriage"
                && (string.IsNullOrWhiteSpace(s.CtcSpouseFirst) || string.IsNullOrWhiteSpace(s.CtcSpouseLast)))
            { error = "Please enter the wife's first and last name."; return false; }
            if (s.HasCtc && string.IsNullOrWhiteSpace(s.CtcEventProvince))
            { error = "Please select a province."; return false; }
            if (s.HasCtc && string.IsNullOrWhiteSpace(s.CtcEventCity))
            { error = "Please select a city or municipality."; return false; }

            // Returning-client pickup: a typed queue number that maps to a parked request is
            // a reclaim — link the new ticket to that transaction and jump the queue.
            long returnTxnId = 0;
            if (s.HasClaim && !string.IsNullOrWhiteSpace(s.ClaimTicketEntry))
            {
                returnTxnId = ResolveParkedByQueue(s.ClaimTicketEntry.Trim(), FullName(s));
                if (returnTxnId == 0)
                {
                    error = "That queue number was not found among your held requests. Check the Q-number " +
                            "on your ticket and that your name matches the earlier visit, or leave it blank " +
                            "to start a new claim.";
                    return false;
                }
            }

            return true;
        }

        // ------------------------------------------------ Certified True Copy (local record)
        /// <summary>
        /// The request in one line, for the places that only have room for one — "Birth CTC ·
        /// 2 copies · SHELLIAN CLEAR TALOSIG · 2018-06-12 · Reg 2018-4555". Built from whatever
        /// the client actually gave, so a sparse request stays short rather than padding with
        /// empty separators.
        /// </summary>
        /// <summary>Writes a technical error to a log file next to the temp folder. Never shown to a
        /// client and never throws - a failure to log must not make things worse.</summary>
        public static void LogError(Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "croms-kiosk-error.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + ex + Environment.NewLine + Environment.NewLine);
            }
            catch { }
        }

        public static string CtcSummary(KioskSession s)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(s.CtcDocumentType)) parts.Add(s.CtcDocumentType + " CTC");
            if (s.CtcCopies > 1) parts.Add(s.CtcCopies + " copies");
            string owner = Join(s.CtcOwnerFirst, s.CtcOwnerMiddle, s.CtcOwnerLast, s.CtcOwnerSuffix);
            if (!string.IsNullOrWhiteSpace(owner)) parts.Add(owner);
            string spouse = Join(s.CtcSpouseFirst, s.CtcSpouseMiddle, s.CtcSpouseLast, s.CtcSpouseSuffix);
            if (!string.IsNullOrWhiteSpace(spouse)) parts.Add("& " + spouse);
            if (s.CtcEventDate.HasValue) parts.Add(s.CtcEventDate.Value.ToString("yyyy-MM-dd"));
            if (!string.IsNullOrWhiteSpace(s.CtcRegistryNo)) parts.Add("Reg " + s.CtcRegistryNo.Trim());
            string line = string.Join(" · ", parts);
            return line.Length > 150 ? line.Substring(0, 150) : line;
        }

        private static string Join(params string[] names)
        {
            var kept = new List<string>();
            foreach (string n in names) if (!string.IsNullOrWhiteSpace(n)) kept.Add(n.Trim());
            return string.Join(" ", kept);
        }

        /// <summary>
        /// Writes the kiosk's Certified True Copy intake, linked to the new ticket. This is what
        /// the CLIENT described, not a certificate request — staff create the
        /// `certificate_requests` row once they have found the actual registry entry.
        /// Never throws: a failure here must not cost the client their queue number, since the
        /// details are also carried on the ticket and can be re-asked at the counter.
        /// </summary>
        public static void SaveCtcRequest(KioskSession s, long ticketId)
        {
            bool marriage = s.CtcDocumentType == "Marriage";
            try
            {
                try { InsertCtc(s, ticketId, marriage, true); }
                catch (MySqlException ex) when (ex.Number == 1054)
                {
                    // Migration 79 (name suffix columns) not applied yet: keep the suffix by
                    // appending it to the last name rather than dropping it.
                    InsertCtc(s, ticketId, marriage, false);
                }
            }
            catch { /* ticket already exists; the counter can re-ask */ }
        }

        private static void InsertCtc(KioskSession s, long ticketId, bool marriage, bool withSuffix)
        {
            string ownerLast = withSuffix ? s.CtcOwnerLast : Join(s.CtcOwnerLast, s.CtcOwnerSuffix);
            string spouseLast = withSuffix ? s.CtcSpouseLast : Join(s.CtcSpouseLast, s.CtcSpouseSuffix);
            Db.Push(
                "INSERT INTO kiosk_ctc_intake (source, queue_ticket_id, doc_type, copies, purpose, relationship, registry_no, " +
                "owner_first, owner_middle, owner_last, " + (withSuffix ? "owner_suffix, spouse_suffix, " : "") +
                "spouse_first, spouse_middle, spouse_last, " +
                "event_date, event_city, event_province, remarks, status) " +
                "VALUES ('Kiosk', @tid, @doc, @copies, @purpose, @rel, @reg, @of, @om, @ol, " + (withSuffix ? "@os, @ss, " : "") +
                "@sf, @sm, @sl, @ed, @ec, @ep, @rem, 'Requested')",
                new MySqlParameter("@tid", ticketId),
                new MySqlParameter("@doc", s.CtcDocumentType),
                new MySqlParameter("@copies", Math.Max(1, s.CtcCopies)),
                new MySqlParameter("@purpose", NullIfBlank(s.CtcPurpose)),
                new MySqlParameter("@rel", NullIfBlank(s.CtcRelationship)),
                new MySqlParameter("@reg", NullIfBlank(s.CtcRegistryNo)),
                new MySqlParameter("@of", NullIfBlank(s.CtcOwnerFirst)),
                new MySqlParameter("@om", NullIfBlank(s.CtcOwnerMiddle)),
                new MySqlParameter("@ol", NullIfBlank(ownerLast)),
                new MySqlParameter("@os", NullIfBlank(s.CtcOwnerSuffix)),
                new MySqlParameter("@ss", marriage ? NullIfBlank(s.CtcSpouseSuffix) : DBNull.Value),
                new MySqlParameter("@sf", marriage ? NullIfBlank(s.CtcSpouseFirst) : DBNull.Value),
                new MySqlParameter("@sm", marriage ? NullIfBlank(s.CtcSpouseMiddle) : DBNull.Value),
                new MySqlParameter("@sl", marriage ? NullIfBlank(spouseLast) : DBNull.Value),
                new MySqlParameter("@ed", s.CtcEventDate.HasValue ? (object)s.CtcEventDate.Value.Date : DBNull.Value),
                new MySqlParameter("@ec", NullIfBlank(s.CtcEventCity)),
                new MySqlParameter("@ep", NullIfBlank(s.CtcEventProvince)),
                new MySqlParameter("@rem", NullIfBlank(s.CtcDetails)));
        }

        // ------------------------------------------------------------ PSA copy (BREQS)
        public static readonly string[] BreqsDocTypes = { "Birth", "Marriage", "Death" };
        // Same lists as CROMS.Data.BreqsService on the staff side (the kiosk has no reference to CROMS.exe).
        public static readonly string[] BreqsRelationships =
            { "Self (document owner)", "Parent", "Spouse", "Child", "Sibling", "Guardian", "Authorized representative" };
        public static readonly string[] BreqsPurposes =
            { "Passport / DFA", "School / Enrollment", "Employment", "SSS / GSIS / PhilHealth", "Travel / Visa",
              "Marriage", "Legal / Court", "Personal copy", "Others" };

        // A certified true copy is asked for by the same people for the same reasons as a PSA
        // copy, so the two share one wording — which also keeps them comparable in the monthly
        // report. Declared AFTER the arrays they alias: a static field initialiser that reads a
        // field declared further down runs first and reads null.
        public static readonly string[] CtcPurposes = BreqsPurposes;
        // The requester's relationship to the owner of the record. Its own short list (not the PSA copy
        // one): the office asked for exactly these. "Other" opens a specify box on the form and is
        // stored as "Other - <detail>".
        public static readonly string[] CtcRelationships =
            { "Self", "Parent", "Child", "Spouse", "Authorized Representative", "Other" };

        /// <summary>What is still missing from a PSA copy request, in the client's words; null when complete.</summary>
        public static string BreqsProblem(KioskSession s)
        {
            if (string.IsNullOrWhiteSpace(s.BreqsDocType)) return "Please choose which PSA certificate you need: Birth, Marriage or Death.";
            if (string.IsNullOrWhiteSpace(s.IdType)) return "Please choose the valid ID you will present.";
            if (string.IsNullOrWhiteSpace(s.IdNo)) return "Please enter the number on your valid ID.";
            if (string.IsNullOrWhiteSpace(s.OwnerFirst) || string.IsNullOrWhiteSpace(s.OwnerLast))
                return s.BreqsDocType == "Marriage" ? "Please enter the husband's first and last name."
                     : s.BreqsDocType == "Death" ? "Please enter the first and last name of the person who died."
                     : "Please enter the first and last name on the birth certificate.";
            if (s.BreqsDocType == "Marriage" && (string.IsNullOrWhiteSpace(s.SpouseFirst) || string.IsNullOrWhiteSpace(s.SpouseLast)))
                return "Please enter the wife's first and last name.";
            if (s.EventDate.HasValue && s.EventDate.Value.Date > DateTime.Today) return "The date cannot be in the future.";
            return null;
        }

        /// <summary>
        /// Log the PSA copy request, linked to the new ticket, status Requested. The staff BREQS
        /// desk picks it up from there. Numbering and retry mirror BreqsService.Save on the staff side.
        /// </summary>
        public static string SaveBreqsRequest(KioskSession s, long ticketId, string ticketCode)
        {
            decimal fee = 50m;
            try
            {
                DataTable f = Db.Pull("SELECT setting_value FROM app_settings WHERE setting_key = 'BREQS_FEE_PER_COPY'");
                decimal d;
                if (f.Rows.Count > 0 && decimal.TryParse(f.Rows[0][0] as string, System.Globalization.NumberStyles.Number,
                                                          System.Globalization.CultureInfo.InvariantCulture, out d)) fee = d;
            }
            catch { }

            bool marriage = s.BreqsDocType == "Marriage", birth = s.BreqsDocType == "Birth";
            int copies = Math.Max(1, s.BreqsCopies);
            for (int attempt = 0; ; attempt++)
            {
                DataTable n = Db.Pull("SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(request_no, '-', -1) AS UNSIGNED)), 0) + 1 FROM psa_copy_requests WHERE request_no LIKE @p",
                                      new MySqlParameter("@p", "BREQS-" + DateTime.Today.Year + "-%"));
                string no = string.Format("BREQS-{0}-{1:D4}", DateTime.Today.Year, Convert.ToInt32(n.Rows[0][0]));
                try
                {
                    long id = Db.Insert(
                        "INSERT INTO psa_copy_requests (request_no, source, queue_ticket_id, requester_first, requester_middle, requester_last, contact_no, " +
                        "relationship, valid_id_type, valid_id_no, doc_type, copies, purpose, owner_first, owner_middle, owner_last, spouse_first, " +
                        "spouse_middle, spouse_last, event_date, event_city, event_province, father_name, mother_maiden_name, status, fee_amount) " +
                        "VALUES (@no, 'Kiosk', @tid, @rf, @rm, @rl, @contact, @rel, @idt, @idn, @doc, @copies, @purpose, @of, @om, @ol, @sf, @sm, @sl, " +
                        "@ed, @ec, @ep, @fa, @mo, 'Requested', @fee)",
                        new MySqlParameter("@no", no), new MySqlParameter("@tid", ticketId),
                        new MySqlParameter("@rf", s.First.Trim()), new MySqlParameter("@rm", NullIfBlank(s.Middle)), new MySqlParameter("@rl", s.Last.Trim()),
                        new MySqlParameter("@contact", NullIfBlank(s.Contact)), new MySqlParameter("@rel", NullIfBlank(s.BreqsRelationship)),
                        new MySqlParameter("@idt", NullIfBlank(s.IdType)), new MySqlParameter("@idn", NullIfBlank(s.IdNo)),
                        new MySqlParameter("@doc", s.BreqsDocType), new MySqlParameter("@copies", copies),
                        new MySqlParameter("@purpose", NullIfBlank(s.BreqsPurpose)),
                        new MySqlParameter("@of", NullIfBlank(s.OwnerFirst)), new MySqlParameter("@om", NullIfBlank(s.OwnerMiddle)), new MySqlParameter("@ol", NullIfBlank(s.OwnerLast)),
                        new MySqlParameter("@sf", marriage ? NullIfBlank(s.SpouseFirst) : DBNull.Value),
                        new MySqlParameter("@sm", marriage ? NullIfBlank(s.SpouseMiddle) : DBNull.Value),
                        new MySqlParameter("@sl", marriage ? NullIfBlank(s.SpouseLast) : DBNull.Value),
                        new MySqlParameter("@ed", s.EventDate.HasValue ? (object)s.EventDate.Value.Date : DBNull.Value),
                        new MySqlParameter("@ec", NullIfBlank(s.EventCity)), new MySqlParameter("@ep", NullIfBlank(s.EventProvince)),
                        new MySqlParameter("@fa", birth ? NullIfBlank(s.FatherName) : DBNull.Value),
                        new MySqlParameter("@mo", birth ? NullIfBlank(s.MotherMaidenName) : DBNull.Value),
                        new MySqlParameter("@fee", fee * copies));
                    Db.Push("INSERT INTO psa_copy_history (request_id, action, from_status, to_status, note) VALUES (@id, 'Request logged (kiosk)', NULL, 'Requested', @note)",
                            new MySqlParameter("@id", id),
                            new MySqlParameter("@note", s.BreqsDocType + ", " + copies + " cop" + (copies == 1 ? "y" : "ies") + ", ticket " + ticketCode));
                    return no;
                }
                catch (MySqlException ex) when (ex.Number == 1062 && attempt < 5) { }
            }
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
        /// <summary>Test seam only: when true, Submit saves everything but skips the thermal
        /// printer and the modal confirmation, so an automated run cannot print or block.
        /// Never set by the kiosk itself.</summary>
        public static bool TestMode;

        private static void PrintTicket(string code, List<string> services, string priorityLane,
            string name, string spouseName, int ahead)
        {
            if (TestMode) return;
            try
            {
                using (var doc = new PrintDocument())
                {
                    int h = 330 + services.Count * 24 + (priorityLane != "Regular" ? 36 : 0)
                            + (!string.IsNullOrEmpty(spouseName) ? 18 : 0);
                    doc.DefaultPageSettings.PaperSize = new PaperSize("Q58", 228, h); // 58mm wide
                    doc.DefaultPageSettings.Margins = new Margins(8, 8, 10, 10);
                    doc.DocumentName = "Queue Ticket " + code;
                    doc.PrintPage += (s, e) => DrawTicket(e, code, services, priorityLane, name, spouseName, ahead);
                    doc.Print();
                }
            }
            catch { /* no printer / cancelled — on-screen ticket still shows the number */ }
        }

        private static void DrawTicket(PrintPageEventArgs e, string code, List<string> services,
            string priorityLane, string name, string spouseName, int ahead)
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
            }
        }

        // ------------------------------------------------ on-screen ticket
        private static void ShowTicket(string code, List<string> services)
        {
            if (TestMode) return;
            using (var dlg = new Form())
            {
                dlg.Text = "Your Queue Number";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterScreen;
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ClientSize = new Size(480, 420);
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
                dlg.Controls.Add(big);
                dlg.Controls.Add(header);
                dlg.ShowDialog();
            }
        }
    }
}

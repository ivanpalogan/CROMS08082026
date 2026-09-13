using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>One request for a PSA-issued copy (migration 40). Field meaning follows <see cref="DocType"/>.</summary>
    public sealed class BreqsRequest
    {
        public int Id;
        public string RequestNo, Source = BreqsService.SourceCounter;
        public int? QueueTicketId;

        public string RequesterFirst, RequesterMiddle, RequesterLast, ContactNo, Relationship, ValidIdType, ValidIdNo;

        public string DocType = BreqsService.Birth;
        public int Copies = 1;
        public string Purpose;
        /// <summary>Birth: the registrant. Marriage: the husband. Death: the deceased.</summary>
        public string OwnerFirst, OwnerMiddle, OwnerLast;
        /// <summary>Marriage only: the wife.</summary>
        public string SpouseFirst, SpouseMiddle, SpouseLast;
        public DateTime? EventDate;
        public string EventCity, EventProvince;
        /// <summary>Birth only.</summary>
        public string FatherName, MotherMaidenName;

        public string Status = BreqsService.Requested;
        public decimal? FeeAmount;
        public string OrNo;
        public DateTime? OrDate;
        public string PsaReferenceNo;
        public DateTime? SubmittedAt, ExpectedDate, ReceivedAt, ReleasedAt, CreatedAt;
        public string ScanId, OcrDocKind, OcrName, OcrMatch, PsaSecurityNo;
        public int? OcrConfidence;
        public bool HasScan;
        public string ClaimantName, ClaimantIdType, ClaimantIdNo, OutcomeReason, Remarks;
        public bool ClaimantIsRep;

        public string RequesterName { get { return MarriageRules.JoinName(RequesterFirst, RequesterMiddle, RequesterLast) ?? ""; } }
        public string OwnerName { get { return MarriageRules.JoinName(OwnerFirst, OwnerMiddle, OwnerLast) ?? ""; } }
        public string SpouseName { get { return MarriageRules.JoinName(SpouseFirst, SpouseMiddle, SpouseLast) ?? ""; } }

        /// <summary>What the list shows in one line: "Birth - JUAN DELA CRUZ" / "Marriage - A & B".</summary>
        public string DocumentLine
        {
            get
            {
                string who = DocType == BreqsService.Marriage && SpouseName.Length > 0 ? OwnerName + " & " + SpouseName : OwnerName;
                return DocType + (who.Length > 0 ? " - " + who : "");
            }
        }
    }

    public sealed class BreqsSettings
    {
        public int TurnaroundDays = 7, UnclaimedDays = 30;
        public decimal FeePerCopy = 50m;
    }

    /// <summary>
    /// BREQS - requests for a PSA-issued copy of a birth, marriage or death certificate.
    /// <para/>
    /// The office's flow (2026-09-13): the client logs the request (kiosk or counter), staff record
    /// the Treasury O.R., submit it through PSA's BREQS, collect the copy at the PSA office in
    /// person, scan it through CROMS's OCR so the record shows THIS is the document that was asked
    /// for (and keeps a copy), then release it to the client.
    /// <para/>
    /// Every status change goes through <see cref="Move"/>, which refuses a jump the workflow does
    /// not allow and writes history + audit. Screens decide nothing about legality of a move.
    /// </summary>
    public static class BreqsService
    {
        public const string Birth = "Birth", Marriage = "Marriage", Death = "Death";
        public static readonly string[] DocTypes = { Birth, Marriage, Death };

        public const string SourceKiosk = "Kiosk", SourceCounter = "Counter";

        // Stored statuses.
        public const string Requested = "Requested", Paid = "Paid", Submitted = "Submitted to PSA",
                            Received = "Received from PSA", Released = "Released",
                            NoRecord = "No Record at PSA", Cancelled = "Cancelled";
        public static readonly string[] Statuses = { Requested, Paid, Submitted, Received, Released, NoRecord, Cancelled };

        /// <summary>Who may ask for someone's civil registry document (PSA's own rule, recorded, not enforced).</summary>
        public static readonly string[] Relationships =
            { "Self (document owner)", "Parent", "Spouse", "Child", "Sibling", "Guardian", "Authorized representative" };

        public static readonly string[] Purposes =
            { "Passport / DFA", "School / Enrollment", "Employment", "SSS / GSIS / PhilHealth", "Travel / Visa",
              "Marriage", "Legal / Court", "Personal copy", "Others" };

        // ================================================================ rules (pure)

        private static readonly Dictionary<string, string[]> Allowed = new Dictionary<string, string[]>
        {
            { Requested, new[] { Paid, Cancelled } },
            { Paid,      new[] { Submitted, Cancelled } },
            { Submitted, new[] { Received, NoRecord } },
            { Received,  new[] { Released } },
            { Released,  new string[0] },
            { NoRecord,  new string[0] },
            { Cancelled, new string[0] },
        };

        public static bool CanMove(string from, string to)
        {
            string[] next;
            return Allowed.TryGetValue(from ?? "", out next) && next.Contains(to);
        }

        public static bool IsClosed(string status) { return status == Released || status == NoRecord || status == Cancelled; }

        /// <summary>Submitted, and past the expected date. Derived - never stored.</summary>
        public static bool IsOverdue(BreqsRequest r, DateTime today)
        {
            return r.Status == Submitted && r.ExpectedDate.HasValue && today.Date > r.ExpectedDate.Value.Date;
        }

        /// <summary>Received and still not released after the unclaimed period. Derived - never stored.</summary>
        public static bool IsUnclaimed(BreqsRequest r, DateTime today, BreqsSettings s)
        {
            return r.Status == Received && r.ReceivedAt.HasValue && (today.Date - r.ReceivedAt.Value.Date).TotalDays > s.UnclaimedDays;
        }

        /// <summary>The status as the desk should read it, with the derived flags folded in.</summary>
        public static string DisplayStatus(BreqsRequest r, DateTime today, BreqsSettings s)
        {
            if (IsOverdue(r, today)) return "Overdue at PSA";
            if (IsUnclaimed(r, today, s)) return "Unclaimed";
            if (r.Status == Received) return "Ready for release";
            return r.Status;
        }

        /// <summary>What must be true before a request can be saved. Empty list = fine.</summary>
        public static List<string> Validate(BreqsRequest r)
        {
            var e = new List<string>();
            if (string.IsNullOrWhiteSpace(r.RequesterFirst) || string.IsNullOrWhiteSpace(r.RequesterLast))
                e.Add("Enter the requester's first and last name.");
            if (string.IsNullOrWhiteSpace(r.ValidIdType)) e.Add("Choose the requester's valid ID type.");
            if (string.IsNullOrWhiteSpace(r.ValidIdNo)) e.Add("Enter the valid ID number.");
            if (!DocTypes.Contains(r.DocType)) e.Add("Choose the certificate needed: Birth, Marriage or Death.");
            if (r.Copies < 1 || r.Copies > 20) e.Add("Copies must be between 1 and 20.");
            if (string.IsNullOrWhiteSpace(r.OwnerFirst) || string.IsNullOrWhiteSpace(r.OwnerLast))
                e.Add(r.DocType == Marriage ? "Enter the husband's first and last name."
                    : r.DocType == Death ? "Enter the first and last name of the deceased."
                    : "Enter the first and last name on the birth certificate.");
            if (r.DocType == Marriage && (string.IsNullOrWhiteSpace(r.SpouseFirst) || string.IsNullOrWhiteSpace(r.SpouseLast)))
                e.Add("Enter the wife's first and last name.");
            if (r.EventDate.HasValue && r.EventDate.Value.Date > DateTime.Today)
                e.Add("The date of the " + r.DocType.ToLowerInvariant() + " cannot be in the future.");
            return e;
        }

        /// <summary>
        /// Does the scanned PSA copy name the person the request is for? Compared on normalised
        /// names (case, accents, punctuation folded) so "DELA CRUZ" and "Dela Cruz" agree.
        /// "Match" = last name and first name both agree; "Partial" = last name only;
        /// "Mismatch" = OCR read a name and it is someone else; "Unread" = OCR found no name.
        /// It is a flag for staff to look at, never a decision.
        /// </summary>
        public static string CompareName(BreqsRequest r, string ocrFirst, string ocrLast)
        {
            string of = N(ocrFirst), ol = N(ocrLast);
            if (of.Length == 0 && ol.Length == 0) return "Unread";
            string rf = N(r.OwnerFirst), rl = N(r.OwnerLast);
            bool last = ol.Length > 0 && (ol == rl || ol.Replace(" ", "") == rl.Replace(" ", ""));
            bool first = of.Length > 0 && rf.Length > 0 && (of.StartsWith(rf) || rf.StartsWith(of));
            if (last && first) return "Match";
            if (last) return "Partial";
            return "Mismatch";
        }

        private static string N(string s) { return LearningLibrary.Normalize(s ?? ""); }

        // ================================================================ settings

        public static BreqsSettings Settings
        {
            get
            {
                var s = new BreqsSettings();
                try
                {
                    foreach (DataRow row in Db.Pull("SELECT setting_key, setting_value FROM app_settings WHERE setting_key LIKE 'BREQS%'").Rows)
                    {
                        string k = row["setting_key"].ToString(), v = row["setting_value"] as string;
                        int n; decimal d;
                        if (k == "BREQS_TURNAROUND_DAYS" && int.TryParse(v, out n) && n >= 0) s.TurnaroundDays = n;
                        else if (k == "BREQS_UNCLAIMED_DAYS" && int.TryParse(v, out n) && n >= 0) s.UnclaimedDays = n;
                        else if (k == "BREQS_FEE_PER_COPY" && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out d) && d >= 0) s.FeePerCopy = d;
                    }
                }
                catch { /* migration 40 not applied: defaults */ }
                // The fee schedule is the one place a fee is changed (Fees & Payments -> Fee schedule, audited).
                // The app_settings value above is only the fallback for a database without migration 41, so
                // an office that edits BREQS on the schedule is not quietly still charged the old amount here.
                try
                {
                    FeeItem f = PaymentService.Fee("BREQS");
                    if (f != null && f.Active && f.Amount.HasValue) s.FeePerCopy = f.Amount.Value;
                }
                catch { /* fees table without migration 41 columns: keep the setting */ }
                return s;
            }
        }

        // ================================================================ reads

        /// <summary>Every column except the scan blob - lists must stay light.</summary>
        private const string Cols =
            "id, request_no, source, queue_ticket_id, requester_first, requester_middle, requester_last, contact_no, relationship, " +
            "valid_id_type, valid_id_no, doc_type, copies, purpose, owner_first, owner_middle, owner_last, spouse_first, spouse_middle, " +
            "spouse_last, event_date, event_city, event_province, father_name, mother_maiden_name, status, fee_amount, or_no, or_date, " +
            "psa_reference_no, submitted_at, expected_date, received_at, scan_id, ocr_doc_kind, ocr_confidence, ocr_name, ocr_match, " +
            "psa_security_no, released_at, claimant_name, claimant_id_type, claimant_id_no, claimant_is_rep, outcome_reason, remarks, " +
            "created_at, scan_image IS NOT NULL AS has_scan";

        public static BreqsRequest Load(int id)
        {
            DataTable t = Db.Pull("SELECT " + Cols + " FROM breqs_requests WHERE id = @id", P("@id", id));
            return t.Rows.Count == 0 ? null : From(t.Rows[0]);
        }

        public static BreqsRequest LoadByTicket(int queueTicketId)
        {
            DataTable t = Db.Pull("SELECT " + Cols + " FROM breqs_requests WHERE queue_ticket_id = @q ORDER BY id DESC LIMIT 1", P("@q", queueTicketId));
            return t.Rows.Count == 0 ? null : From(t.Rows[0]);
        }

        /// <param name="search">Request no., requester or document-owner name; null for all.</param>
        public static List<BreqsRequest> List(string search, bool includeClosed)
        {
            string where = includeClosed ? "1=1" : "status NOT IN ('" + Released + "','" + NoRecord + "','" + Cancelled + "')";
            var ps = new List<MySqlParameter>();
            if (!string.IsNullOrWhiteSpace(search))
            {
                where += " AND (request_no LIKE @q OR CONCAT_WS(' ', requester_first, requester_last) LIKE @q OR " +
                         "CONCAT_WS(' ', owner_first, owner_last) LIKE @q OR CONCAT_WS(' ', spouse_first, spouse_last) LIKE @q OR psa_reference_no LIKE @q)";
                ps.Add(P("@q", "%" + search.Trim() + "%"));
            }
            return Db.Pull("SELECT " + Cols + " FROM breqs_requests WHERE " + where + " ORDER BY id DESC LIMIT 500", ps.ToArray())
                     .AsEnumerable().Select(From).ToList();
        }

        public static byte[] ScanImage(int id)
        {
            DataTable t = Db.Pull("SELECT scan_image FROM breqs_requests WHERE id = @id", P("@id", id));
            return t.Rows.Count == 0 || t.Rows[0][0] == DBNull.Value ? null : (byte[])t.Rows[0][0];
        }

        public static DataTable History(int id)
        {
            return Db.Pull("SELECT h.created_at AS `When`, h.action AS `What`, h.to_status AS `Status`, h.note AS `Note`, " +
                           "COALESCE(u.full_name, u.username, 'kiosk') AS `By` FROM breqs_history h LEFT JOIN users u ON u.id = h.user_id " +
                           "WHERE h.request_id = @id ORDER BY h.id", P("@id", id));
        }

        private static BreqsRequest From(DataRow r)
        {
            return new BreqsRequest
            {
                Id = Convert.ToInt32(r["id"]), RequestNo = S(r["request_no"]), Source = S(r["source"]), QueueTicketId = I(r["queue_ticket_id"]),
                RequesterFirst = S(r["requester_first"]), RequesterMiddle = S(r["requester_middle"]), RequesterLast = S(r["requester_last"]),
                ContactNo = S(r["contact_no"]), Relationship = S(r["relationship"]), ValidIdType = S(r["valid_id_type"]), ValidIdNo = S(r["valid_id_no"]),
                DocType = S(r["doc_type"]), Copies = Convert.ToInt32(r["copies"]), Purpose = S(r["purpose"]),
                OwnerFirst = S(r["owner_first"]), OwnerMiddle = S(r["owner_middle"]), OwnerLast = S(r["owner_last"]),
                SpouseFirst = S(r["spouse_first"]), SpouseMiddle = S(r["spouse_middle"]), SpouseLast = S(r["spouse_last"]),
                EventDate = D(r["event_date"]), EventCity = S(r["event_city"]), EventProvince = S(r["event_province"]),
                FatherName = S(r["father_name"]), MotherMaidenName = S(r["mother_maiden_name"]),
                Status = S(r["status"]), FeeAmount = r["fee_amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(r["fee_amount"]),
                OrNo = S(r["or_no"]), OrDate = D(r["or_date"]), PsaReferenceNo = S(r["psa_reference_no"]),
                SubmittedAt = D(r["submitted_at"]), ExpectedDate = D(r["expected_date"]), ReceivedAt = D(r["received_at"]),
                ScanId = S(r["scan_id"]), OcrDocKind = S(r["ocr_doc_kind"]), OcrConfidence = I(r["ocr_confidence"]),
                OcrName = S(r["ocr_name"]), OcrMatch = S(r["ocr_match"]), PsaSecurityNo = S(r["psa_security_no"]),
                ReleasedAt = D(r["released_at"]), ClaimantName = S(r["claimant_name"]), ClaimantIdType = S(r["claimant_id_type"]),
                ClaimantIdNo = S(r["claimant_id_no"]), ClaimantIsRep = Convert.ToInt32(r["claimant_is_rep"]) != 0,
                OutcomeReason = S(r["outcome_reason"]), Remarks = S(r["remarks"]), CreatedAt = D(r["created_at"]),
                HasScan = Convert.ToInt32(r["has_scan"]) != 0
            };
        }

        // ================================================================ writes

        /// <summary>
        /// Insert a new request (status Requested) or update the details of one that is still open.
        /// Returns its id. Details stop being editable once the request has gone to PSA - what PSA was
        /// asked for must stay what the record says was asked for.
        /// </summary>
        public static int Save(BreqsRequest r, int? userId)
        {
            List<string> errors = Validate(r);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));

            var cols = new Dictionary<string, object>
            {
                { "source", r.Source ?? SourceCounter }, { "queue_ticket_id", r.QueueTicketId },
                { "requester_first", T(r.RequesterFirst) }, { "requester_middle", T(r.RequesterMiddle) }, { "requester_last", T(r.RequesterLast) },
                { "contact_no", T(r.ContactNo) }, { "relationship", T(r.Relationship) }, { "valid_id_type", T(r.ValidIdType) }, { "valid_id_no", T(r.ValidIdNo) },
                { "doc_type", r.DocType }, { "copies", r.Copies }, { "purpose", T(r.Purpose) },
                { "owner_first", T(r.OwnerFirst) }, { "owner_middle", T(r.OwnerMiddle) }, { "owner_last", T(r.OwnerLast) },
                // A spouse only means something on a marriage request; parents only on a birth.
                { "spouse_first", r.DocType == Marriage ? T(r.SpouseFirst) : null },
                { "spouse_middle", r.DocType == Marriage ? T(r.SpouseMiddle) : null },
                { "spouse_last", r.DocType == Marriage ? T(r.SpouseLast) : null },
                { "event_date", r.EventDate }, { "event_city", T(r.EventCity) }, { "event_province", T(r.EventProvince) },
                { "father_name", r.DocType == Birth ? T(r.FatherName) : null },
                { "mother_maiden_name", r.DocType == Birth ? T(r.MotherMaidenName) : null },
                { "remarks", T(r.Remarks) },
            };

            if (r.Id > 0)
            {
                BreqsRequest cur = Load(r.Id);
                if (cur == null) throw new InvalidOperationException("Request not found.");
                if (cur.Status != Requested && cur.Status != Paid)
                    throw new InvalidOperationException("This request is already " + cur.Status.ToLowerInvariant() +
                        " - its details are what PSA was asked for and can no longer be edited.");
                Db.Push("UPDATE breqs_requests SET " + string.Join(", ", cols.Keys.Select(k => k + " = @" + k)) + " WHERE id = @id",
                        cols.Select(kv => P("@" + kv.Key, kv.Value)).Concat(new[] { P("@id", r.Id) }).ToArray());
                AddHistory(r.Id, "Details updated", cur.Status, cur.Status, null, userId);
                Audit.Write(Audit.Update, "breqs_requests", r.Id, "BREQS " + cur.RequestNo + " details");
                return r.Id;
            }

            cols["status"] = Requested;
            cols["created_by"] = userId;
            cols["fee_amount"] = Settings.FeePerCopy * r.Copies;
            int id = 0;
            string no = null;
            for (int attempt = 0; ; attempt++)
            {
                no = NextRequestNo(DateTime.Today.Year);
                cols["request_no"] = no;
                try
                {
                    id = (int)Db.Insert("INSERT INTO breqs_requests (" + string.Join(", ", cols.Keys) + ") VALUES (" +
                                        string.Join(", ", cols.Keys.Select(k => "@" + k)) + ")",
                                        cols.Select(kv => P("@" + kv.Key, kv.Value)).ToArray());
                    break;
                }
                catch (MySqlException ex) when (ex.Number == 1062 && ex.Message.IndexOf("ux_breqs_request_no", StringComparison.OrdinalIgnoreCase) >= 0
                                                 && attempt < RegistryNumber.MaxRetries) { }
            }
            r.Id = id; r.RequestNo = no; r.Status = Requested;
            AddHistory(id, "Request logged (" + (r.Source ?? SourceCounter).ToLowerInvariant() + ")", null, Requested,
                       r.DocumentLine + ", " + r.Copies + " cop" + (r.Copies == 1 ? "y" : "ies"), userId);
            Audit.Write(Audit.Create, "breqs_requests", id, "BREQS " + no + " " + r.DocumentLine);
            return id;
        }

        public static string NextRequestNo(int year)
        {
            DataTable t = Db.Pull("SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(request_no, '-', -1) AS UNSIGNED)), 0) + 1 FROM breqs_requests " +
                                  "WHERE request_no LIKE @p", P("@p", "BREQS-" + year + "-%"));
            return string.Format("BREQS-{0}-{1:D4}", year, Convert.ToInt32(t.Rows[0][0]));
        }

        public static void RecordPayment(int id, string orNo, DateTime orDate, decimal amount, int? userId)
        {
            if (string.IsNullOrWhiteSpace(orNo)) throw new InvalidOperationException("Enter the Treasury official receipt number.");
            // The O.R. is checked against the payment log BEFORE the status moves, so a receipt already
            // recorded for someone else refuses the payment instead of leaving a Paid request with no log row.
            PaymentService.EnsureOrFree(orNo, "breqs_requests", id);
            Move(id, Paid, "Payment recorded", "O.R. " + orNo.Trim() + ", PHP " + amount.ToString("0.00", CultureInfo.InvariantCulture), userId,
                 "or_no = @or, or_date = @ord, fee_amount = @amt", P("@or", orNo.Trim()), P("@ord", orDate.Date), P("@amt", amount));

            BreqsRequest r = Load(id);
            int copies = r == null ? 1 : Math.Max(1, r.Copies);
            PaymentService.RecordForModule(new PaymentEntry
            {
                Source = PaymentService.SourceBreqs, SourceTable = "breqs_requests", SourceId = id,
                PayerName = r == null ? null : r.RequesterName, Purpose = "BREQS " + (r == null ? "" : r.RequestNo + " ").Trim() + " (PSA copy)",
                OrNumber = orNo, PaidAt = orDate.Date,
                Lines = { new PaymentLine { FeeCode = "BREQS", Description = "BREQS fee (PSA copy), per copy", Quantity = copies, UnitAmount = amount / copies } }
            }, userId);
        }

        public static void SubmitToPsa(int id, string psaReference, DateTime submittedOn, int? userId)
        {
            DateTime expected = submittedOn.Date.AddDays(Settings.TurnaroundDays);
            Move(id, Submitted, "Submitted to PSA", (string.IsNullOrWhiteSpace(psaReference) ? "" : "BREQS ref " + psaReference.Trim() + ", ") +
                 "expected " + expected.ToString("dd MMM yyyy", CultureInfo.InvariantCulture), userId,
                 "psa_reference_no = @ref, submitted_at = @sub, expected_date = @exp",
                 P("@ref", T(psaReference)), P("@sub", submittedOn), P("@exp", expected));
        }

        /// <summary>
        /// The PSA copy arrived and was scanned. Stores the scan and what OCR read from it, logs the
        /// run to ocr_batch (so the scan sits in the same audit trail as every other scan), and moves
        /// the request to Received. The name-match flag is kept for staff; it never blocks.
        /// </summary>
        public static void ReceiveFromPsa(int id, byte[] scan, DocAiResult ocr, string securityNo, int? userId)
        {
            if (scan == null || scan.Length == 0) throw new InvalidOperationException("Scan the PSA copy first.");
            BreqsRequest r = Load(id);
            if (r == null) throw new InvalidOperationException("Request not found.");

            string kind = ocr == null ? null : ocr.Kind.ToString();
            int? conf = ocr == null ? (int?)null : ocr.OcrConfidence;
            string first, last;
            OcrOwner(r.DocType, ocr, out first, out last);
            string match = CompareName(r, first, last);
            string ocrName = MarriageRules.JoinName(first, null, last);
            if (ocr != null && ocr.Kind != DocKind.Unknown && ocr.Kind.ToString() != r.DocType)
                match = "Wrong document";

            string scanId = "BRQ-" + DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + id;
            Move(id, Received, "Received from PSA and scanned",
                 "OCR: " + (kind ?? "not run") + (conf.HasValue ? " " + conf + "%" : "") + ", name " + match.ToLowerInvariant() +
                 (ocrName == null ? "" : " (" + ocrName + ")"), userId,
                 "received_at = NOW(), received_by = @by, scan_image = @img, scan_id = @sid, ocr_doc_kind = @kind, ocr_confidence = @conf, " +
                 "ocr_name = @name, ocr_match = @match, psa_security_no = @sec",
                 P("@by", userId), new MySqlParameter("@img", MySqlDbType.LongBlob) { Value = scan }, P("@sid", scanId), P("@kind", kind),
                 P("@conf", conf), P("@name", ocrName), P("@match", match), P("@sec", T(securityNo)));

            try
            {
                Db.Push("INSERT INTO ocr_batch (scan_id, source_book, doc_class, doc_kind, record_table, record_id, confidence, needs_review, " +
                        "review_reason, username, status, raw_text) VALUES (@sid, 'PSA copy (BREQS)', @class, @kind, 'breqs_requests', @rid, " +
                        "@conf, @need, @reason, @user, 'Attached', @raw)",
                        P("@sid", scanId), P("@class", "PSA " + r.DocType + " copy"), P("@kind", kind ?? "Unknown"), P("@rid", id), P("@conf", conf),
                        P("@need", match == "Match" ? 0 : 1), P("@reason", match == "Match" ? null : "Name on the PSA copy: " + match),
                        P("@user", Session.User == null ? null : Session.User.Username), P("@raw", ocr == null ? null : ocr.RawText));
            }
            catch { /* the request is already updated; a missing batch log must not undo it */ }
        }

        /// <summary>The name the PSA copy is FOR, out of the OCR fields for this document type.</summary>
        public static void OcrOwner(string docType, DocAiResult ocr, out string first, out string last)
        {
            first = last = null;
            if (ocr == null) return;
            Dictionary<string, string> m = ocr.Map();
            Func<string, string> get = k => { string v; return m.TryGetValue(k, out v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null; };
            if (docType == Marriage) { first = get("HusbandFirst"); last = get("HusbandLast"); }
            else if (docType == Death) { first = get("DeceasedFirst"); last = get("DeceasedLast"); }
            else { first = get("ChildFirst"); last = get("ChildLast"); }
        }

        public static void Release(int id, string claimant, string idType, string idNo, bool representative, int? userId)
        {
            if (string.IsNullOrWhiteSpace(claimant)) throw new InvalidOperationException("Enter the name of the person claiming the document.");
            if (string.IsNullOrWhiteSpace(idType) || string.IsNullOrWhiteSpace(idNo)) throw new InvalidOperationException("Record the claimant's valid ID type and number.");
            Move(id, Released, "Released to " + (representative ? "representative" : "requester"), claimant.Trim() + ", " + idType.Trim() + " " + idNo.Trim(), userId,
                 "released_at = NOW(), released_by = @by, claimant_name = @cn, claimant_id_type = @ct, claimant_id_no = @ci, claimant_is_rep = @rep",
                 P("@by", userId), P("@cn", claimant.Trim()), P("@ct", idType.Trim()), P("@ci", idNo.Trim()), P("@rep", representative ? 1 : 0));
        }

        public static void MarkNoRecord(int id, string reason, int? userId)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("State what PSA returned (e.g. no record found, negative certification).");
            Move(id, NoRecord, "PSA returned no record", reason.Trim(), userId, "outcome_reason = @why", P("@why", reason.Trim()));
        }

        public static void Cancel(int id, string reason, int? userId)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Give a reason for cancelling.");
            Move(id, Cancelled, "Cancelled", reason.Trim(), userId, "outcome_reason = @why", P("@why", reason.Trim()));
        }

        /// <summary>The ONLY place a status changes: refuses an illegal move, then writes status + extra columns + history + audit.</summary>
        private static void Move(int id, string to, string action, string note, int? userId, string extraSet, params MySqlParameter[] extra)
        {
            BreqsRequest cur = Load(id);
            if (cur == null) throw new InvalidOperationException("Request not found.");
            if (!CanMove(cur.Status, to))
                throw new InvalidOperationException("A request that is " + cur.Status.ToLowerInvariant() + " cannot be marked " + to.ToLowerInvariant() + ".");
            var ps = new List<MySqlParameter>(extra) { P("@to", to), P("@id", id), P("@from", cur.Status) };
            // "AND status = @from" makes a double-click or a second PC acting at the same moment a
            // no-op instead of a second history row for a move that already happened.
            int n = Db.Push("UPDATE breqs_requests SET status = @to" + (string.IsNullOrEmpty(extraSet) ? "" : ", " + extraSet) +
                            " WHERE id = @id AND status = @from", ps.ToArray());
            if (n == 0) throw new InvalidOperationException("This request was changed by someone else just now - reopen it.");
            AddHistory(id, action, cur.Status, to, note, userId);
            Audit.Write(Audit.Update, "breqs_requests", id, "BREQS " + cur.RequestNo + ": " + cur.Status + " -> " + to);
        }

        private static void AddHistory(int id, string action, string from, string to, string note, int? userId)
        {
            Db.Push("INSERT INTO breqs_history (request_id, action, from_status, to_status, note, user_id) VALUES (@r, @a, @f, @t, @n, @u)",
                    P("@r", id), P("@a", action), P("@f", from), P("@t", to), P("@n", note == null ? null : (note.Length > 255 ? note.Substring(0, 255) : note)), P("@u", userId));
        }

        // ================================================================ helpers
        private static MySqlParameter P(string n, object v) { return new MySqlParameter(n, v ?? DBNull.Value); }
        private static string T(string s) { return string.IsNullOrWhiteSpace(s) ? null : s.Trim(); }
        private static string S(object v) { return v == null || v == DBNull.Value ? null : v.ToString(); }
        private static int? I(object v) { return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v); }
        private static DateTime? D(object v) { return v == null || v == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(v); }
    }
}

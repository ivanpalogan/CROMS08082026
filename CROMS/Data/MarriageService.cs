using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Every database write the marriage workflow makes, in one place, so the screens and
    /// CROMS.MarriageTest drive EXACTLY the same code. The screens never write licence /
    /// requirement / registration / transmittal rows themselves.
    ///
    /// <para>Rules come from <see cref="MarriageRules"/>; this class re-validates on the
    /// server side before every state change, so a stale screen cannot issue a licence or
    /// register a marriage that the current data no longer supports.</para>
    ///
    /// <para>Multi-row state changes (issue, register, batch send / acknowledge / return)
    /// run in one transaction. Number assignment (licence no., registry no.) is MAX+1 backed
    /// by a UNIQUE index and retried on collision - the same pairing migration 27 put on
    /// registry numbers.</para>
    /// </summary>
    public static class MarriageService
    {
        private static MarriageSettings _settings;
        public static MarriageSettings Settings { get { return _settings ?? (_settings = MarriageSettings.Load()); } }
        public static void ReloadSettings() { _settings = MarriageSettings.Load(); }

        public static readonly string[] RequirementStatuses = { "Missing", "Submitted", "Verified", "Rejected", "Waived" };
        public static readonly string[] AdviceOutcomes = { "", "Favorable", "Unfavorable", "Not obtained" };
        public static readonly string[] CopyStatuses = { "Pending", "Prepared", "Released", "Sent", "Filed", "Acknowledged" };
        public static readonly string[] SubmissionMethods = { "Physical Batch", "Electronic Submission / Endorsement", "Other Official Method" };

        // ================================================================ plumbing
        private static MySqlParameter P(string name, object value)
        {
            if (value is string && string.IsNullOrWhiteSpace((string)value)) value = null;
            return new MySqlParameter(name, value ?? DBNull.Value);
        }

        private static int Exec(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteNonQuery();
            }
        }

        private static long Insert(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.Parameters.AddRange(ps);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        private static object Scalar(MySqlConnection c, MySqlTransaction t, string sql, params MySqlParameter[] ps)
        {
            using (var cmd = new MySqlCommand(sql, c, t))
            {
                cmd.Parameters.AddRange(ps);
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Runs <paramref name="work"/> in one transaction; rolls back on any exception.</summary>
        private static void Tx(Action<MySqlConnection, MySqlTransaction> work)
        {
            using (var c = new MySqlConnection(ServerConfig.EffectiveConnectionString))
            {
                c.Open();
                using (MySqlTransaction t = c.BeginTransaction())
                {
                    try { work(c, t); t.Commit(); }
                    catch { try { t.Rollback(); } catch { } throw; }
                }
            }
        }

        private static object UserId { get { return Session.User != null ? (object)Session.User.Id : null; } }

        public static bool IsRegistrar
        {
            get { return Session.User != null && (Session.User.Role == "Registrar" || Session.User.Role == "Admin"); }
        }

        private static void RequireRegistrar(string action)
        {
            if (!IsRegistrar)
                throw new UnauthorizedAccessException("Only a Registrar or Admin can " + action + ".");
        }

        public static bool IsAdmin
        {
            get { return Session.User != null && Session.User.Role == "Admin"; }
        }

        private static void RequireAdmin(string action)
        {
            if (!IsAdmin)
                throw new UnauthorizedAccessException("Only an Admin can " + action + ".");
        }

        private static bool DuplicateOn(MySqlException ex, string index)
        {
            return ex != null && ex.Number == 1062 && ex.Message != null &&
                   ex.Message.IndexOf(index, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static DateTime? Dt(object v) { return v == null || v == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(v); }
        private static int? Int(object v) { return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v); }
        private static string Str(object v) { return v == null || v == DBNull.Value ? null : v.ToString(); }
        private static string Col(DataRow r, string c) { return r.Table.Columns.Contains(c) ? Str(r[c]) : null; }
        private static DateTime? ColD(DataRow r, string c) { return r.Table.Columns.Contains(c) ? Dt(r[c]) : null; }

        // ================================================================ history
        public static void History(string entity, int id, string ev, string from, string to, string details,
                                   MySqlConnection c = null, MySqlTransaction t = null)
        {
            const string sql = "INSERT INTO marriage_history (entity, entity_id, event, from_status, to_status, details, user_id) " +
                               "VALUES (@e, @id, @ev, @f, @to, @d, @u)";
            var ps = new[] { P("@e", entity), P("@id", id), P("@ev", ev), P("@f", from), P("@to", to), P("@d", details), P("@u", UserId) };
            if (c != null) Exec(c, t, sql, ps);
            else Db.Push(sql, ps);
        }

        public static DataTable HistoryOf(string entity, int id)
        {
            return Db.Pull(
                "SELECT h.created_at AS `When`, h.event AS `Event`, " +
                "CONCAT_WS(' -> ', NULLIF(h.from_status,''), NULLIF(h.to_status,'')) AS `Status`, " +
                "h.details AS `Details`, COALESCE(u.full_name, u.username, '-') AS `By` " +
                "FROM marriage_history h LEFT JOIN users u ON u.id = h.user_id " +
                "WHERE h.entity = @e AND h.entity_id = @id ORDER BY h.id DESC",
                P("@e", entity), P("@id", id));
        }

        // ================================================================ catalogue
        public static List<ReqType> Catalog()
        {
            var list = new List<ReqType>();
            foreach (DataRow r in Db.Pull("SELECT * FROM marriage_requirement_types").Rows)
                list.Add(new ReqType
                {
                    Code = Str(r["code"]), Label = Str(r["label"]), AppliesTo = Str(r["applies_to"]),
                    RuleKey = Str(r["rule_key"]), Basis = Str(r["legal_basis"]),
                    PerParty = Convert.ToInt32(r["per_party"]) != 0, Blocking = Convert.ToInt32(r["blocking"]) != 0,
                    Active = Convert.ToInt32(r["is_active"]) != 0, Sort = Convert.ToInt32(r["sort_order"]),
                    // Absent on a database from before migration 42 - Str/no-column reads as
                    // null/1, which is exactly "not part of a group", the pre-42 meaning.
                    GroupCode = r.Table.Columns.Contains("group_code") ? Str(r["group_code"]) : null,
                    GroupMin = r.Table.Columns.Contains("group_min") ? Convert.ToInt32(r["group_min"]) : 1
                });
            return list;
        }

        public static List<ReqRow> Requirements(string ownerType, int ownerId)
        {
            var list = new List<ReqRow>();
            DataTable dt = Db.Pull(
                "SELECT id, party, req_code, req_label, status, outcome, given_by, reference_no, doc_date, " +
                "attachment IS NOT NULL AS has_att, verified_by, verified_at, notes " +
                "FROM marriage_requirements WHERE owner_type = @t AND owner_id = @id ORDER BY id",
                P("@t", ownerType), P("@id", ownerId));
            foreach (DataRow r in dt.Rows) list.Add(ToReq(r));
            return list;
        }

        private static ReqRow ToReq(DataRow r)
        {
            return new ReqRow
            {
                Id = Convert.ToInt32(r["id"]), Party = Str(r["party"]), Code = Str(r["req_code"]),
                Label = Str(r["req_label"]), Status = Str(r["status"]) ?? "Missing", Outcome = Str(r["outcome"]),
                GivenBy = Str(r["given_by"]), ReferenceNo = Str(r["reference_no"]), DocDate = Dt(r["doc_date"]),
                HasAttachment = Convert.ToInt32(r["has_att"]) != 0, VerifiedBy = Int(r["verified_by"]),
                VerifiedAt = Dt(r["verified_at"]), Notes = Str(r["notes"])
            };
        }

        /// <summary>
        /// Update one requirement row. Marking it Verified stamps who verified it and when;
        /// moving it off Verified clears that stamp so the record never claims a check that
        /// no longer stands.
        /// </summary>
        public static void SaveRequirement(ReqRow r)
        {
            if (Array.IndexOf(RequirementStatuses, r.Status) < 0) throw new ArgumentException("Unknown status " + r.Status);
            DataTable cur = Db.Pull("SELECT owner_type, owner_id, status FROM marriage_requirements WHERE id = @id", P("@id", r.Id));
            if (cur.Rows.Count == 0) throw new InvalidOperationException("Requirement not found.");
            string owner = Str(cur.Rows[0]["owner_type"]);
            int ownerId = Convert.ToInt32(cur.Rows[0]["owner_id"]);
            string old = Str(cur.Rows[0]["status"]);

            bool verified = r.Status == "Verified";
            Db.Push(
                "UPDATE marriage_requirements SET status=@s, outcome=@o, given_by=@g, reference_no=@ref, doc_date=@dd, notes=@n, " +
                "verified_by = CASE WHEN @v = 1 THEN COALESCE(verified_by, @u) ELSE NULL END, " +
                "verified_at = CASE WHEN @v = 1 THEN COALESCE(verified_at, NOW()) ELSE NULL END " +
                "WHERE id = @id",
                P("@s", r.Status), P("@o", r.Outcome), P("@g", r.GivenBy), P("@ref", r.ReferenceNo),
                P("@dd", r.DocDate), P("@n", r.Notes), P("@v", verified ? 1 : 0), P("@u", UserId), P("@id", r.Id));

            if (old != r.Status)
                History(owner, ownerId, "Requirement", old, r.Status, r.Code + (r.Party == "Both" ? "" : " (" + r.Party + ")"));
            if (owner == "License") RecomputeEarliest(ownerId);
        }

        public static void AttachRequirement(int reqId, byte[] bytes, string fileName)
        {
            if (bytes == null || bytes.Length == 0) throw new ArgumentException("Empty file.");
            if (bytes.Length > 8 * 1024 * 1024) throw new ArgumentException("File is over 8 MB.");
            Db.Push("UPDATE marriage_requirements SET attachment=@b, attachment_name=@n, " +
                    "status = CASE WHEN status = 'Missing' THEN 'Submitted' ELSE status END WHERE id=@id",
                new MySqlParameter("@b", MySqlDbType.LongBlob) { Value = bytes }, P("@n", fileName), P("@id", reqId));
        }

        public static byte[] RequirementAttachment(int reqId, out string fileName)
        {
            fileName = null;
            DataTable dt = Db.Pull("SELECT attachment, attachment_name FROM marriage_requirements WHERE id=@id", P("@id", reqId));
            if (dt.Rows.Count == 0 || dt.Rows[0]["attachment"] == DBNull.Value) return null;
            fileName = Str(dt.Rows[0]["attachment_name"]);
            return (byte[])dt.Rows[0]["attachment"];
        }

        /// <summary>
        /// Make the requirement rows match what the rules say this couple needs. Missing rows
        /// are created. A row that is no longer needed is removed ONLY when nobody has touched
        /// it; one carrying a status, attachment or reference is kept (the screen marks it
        /// "no longer required") - deleting evidence staff recorded would be worse than
        /// showing one extra line.
        /// </summary>
        /// <summary>
        /// Adds a row for every Need not already tracked, and drops a row that is no longer
        /// needed AND was never touched (still Missing, no attachment/reference/date) - a
        /// requirement staff already worked on is kept even if the conditions that produced it
        /// change later, since it holds a record. Generic on ownerType, so any owner shaped like
        /// License/Marriage/Birth can use the same requirements table and the same screen
        /// control (RequirementsGrid) rather than a parallel implementation.
        /// </summary>
        public static void SyncRequirements(string ownerType, int ownerId, List<Need> needs)
        {
            List<ReqRow> rows = Requirements(ownerType, ownerId);
            foreach (Need n in needs)
                if (MarriageRules.RowFor(rows, n) == null)
                    Db.Push("INSERT IGNORE INTO marriage_requirements (owner_type, owner_id, party, req_code, req_label) " +
                            "VALUES (@t, @id, @p, @c, @l)",
                        P("@t", ownerType), P("@id", ownerId), P("@p", n.Party), P("@c", n.Code), P("@l", n.Label));
            foreach (ReqRow r in rows)
                // A custom row (added by staff via "+ Add Requirement", never produced by the
                // rules engine) is never swept here - it isn't in `needs` by definition, and the
                // Missing/no-attachment guard below would otherwise delete it the moment it's
                // created, on the very next save that calls SyncRequirements.
                if (!IsCustomCode(r.Code) &&
                    !needs.Any(n => n.Code == r.Code && n.Party == r.Party) && r.Status == "Missing" &&
                    !r.HasAttachment && string.IsNullOrWhiteSpace(r.ReferenceNo) && string.IsNullOrWhiteSpace(r.GivenBy) && !r.DocDate.HasValue)
                    Db.Push("DELETE FROM marriage_requirements WHERE id = @id", P("@id", r.Id));
        }

        private const string CustomCodePrefix = "CUSTOM-";
        public static bool IsCustomCode(string code) { return code != null && code.StartsWith(CustomCodePrefix, StringComparison.Ordinal); }

        /// <summary>
        /// Adds an ad hoc requirement the office's own catalogue doesn't list (a document a
        /// particular case turns out to need that PSA MC 2024-17 / the licence rules don't name).
        /// Staff type the label themselves, so it carries no legal_basis/rule_key and is never
        /// Blocking - it is a record-keeping convenience, not a new statutory requirement CROMS
        /// invented. `IsCustomCode` is what keeps SyncRequirements from deleting it again.
        /// </summary>
        public static ReqRow AddCustomRequirement(string ownerType, int ownerId, string party, string label)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Requirement name is required.");
            string code = CustomCodePrefix + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
            long id = Db.Insert(
                "INSERT INTO marriage_requirements (owner_type, owner_id, party, req_code, req_label) VALUES (@t, @id, @p, @c, @l)",
                P("@t", ownerType), P("@id", ownerId), P("@p", string.IsNullOrWhiteSpace(party) ? "Both" : party),
                P("@c", code), P("@l", label.Trim()));
            History(ownerType, ownerId, "Requirement", null, "Missing", code + " added (custom: " + label.Trim() + ")");
            return Requirements(ownerType, ownerId).First(r => r.Id == (int)id);
        }

        // ================================================================ licences
        public static LicenseFacts LoadLicense(int id)
        {
            DataTable dt = Db.Pull("SELECT * FROM marriage_licenses WHERE id = @id", P("@id", id));
            if (dt.Rows.Count == 0) return null;
            LicenseFacts l = ToLicense(dt.Rows[0]);
            l.Requirements = Requirements("License", id);
            DataTable m = Db.Pull("SELECT id, registry_no, status FROM marriages WHERE license_id = @id", P("@id", id));
            if (m.Rows.Count > 0)
            {
                l.UsedByMarriageId = Convert.ToInt32(m.Rows[0]["id"]);
                l.UsedByRegistryNo = Str(m.Rows[0]["registry_no"]);
                l.UsedByStatus = Str(m.Rows[0]["status"]);
            }
            return l;
        }

        private static LicenseFacts ToLicense(DataRow r)
        {
            var l = new LicenseFacts
            {
                Id = Convert.ToInt32(r["id"]), ApplicationNo = Col(r, "application_no"), LicenseNo = Col(r, "license_no"),
                StoredStatus = Col(r, "status") ?? "Draft", FiledDate = ColD(r, "filed_date"),
                PostingStart = ColD(r, "posting_start"), PostingEnd = ColD(r, "posting_ends"),
                EarliestIssue = ColD(r, "earliest_issue_date"), IssueDate = ColD(r, "issue_date"),
                ExpiryDate = ColD(r, "expiry_date"), DeferralReason = Col(r, "deferral_reason"),
                PaymentOr = Col(r, "payment_or_no"), PaymentDate = ColD(r, "payment_date"),
                ImpedimentNote = Col(r, "impediment_note"), HoldReason = Col(r, "hold_reason"),
                CancelReason = Col(r, "cancel_reason"), Remarks = Col(r, "remarks"),
                OverrideBy = r.Table.Columns.Contains("registrar_override_by") ? Int(r["registrar_override_by"]) : null,
                RequirementsOverrideBy = r.Table.Columns.Contains("requirements_override_by") ? Int(r["requirements_override_by"]) : null,
                RequirementsOverrideAt = ColD(r, "requirements_override_at"),
                RequirementsOverrideReason = Col(r, "requirements_override_reason")
            };
            if (r.Table.Columns.Contains("payment_amount") && r["payment_amount"] != DBNull.Value)
                l.PaymentAmount = Convert.ToDecimal(r["payment_amount"]);
            FillParty(l.Husband, r, "husband");
            FillParty(l.Wife, r, "wife");
            // Legacy rows (pre-33) carried one free-text name each.
            if (string.IsNullOrWhiteSpace(l.Husband.First) && !string.IsNullOrWhiteSpace(Col(r, "husband_name")))
                l.Husband.Last = Col(r, "husband_name");
            if (string.IsNullOrWhiteSpace(l.Wife.First) && !string.IsNullOrWhiteSpace(Col(r, "wife_name")))
                l.Wife.Last = Col(r, "wife_name");
            return l;
        }

        private static void FillParty(Party p, DataRow r, string pre)
        {
            p.First = Col(r, pre + "_first_name"); p.Middle = Col(r, pre + "_middle_name"); p.Last = Col(r, pre + "_last_name");
            p.Dob = ColD(r, pre + "_date_of_birth"); p.PlaceOfBirth = Col(r, pre + "_place_of_birth");
            p.Sex = Col(r, pre + "_sex"); p.BirthCountry = Col(r, pre + "_birth_country");
            p.Citizenship = Col(r, pre + "_citizenship"); p.CivilStatus = Col(r, pre + "_civil_status");
            p.Religion = Col(r, pre + "_religion"); p.Residence = Col(r, pre + "_residence");

            p.FatherFirst = Col(r, pre + "_father_first_name"); p.FatherMiddle = Col(r, pre + "_father_middle_name");
            p.FatherLast = Col(r, pre + "_father_last_name");
            p.FatherCitizenship = Col(r, pre + "_father_citizenship"); p.FatherResidence = Col(r, pre + "_father_residence");
            p.MotherFirst = Col(r, pre + "_mother_first_name"); p.MotherMiddle = Col(r, pre + "_mother_middle_name");
            p.MotherLast = Col(r, pre + "_mother_last_name");
            p.MotherCitizenship = Col(r, pre + "_mother_citizenship"); p.MotherResidence = Col(r, pre + "_mother_residence");
            p.ConsentFirst = Col(r, pre + "_consent_first_name"); p.ConsentMiddle = Col(r, pre + "_consent_middle_name");
            p.ConsentLast = Col(r, pre + "_consent_last_name"); p.ConsentRelationship = Col(r, pre + "_consent_relationship");
            p.ConsentCitizenship = Col(r, pre + "_consent_citizenship"); p.ConsentResidence = Col(r, pre + "_consent_residence");
            p.PrevDissolution = Col(r, pre + "_prev_dissolution");
            p.PrevDissolvedMunicipality = Col(r, pre + "_prev_dissolved_municipality");
            p.PrevDissolvedProvince = Col(r, pre + "_prev_dissolved_province");
            p.PrevDissolvedDate = ColD(r, pre + "_prev_dissolved_date");

            // A licence filed before migration 38 has only the retired joined name. It is shown
            // whole in the LAST cell for the clerk to correct - never split on spaces, which
            // cannot tell a two-word surname from a middle name (the 2026-09-02 OCR failure).
            // Same precedent as the legacy husband_name above.
            string oldFather = Col(r, pre + "_father_name"), oldMother = Col(r, pre + "_mother_name");
            if (MarriageRules.JoinName(p.FatherFirst, p.FatherMiddle, p.FatherLast) == null && oldFather != null) p.FatherLast = oldFather;
            if (MarriageRules.JoinName(p.MotherFirst, p.MotherMiddle, p.MotherLast) == null && oldMother != null) p.MotherLast = oldMother;
            p.Father = MarriageRules.JoinName(p.FatherFirst, p.FatherMiddle, p.FatherLast);
            p.Mother = MarriageRules.JoinName(p.MotherFirst, p.MotherMiddle, p.MotherLast);
        }

        /// <summary>Every licence with its requirements and marriage link (small office: loaded whole).</summary>
        public static List<LicenseFacts> ListLicenses()
        {
            var list = new List<LicenseFacts>();
            DataTable dt = Db.Pull(
                "SELECT l.*, m.id AS m_id, m.registry_no AS m_reg, m.status AS m_status " +
                "FROM marriage_licenses l LEFT JOIN marriages m ON m.license_id = l.id ORDER BY l.id DESC");
            var reqs = new Dictionary<int, List<ReqRow>>();
            DataTable rq = Db.Pull(
                "SELECT id, owner_id, party, req_code, req_label, status, outcome, given_by, reference_no, doc_date, " +
                "attachment IS NOT NULL AS has_att, verified_by, verified_at, notes FROM marriage_requirements WHERE owner_type='License'");
            foreach (DataRow r in rq.Rows)
            {
                int o = Convert.ToInt32(r["owner_id"]);
                if (!reqs.ContainsKey(o)) reqs[o] = new List<ReqRow>();
                reqs[o].Add(ToReq(r));
            }
            foreach (DataRow r in dt.Rows)
            {
                LicenseFacts l = ToLicense(r);
                List<ReqRow> rr;
                l.Requirements = reqs.TryGetValue(l.Id, out rr) ? rr : new List<ReqRow>();
                if (r["m_id"] != DBNull.Value)
                {
                    l.UsedByMarriageId = Convert.ToInt32(r["m_id"]);
                    l.UsedByRegistryNo = Str(r["m_reg"]);
                    l.UsedByStatus = Str(r["m_status"]);
                }
                list.Add(l);
            }
            return list;
        }

        public static int OutstandingBlocking(LicenseFacts l, List<ReqType> catalog)
        {
            DateTime on = l.FiledDate ?? DateTime.Today;
            List<Need> needs = MarriageRules.Needs(l.Husband, l.Wife, on, catalog, "License", Settings);
            return MarriageRules.RequirementIssues(needs, l.Requirements, l.Husband, l.Wife, "Requirements").Count;
        }

        private static string NextNumber(string table, string column, string prefix, int year)
        {
            DataTable dt = Db.Pull(
                "SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(" + column + ", '-', -1) AS UNSIGNED)), 0) + 1 AS n FROM " + table +
                " WHERE " + column + " LIKE @p", P("@p", year + "-" + prefix + "-%"));
            int n = dt.Rows.Count > 0 && dt.Rows[0]["n"] != DBNull.Value ? Convert.ToInt32(dt.Rows[0]["n"]) : 1;
            return string.Format("{0}-{1}-{2:D4}", year, prefix, n);
        }

        public static string PeekNextLicenseNo(DateTime issueDate)
        {
            return NextNumber("marriage_licenses", "license_no", "L", issueDate.Year);
        }

        /// <summary>Insert or update a Form 90 application's applicant data. Returns its id.</summary>
        public static int SaveLicense(LicenseFacts l)
        {
            if (l.Id > 0)
            {
                LicenseFacts cur = LoadLicense(l.Id);
                if (cur == null) throw new InvalidOperationException("Application not found.");
                if (cur.StoredStatus == "Issued" || cur.StoredStatus == "Used" || cur.StoredStatus == "Expired" || cur.StoredStatus == "Cancelled")
                    throw new InvalidOperationException("An " + cur.StoredStatus.ToLowerInvariant() +
                        " licence is a closed record and cannot be edited.");
            }

            var ps = new List<MySqlParameter>
            {
                P("@filed", (l.FiledDate ?? DateTime.Today).Date), P("@remarks", l.Remarks),
                P("@hname", l.Husband.FullName), P("@wname", l.Wife.FullName)
            };
            var cols = new List<string> { "filed_date", "remarks", "husband_name", "wife_name" };
            var vals = new List<string> { "@filed", "@remarks", "@hname", "@wname" };
            foreach (var pair in new[] { Tuple.Create("husband", l.Husband), Tuple.Create("wife", l.Wife) })
            {
                string pre = pair.Item1; Party p = pair.Item2;
                AddCol(cols, vals, ps, pre + "_first_name", p.First); AddCol(cols, vals, ps, pre + "_middle_name", p.Middle);
                AddCol(cols, vals, ps, pre + "_last_name", p.Last); AddCol(cols, vals, ps, pre + "_date_of_birth", p.Dob);
                AddCol(cols, vals, ps, pre + "_place_of_birth", p.PlaceOfBirth); AddCol(cols, vals, ps, pre + "_sex", p.Sex);
                AddCol(cols, vals, ps, pre + "_birth_country", p.BirthCountry);
                AddCol(cols, vals, ps, pre + "_citizenship", p.Citizenship);
                AddCol(cols, vals, ps, pre + "_civil_status", p.CivilStatus); AddCol(cols, vals, ps, pre + "_religion", p.Religion);
                AddCol(cols, vals, ps, pre + "_residence", p.Residence);
                // The joined _father_name / _mother_name columns are retired (migration 38):
                // not written, so a legacy value stays exactly as it was filed.
                AddCol(cols, vals, ps, pre + "_father_first_name", p.FatherFirst); AddCol(cols, vals, ps, pre + "_father_middle_name", p.FatherMiddle);
                AddCol(cols, vals, ps, pre + "_father_last_name", p.FatherLast);
                AddCol(cols, vals, ps, pre + "_father_citizenship", p.FatherCitizenship); AddCol(cols, vals, ps, pre + "_father_residence", p.FatherResidence);
                AddCol(cols, vals, ps, pre + "_mother_first_name", p.MotherFirst); AddCol(cols, vals, ps, pre + "_mother_middle_name", p.MotherMiddle);
                AddCol(cols, vals, ps, pre + "_mother_last_name", p.MotherLast);
                AddCol(cols, vals, ps, pre + "_mother_citizenship", p.MotherCitizenship); AddCol(cols, vals, ps, pre + "_mother_residence", p.MotherResidence);
                AddCol(cols, vals, ps, pre + "_consent_first_name", p.ConsentFirst); AddCol(cols, vals, ps, pre + "_consent_middle_name", p.ConsentMiddle);
                AddCol(cols, vals, ps, pre + "_consent_last_name", p.ConsentLast); AddCol(cols, vals, ps, pre + "_consent_relationship", p.ConsentRelationship);
                AddCol(cols, vals, ps, pre + "_consent_citizenship", p.ConsentCitizenship); AddCol(cols, vals, ps, pre + "_consent_residence", p.ConsentResidence);

                // Belt and braces with the greyed block on screen: a previous-marriage entry is
                // never written for a party whose civil status says there was none to dissolve.
                bool prev = MarriageRules.IsPreviouslyMarried(p.CivilStatus);
                AddCol(cols, vals, ps, pre + "_prev_dissolution", prev ? p.PrevDissolution : null);
                AddCol(cols, vals, ps, pre + "_prev_dissolved_municipality", prev ? p.PrevDissolvedMunicipality : null);
                AddCol(cols, vals, ps, pre + "_prev_dissolved_province", prev ? p.PrevDissolvedProvince : null);
                AddCol(cols, vals, ps, pre + "_prev_dissolved_date", prev ? p.PrevDissolvedDate : null);
            }

            int id = l.Id;
            if (id > 0)
            {
                ps.Add(P("@id", id));
                Db.Push("UPDATE marriage_licenses SET " +
                        string.Join(", ", cols.Select((c, i) => c + " = " + vals[i])) + " WHERE id = @id", ps.ToArray());
                Audit.Write(Audit.Update, "marriage_licenses", id, "Form 90 " + l.ApplicationNo);
            }
            else
            {
                cols.Add("status"); vals.Add("'Draft'");
                cols.Add("created_by"); vals.Add("@cb"); ps.Add(P("@cb", UserId));
                cols.Add("application_no"); vals.Add("@app");
                MySqlParameter app = P("@app", null); ps.Add(app);
                for (int attempt = 0; ; attempt++)
                {
                    app.Value = NextNumber("marriage_licenses", "application_no", "LA", (l.FiledDate ?? DateTime.Today).Year);
                    try
                    {
                        id = (int)Db.Insert("INSERT INTO marriage_licenses (" + string.Join(", ", cols) + ") VALUES (" +
                                            string.Join(", ", vals) + ")", ps.ToArray());
                        break;
                    }
                    catch (MySqlException ex) when (DuplicateOn(ex, "ux_mlic_application_no") && attempt < RegistryNumber.MaxRetries) { }
                }
                l.Id = id;
                l.ApplicationNo = app.Value.ToString();
                History("License", id, "Application filed", null, "Draft", "Form 90 " + l.ApplicationNo);
                Audit.Write(Audit.Create, "marriage_licenses", id, "Form 90 " + l.ApplicationNo);
            }
            SyncLicenseRequirements(id);
            return id;
        }

        private static void AddCol(List<string> cols, List<string> vals, List<MySqlParameter> ps, string col, object v)
        {
            cols.Add(col); vals.Add("@" + col); ps.Add(P("@" + col, v));
        }

        public static void SyncLicenseRequirements(int id)
        {
            LicenseFacts l = LoadLicense(id);
            if (l == null) return;
            List<Need> needs = MarriageRules.Needs(l.Husband, l.Wife, l.FiledDate ?? DateTime.Today, Catalog(), "License", Settings);
            SyncRequirements("License", id, needs);
            RecomputeEarliest(id);
        }

        /// <summary>Re-derive posting end / earliest issue after anything that can defer it.</summary>
        public static void RecomputeEarliest(int id)
        {
            LicenseFacts l = LoadLicense(id);
            if (l == null || !l.PostingStart.HasValue || l.StoredStatus != "Posting" && l.StoredStatus != "On Hold") return;
            string deferral = MarriageRules.Deferral(l.Requirements);
            DateTime earliest = MarriageRules.EarliestIssue(l.PostingStart.Value, deferral != null, Settings);
            if (l.EarliestIssue != earliest || l.DeferralReason != deferral)
            {
                Db.Push("UPDATE marriage_licenses SET posting_ends=@e, earliest_issue_date=@ei, deferral_reason=@d WHERE id=@id",
                    P("@e", MarriageRules.PostingEnd(l.PostingStart.Value, Settings)), P("@ei", earliest), P("@d", deferral), P("@id", id));
                if (l.EarliestIssue.HasValue)
                    History("License", id, "Earliest issue changed", null, null,
                        MarriageRules.D(l.EarliestIssue) + " -> " + MarriageRules.D(earliest) + (deferral == null ? "" : " (" + deferral + ")"));
            }
        }

        /// <summary>Staff confirm the application is accepted and the notice is up. Returns what stops it, if anything.</summary>
        public static List<RuleIssue> StartPosting(int id, DateTime start)
        {
            LicenseFacts l = LoadLicense(id);
            if (l == null) throw new InvalidOperationException("Application not found.");
            List<RuleIssue> issues = MarriageRules.ValidateForPosting(l, Settings).Where(i => i.Blocks).ToList();
            if (issues.Count > 0) return issues;

            string deferral = MarriageRules.Deferral(l.Requirements);
            DateTime end = MarriageRules.PostingEnd(start, Settings);
            DateTime earliest = MarriageRules.EarliestIssue(start, deferral != null, Settings);
            Db.Push("UPDATE marriage_licenses SET status='Posting', posting_start=@s, posting_ends=@e, earliest_issue_date=@ei, " +
                    "deferral_reason=@d WHERE id=@id AND status='Draft'",
                P("@s", start.Date), P("@e", end), P("@ei", earliest), P("@d", deferral), P("@id", id));
            History("License", id, "Posting started", "Draft", "Posting",
                "Posted " + MarriageRules.D(start) + " - " + MarriageRules.D(end) + "; earliest issue " + MarriageRules.D(earliest));
            Audit.Write(Audit.Update, "marriage_licenses", id, "Posting started " + l.ApplicationNo);
            return issues;
        }

        public static void RecordPayment(int id, string orNo, decimal? amount, DateTime? date)
        {
            if (!string.IsNullOrWhiteSpace(orNo)) PaymentService.EnsureOrFree(orNo, "marriage_licenses", id);
            Db.Push("UPDATE marriage_licenses SET payment_or_no=@o, payment_amount=@a, payment_date=@d WHERE id=@id",
                P("@o", orNo), P("@a", amount), P("@d", date), P("@id", id));
            History("License", id, "Payment recorded", null, null, "O.R. " + orNo + (amount.HasValue ? " PHP " + amount.Value.ToString("0.00") : ""));

            // Into the shared payment log, so the monthly collection report sees it. ONE unitemised line:
            // the licence O.R. may cover the application, licence and solemnization fees together, and
            // which of them it covered is not recorded here - splitting it would invent the breakdown.
            // No amount means nothing to count, so nothing is logged.
            if (!string.IsNullOrWhiteSpace(orNo) && amount.HasValue && amount.Value > 0)
            {
                LicenseFacts l = LoadLicense(id);
                PaymentService.RecordForModule(new PaymentEntry
                {
                    Source = PaymentService.SourceMarriage, SourceTable = "marriage_licenses", SourceId = id,
                    PayerName = l == null ? null : MarriageRules.JoinName(l.Husband.First, null, l.Husband.Last) + " & " + MarriageRules.JoinName(l.Wife.First, null, l.Wife.Last),
                    Purpose = "Marriage licence" + (l == null ? "" : " - application " + l.ApplicationNo),
                    OrNumber = orNo, PaidAt = date ?? DateTime.Today,
                    Lines = { new PaymentLine { Description = "Marriage licence fees (one O.R.)", Quantity = 1, UnitAmount = amount.Value } }
                }, Session.User == null ? (int?)null : Session.User.Id);
            }
        }

        /// <summary>Registrar's recorded finding on a possible impediment (Art. 18).</summary>
        public static void SetImpedimentNote(int id, string note)
        {
            RequireRegistrar("record a finding on an impediment");
            Db.Push("UPDATE marriage_licenses SET impediment_note=@n, registrar_override_by=@u, registrar_override_at=NOW() WHERE id=@id",
                P("@n", note), P("@u", UserId), P("@id", id));
            History("License", id, "Registrar finding", null, null, note);
        }

        /// <summary>
        /// Admin-only power: let a licence issue despite missing/unverified requirement
        /// attachments (a client cannot supply every document, but the office still needs to
        /// issue). Requires a written reason, is Admin-only (narrower than the usual
        /// Registrar-or-Admin gate on this table), and only lifts the requirement checks -
        /// posting, payment, an unresolved impediment and the under-18 hard stop are untouched;
        /// see <see cref="MarriageRules.ApplyOverride"/>. Always audited, on this table and in
        /// audit_log, and the licence itself carries who/when/why permanently.
        /// </summary>
        public static void OverrideRequirements(int id, string reason)
        {
            RequireAdmin("override missing marriage licence requirements");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required to override missing requirements.");
            Db.Push("UPDATE marriage_licenses SET requirements_override_by=@u, requirements_override_at=NOW(), requirements_override_reason=@r WHERE id=@id",
                P("@u", UserId), P("@r", reason), P("@id", id));
            History("License", id, "Requirements overridden by Admin", null, null, reason);
            Audit.Write(Audit.Update, "marriage_licenses", id, "Requirements override recorded: " + reason);
        }

        /// <summary>Withdraws a requirements override (e.g. entered by mistake) before the licence is issued.</summary>
        public static void ClearRequirementsOverride(int id)
        {
            RequireAdmin("withdraw a marriage licence requirements override");
            Db.Push("UPDATE marriage_licenses SET requirements_override_by=NULL, requirements_override_at=NULL, requirements_override_reason=NULL WHERE id=@id",
                P("@id", id));
            History("License", id, "Requirements override withdrawn", null, null, null);
            Audit.Write(Audit.Update, "marriage_licenses", id, "Requirements override withdrawn");
        }

        public static void Hold(int id, string reason)
        {
            LicenseFacts l = LoadLicense(id);
            if (l.StoredStatus != "Posting" && l.StoredStatus != "Draft") throw new InvalidOperationException("Only a draft or posting application can be put on hold.");
            Db.Push("UPDATE marriage_licenses SET status='On Hold', hold_reason=@r, remarks=CONCAT_WS(' | ', remarks, CONCAT('Held from ', @from)) WHERE id=@id",
                P("@r", reason), P("@from", l.StoredStatus), P("@id", id));
            History("License", id, "Put on hold", l.StoredStatus, "On Hold", reason);
        }

        public static void ReleaseHold(int id)
        {
            LicenseFacts l = LoadLicense(id);
            if (l.StoredStatus != "On Hold") return;
            string back = l.PostingStart.HasValue ? "Posting" : "Draft";
            Db.Push("UPDATE marriage_licenses SET status=@s, hold_reason=NULL WHERE id=@id", P("@s", back), P("@id", id));
            History("License", id, "Hold released", "On Hold", back, null);
            RecomputeEarliest(id);
        }

        public static void Cancel(int id, string reason)
        {
            RequireRegistrar("cancel a marriage licence application");
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required to cancel.");
            LicenseFacts l = LoadLicense(id);
            if (l.StoredStatus == "Used" || l.UsedByMarriageId.HasValue)
                throw new InvalidOperationException("A licence a marriage record is linked to cannot be cancelled.");
            Db.Push("UPDATE marriage_licenses SET status='Cancelled', cancel_reason=@r WHERE id=@id", P("@r", reason), P("@id", id));
            History("License", id, "Cancelled", l.StoredStatus, "Cancelled", reason);
            Audit.Write(Audit.Update, "marriage_licenses", id, "Cancelled: " + reason);
        }

        /// <summary>
        /// Issue the licence on the ACTUAL issue date (never defaulted to the posting end).
        /// Re-validates against current data first; returns the blocking issues, empty on success.
        /// </summary>
        public static List<RuleIssue> IssueLicense(int id, DateTime issueDate, out string licenseNo)
        {
            RequireRegistrar("issue a marriage licence");
            licenseNo = null;
            LicenseFacts l = LoadLicense(id);
            if (l == null) throw new InvalidOperationException("Application not found.");
            List<RuleIssue> issues = MarriageRules.ValidateForIssue(l, Catalog(), issueDate, Settings).Where(i => i.Blocks).ToList();
            issues = MarriageRules.ApplyOverride(issues, l);
            if (issueDate.Date > DateTime.Today)
                issues.Add(new RuleIssue(RuleSeverity.Blocking, "FUTURE", "Issue date cannot be in the future.", "Issue License"));
            if (issues.Count > 0) return issues;

            DateTime expiry = MarriageRules.Expiry(issueDate, Settings);
            for (int attempt = 0; ; attempt++)
            {
                string no = PeekNextLicenseNo(issueDate);
                try
                {
                    Tx((c, t) =>
                    {
                        int n = Exec(c, t,
                            "UPDATE marriage_licenses SET status='Issued', license_no=@no, issue_date=@i, expiry_date=@x, " +
                            "issued_by=@u, issued_at=NOW() WHERE id=@id AND status='Posting'",
                            P("@no", no), P("@i", issueDate.Date), P("@x", expiry), P("@u", UserId), P("@id", id));
                        if (n != 1) throw new InvalidOperationException("The application changed while it was being issued - reload it.");
                        string note = no + " issued " + MarriageRules.D(issueDate) + ", valid until " + MarriageRules.D(expiry);
                        if (l.RequirementsOverrideBy.HasValue)
                            note += " [ISSUED WITH REQUIREMENTS OVERRIDDEN: " + l.RequirementsOverrideReason + "]";
                        History("License", id, "Licence issued", "Posting", "Issued", note, c, t);
                    });
                    licenseNo = no;
                    break;
                }
                catch (MySqlException ex) when (DuplicateOn(ex, "ux_mlic_license_no") && attempt < RegistryNumber.MaxRetries) { }
            }
            Audit.Write(Audit.Update, "marriage_licenses", id, "Issued licence " + licenseNo);
            return issues;
        }

        /// <summary>
        /// Persist Expired on licences whose validity has passed, so the stored status is
        /// queryable and the change is in the history. Never deletes. Returns how many moved.
        /// </summary>
        public static int SweepExpired()
        {
            DataTable dt = Db.Pull("SELECT id, license_no, expiry_date FROM marriage_licenses " +
                                   "WHERE status='Issued' AND expiry_date < CURDATE()");
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                if (Db.Push("UPDATE marriage_licenses SET status='Expired' WHERE id=@id AND status='Issued'", P("@id", id)) == 1)
                    History("License", id, "Licence expired", "Issued", "Expired",
                        Str(r["license_no"]) + " passed its last valid day " + MarriageRules.D(Dt(r["expiry_date"])));
            }
            return dt.Rows.Count;
        }

        // ================================================================ marriages
        private static readonly HashSet<string> MarriageColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "form_code", "form_name", "registry_no", "book_volume", "book_page", "status", "solemnizer", "solemnizer_position",
            "husband_first_name", "husband_middle_name", "husband_last_name", "husband_age", "husband_date_of_birth",
            "husband_place_of_birth", "husband_birth_country", "husband_citizenship_id", "husband_religion_id", "husband_civil_status", "husband_residence_id",
            "husband_father_name", "husband_mother_name",
            "wife_first_name", "wife_middle_name", "wife_last_name", "wife_age", "wife_date_of_birth",
            "wife_place_of_birth", "wife_birth_country", "wife_citizenship_id", "wife_religion_id", "wife_civil_status", "wife_residence_id",
            "wife_father_name", "wife_mother_name",
            "church_id", "place_municipality_id", "place_province_id", "date_of_marriage", "time_of_marriage",
            "witness1_name", "witness2_name", "license_id", "license_no", "license_date", "license_place",
            "license_out_of_province",
            "license_basis", "exemption_basis", "exemption_notes", "delay_reason",
            "received_by", "received_by_title", "received_by_date", "remarks", "scan_image"
        };

        private static readonly string[] NotNullNames = { "husband_first_name", "husband_last_name", "wife_first_name", "wife_last_name" };

        /// <summary>
        /// Save Form 97 fields (whitelisted columns only). New rows start as Draft. A
        /// REGISTERED record may be edited only by a registrar, and that edit is recorded in
        /// the history - corrections to a registered entry otherwise go through Petitions.
        /// </summary>
        public static int SaveMarriage(int? id, Dictionary<string, object> values)
        {
            foreach (string k in values.Keys)
                if (!MarriageColumns.Contains(k)) throw new ArgumentException("Not a Form 97 column: " + k);
            if (values.ContainsKey("status") && !(new[] { "Draft", "For Review", "Returned" }).Contains(values["status"] as string))
                throw new ArgumentException("Status is set by the workflow, not by a save.");

            string oldStatus = null;
            if (id.HasValue)
            {
                DataTable cur = Db.Pull("SELECT status FROM marriages WHERE id=@id", P("@id", id.Value));
                if (cur.Rows.Count == 0) throw new InvalidOperationException("Marriage record not found.");
                oldStatus = Str(cur.Rows[0]["status"]);
                if (oldStatus == "Registered")
                {
                    RequireRegistrar("edit a registered marriage");
                    values.Remove("status");
                }
            }

            var ps = new List<MySqlParameter>();
            foreach (var kv in values)
            {
                object v = kv.Value;
                if (Array.IndexOf(NotNullNames, kv.Key) >= 0 && (v == null || v == DBNull.Value)) v = "";
                if (kv.Key == "scan_image")
                    ps.Add(new MySqlParameter("@scan_image", MySqlDbType.LongBlob) { Value = v ?? DBNull.Value });
                else ps.Add(P("@" + kv.Key, v));
            }

            try
            {
                if (id.HasValue)
                {
                    ps.Add(P("@id", id.Value));
                    Db.Push("UPDATE marriages SET " + string.Join(", ", values.Keys.Select(k => k + " = @" + k)) + " WHERE id = @id", ps.ToArray());
                    string newStatus = values.ContainsKey("status") ? (string)values["status"] : oldStatus;
                    History("Marriage", id.Value, oldStatus == "Registered" ? "Edited after registration" : "Saved",
                        oldStatus, newStatus, null);
                    Audit.Write(Audit.Update, "marriages", id.Value, "Form 97 saved (" + newStatus + ")");
                }
                else
                {
                    var keys = values.Keys.ToList();
                    if (!values.ContainsKey("status")) { keys.Add("status"); ps.Add(P("@status", "Draft")); }
                    keys.Add("created_by"); ps.Add(P("@created_by", UserId));
                    foreach (string n in NotNullNames) if (!keys.Contains(n)) { keys.Add(n); ps.Add(P("@" + n, "")); }
                    id = (int)Db.Insert("INSERT INTO marriages (" + string.Join(", ", keys) + ") VALUES (" +
                                        string.Join(", ", keys.Select(k => "@" + k)) + ")", ps.ToArray());
                    History("Marriage", id.Value, "Form 97 received", null, values.ContainsKey("status") ? (string)values["status"] : "Draft", null);
                    Audit.Write(Audit.Create, "marriages", id.Value, "Form 97 draft");
                }
            }
            catch (MySqlException ex) when (DuplicateOn(ex, "ux_marriages_license_id"))
            {
                throw new InvalidOperationException("That licence is already linked to another marriage record. One licence supports one marriage.");
            }
            SyncMarriageRequirements(id.Value);
            return id.Value;
        }

        public static MarriageFacts LoadMarriageFacts(int id)
        {
            DataTable dt = Db.Pull(
                "SELECT m.*, hc.name AS h_cit, wc.name AS w_cit FROM marriages m " +
                "LEFT JOIN nationalities hc ON hc.id = m.husband_citizenship_id " +
                "LEFT JOIN nationalities wc ON wc.id = m.wife_citizenship_id WHERE m.id = @id", P("@id", id));
            if (dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];
            var m = new MarriageFacts
            {
                Id = id, Status = Col(r, "status"), RegistryNo = Col(r, "registry_no"),
                DateOfMarriage = ColD(r, "date_of_marriage"), DateReceived = ColD(r, "received_by_date"),
                CasePostingStart = ColD(r, "case_posting_start"),
                HasPlace = r["place_municipality_id"] != DBNull.Value && r["place_province_id"] != DBNull.Value,
                Solemnizer = Col(r, "solemnizer"), SolemnizerPosition = Col(r, "solemnizer_position"),
                Witness1 = Col(r, "witness1_name"), Witness2 = Col(r, "witness2_name"),
                Basis = Col(r, "license_basis"), LicenseId = Int(r["license_id"]),
                OutOfProvinceLicense = r.Table.Columns.Contains("license_out_of_province") && Int(r["license_out_of_province"]) == 1,
                ExternalLicenseNo = Col(r, "license_no"), ExternalLicenseDate = ColD(r, "license_date"),
                ExemptionBasis = Col(r, "exemption_basis"), DelayReason = Col(r, "delay_reason"),
                RegistrarReview = Col(r, "registrar_review_status"), OcrReviewStatus = Col(r, "ocr_review_status"),
                OcrWeakFields = Int(r["ocr_weak_fields"])
            };
            foreach (var pair in new[] { Tuple.Create("husband", m.Husband, "h_cit"), Tuple.Create("wife", m.Wife, "w_cit") })
            {
                Party p = pair.Item2; string pre = pair.Item1;
                p.First = Col(r, pre + "_first_name"); p.Middle = Col(r, pre + "_middle_name"); p.Last = Col(r, pre + "_last_name");
                p.Dob = ColD(r, pre + "_date_of_birth"); p.CivilStatus = Col(r, pre + "_civil_status"); p.Citizenship = Col(r, pair.Item3);
                p.Father = Col(r, pre + "_father_name"); p.Mother = Col(r, pre + "_mother_name");
            }
            m.Requirements = Requirements("Marriage", id);
            return m;
        }

        public static void SyncMarriageRequirements(int id)
        {
            MarriageFacts m = LoadMarriageFacts(id);
            if (m == null) return;
            bool exempt = m.Basis == "Exempt";
            bool delayed = MarriageRules.WouldBeDelayed(m, Settings);
            List<Need> needs = MarriageRules.Needs(m.Husband, m.Wife, m.DateOfMarriage ?? DateTime.Today, Catalog(), "Marriage",
                                                   Settings, exempt, delayed, m.OutOfProvinceLicense);
            // A licensed marriage proves an ended previous marriage from its licence file.
            if (m.LicenseId.HasValue)
            {
                LicenseFacts lic = LoadLicense(m.LicenseId.Value);
                if (lic != null)
                    needs.RemoveAll(n => n.Code == "PREV_MARRIAGE" &&
                        MarriageRules.Satisfied(lic.Requirements.FirstOrDefault(x => x.Code == "PREV_MARRIAGE" && x.Party == n.Party)));
            }
            SyncRequirements("Marriage", id, needs);
        }

        public static List<RuleIssue> ValidateMarriage(int id)
        {
            MarriageFacts m = LoadMarriageFacts(id);
            if (m == null) throw new InvalidOperationException("Marriage record not found.");
            LicenseFacts lic = m.LicenseId.HasValue ? LoadLicense(m.LicenseId.Value) : null;
            return MarriageRules.ValidateMarriage(m, lic, Catalog(), DateTime.Today, Settings);
        }

        /// <summary>Attach the OCR run this record came from; weak fields hold it for review.</summary>
        public static void SetOcrContext(int id, string scanId, int confidence, int weakFields, bool needsReview)
        {
            string review = needsReview || weakFields > 0 ? "Required" : "Completed";
            Db.Push("UPDATE marriages SET ocr_scan_id=@s, ocr_confidence=@c, ocr_weak_fields=@w, ocr_review_status=@r WHERE id=@id",
                P("@s", scanId), P("@c", confidence), P("@w", weakFields), P("@r", review), P("@id", id));
            History("Marriage", id, "Linked to scan", null, null,
                (scanId ?? "scan") + ": " + confidence + "% confidence, " + weakFields + " weak field(s); review " + review.ToLowerInvariant());
        }

        public static void MarkOcrReviewed(int id)
        {
            Db.Push("UPDATE marriages SET ocr_review_status='Completed', ocr_reviewed_by=@u, ocr_reviewed_at=NOW() WHERE id=@id",
                P("@u", UserId), P("@id", id));
            History("Marriage", id, "OCR review completed", "Required", "Completed", "Fields compared against the source scan");
        }

        public static void StartCasePosting(int id, DateTime start)
        {
            Db.Push("UPDATE marriages SET case_posting_start=@s WHERE id=@id", P("@s", start.Date), P("@id", id));
            History("Marriage", id, "Delayed-registration notice posted", null, null,
                MarriageRules.D(start) + " - " + MarriageRules.D(start.AddDays(Settings.DelayedPostingDays - 1)));
        }

        public static void RegistrarReview(int id, bool approve, string notes)
        {
            RequireRegistrar("approve or return a delayed / licence-exempt case");
            if (!approve && string.IsNullOrWhiteSpace(notes)) throw new ArgumentException("Say why the case is returned.");
            string st = approve ? "Approved" : "Returned";
            Db.Push("UPDATE marriages SET registrar_review_status=@s, registrar_review_by=@u, registrar_review_at=NOW(), " +
                    "registrar_review_notes=@n WHERE id=@id", P("@s", st), P("@u", UserId), P("@n", notes), P("@id", id));
            History("Marriage", id, "Registrar review", null, st, notes);
        }

        public static void ReturnForCorrection(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A reason is required.");
            DataTable cur = Db.Pull("SELECT status FROM marriages WHERE id=@id", P("@id", id));
            string old = Str(cur.Rows[0]["status"]);
            if (old == "Registered") throw new InvalidOperationException("A registered marriage is corrected through Petitions, not returned.");
            Db.Push("UPDATE marriages SET status='Returned', return_reason=@r WHERE id=@id", P("@r", reason), P("@id", id));
            History("Marriage", id, "Returned for correction", old, "Returned", reason);
        }

        /// <summary>
        /// Register the marriage: validate against current data, assign the registry number,
        /// stamp who/when, mark the licence Used, open the four copy rows - all in one
        /// transaction. Returns the blocking issues; empty on success.
        /// </summary>
        public static List<RuleIssue> Register(int id, out string registryNo)
        {
            RequireRegistrar("register a marriage");
            registryNo = null;
            MarriageFacts m = LoadMarriageFacts(id);
            if (m == null) throw new InvalidOperationException("Marriage record not found.");
            if (m.Status == "Registered") throw new InvalidOperationException("Already registered as " + m.RegistryNo + ".");
            LicenseFacts lic = m.LicenseId.HasValue ? LoadLicense(m.LicenseId.Value) : null;
            List<RuleIssue> issues = MarriageRules.ValidateMarriage(m, lic, Catalog(), DateTime.Today, Settings)
                                                  .Where(i => i.Blocks).ToList();
            if (issues.Count > 0) return issues;

            bool delayed = MarriageRules.WouldBeDelayed(m, Settings);
            string office = null;
            try { office = Str(Db.Pull("SELECT office_name FROM office_profile LIMIT 1").Rows[0][0]); } catch { }

            for (int attempt = 0; ; attempt++)
            {
                string reg = string.IsNullOrWhiteSpace(m.RegistryNo) ? RegistryNumber.Next("marriages", 'M') : m.RegistryNo;
                try
                {
                    Tx((c, t) =>
                    {
                        int n = Exec(c, t,
                            "UPDATE marriages SET status='Registered', registry_no=@reg, registration_type=@rt, date_registered=CURDATE(), " +
                            "registered_by=@u, registered_at=NOW(), return_reason=NULL WHERE id=@id AND status <> 'Registered'",
                            P("@reg", reg), P("@rt", delayed ? "Delayed" : "Timely"), P("@u", UserId), P("@id", id));
                        if (n != 1) throw new InvalidOperationException("The record changed while it was being registered - reload it.");

                        if (lic != null)
                        {
                            Exec(c, t, "UPDATE marriage_licenses SET status='Used' WHERE id=@id", P("@id", lic.Id));
                            History("License", lic.Id, "Licence used", lic.StoredStatus, "Used", "Marriage registered as " + reg, c, t);
                        }

                        const string ins = "INSERT IGNORE INTO marriage_copies (marriage_id, copy_type, intended_for, status, recipient, disposition_date, staff_id) " +
                                           "VALUES (@m, @t, @f, @s, @r, @d, @u)";
                        Exec(c, t, ins, P("@m", id), P("@t", "Original"), P("@f", "Contracting party - furnished by the solemnizing officer (Art. 23)"), P("@s", "Pending"), P("@r", null), P("@d", null), P("@u", null));
                        Exec(c, t, ins, P("@m", id), P("@t", "Duplicate"), P("@f", "LCRO registry file"), P("@s", "Filed"), P("@r", office), P("@d", DateTime.Today), P("@u", UserId));
                        Exec(c, t, ins, P("@m", id), P("@t", "Triplicate"), P("@f", "PSA / OCRG - through the monthly transmittal"), P("@s", "Pending"), P("@r", null), P("@d", null), P("@u", null));
                        Exec(c, t, ins, P("@m", id), P("@t", "Quadruplicate"), P("@f", "Solemnizing officer's file (Art. 23)"), P("@s", "Pending"), P("@r", null), P("@d", null), P("@u", null));

                        History("Marriage", id, "Registered", m.Status, "Registered",
                            reg + " - " + (delayed ? "DELAYED" : "timely") + " registration" +
                            (lic != null ? ", licence " + lic.LicenseNo : m.Basis == "Exempt" ? ", licence-exempt (" + m.ExemptionBasis + ")" : ""), c, t);
                    });
                    registryNo = reg;
                    break;
                }
                catch (MySqlException ex) when (RegistryNumber.WasTaken(ex, "marriages") && attempt < RegistryNumber.MaxRetries &&
                                                string.IsNullOrWhiteSpace(m.RegistryNo)) { }
            }
            Audit.Write(Audit.Update, "marriages", id, "Registered as " + registryNo);
            return issues;
        }

        public static void ConfirmPsaAvailability(int id, DateTime date, string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new ArgumentException("PSA availability is only recorded against an authoritative reference (e.g. the PSA copy's serial or PSA acknowledgement).");
            Db.Push("UPDATE marriages SET psa_available_date=@d, psa_available_reference=@r WHERE id=@id",
                P("@d", date.Date), P("@r", reference), P("@id", id));
            History("Marriage", id, "PSA availability confirmed", null, null, reference);
        }

        // ================================================================ copies
        public static DataTable Copies(int marriageId)
        {
            return Db.Pull("SELECT c.id, c.copy_type, c.intended_for, c.status, c.recipient, c.disposition_date, c.reference_no, " +
                           "c.remarks, COALESCE(u.full_name, u.username) AS staff FROM marriage_copies c " +
                           "LEFT JOIN users u ON u.id = c.staff_id WHERE c.marriage_id = @m " +
                           "ORDER BY FIELD(c.copy_type,'Original','Duplicate','Triplicate','Quadruplicate')", P("@m", marriageId));
        }

        public static void UpdateCopy(int copyId, string status, string recipient, DateTime? date, string reference, string remarks)
        {
            if (Array.IndexOf(CopyStatuses, status) < 0) throw new ArgumentException("Unknown copy status " + status);
            DataTable cur = Db.Pull("SELECT marriage_id, copy_type, status FROM marriage_copies WHERE id=@id", P("@id", copyId));
            if (cur.Rows.Count == 0) throw new InvalidOperationException("Copy not found.");
            Db.Push("UPDATE marriage_copies SET status=@s, recipient=@r, disposition_date=@d, reference_no=@ref, remarks=@rem, staff_id=@u WHERE id=@id",
                P("@s", status), P("@r", recipient), P("@d", date), P("@ref", reference), P("@rem", remarks), P("@u", UserId), P("@id", copyId));
            History("Marriage", Convert.ToInt32(cur.Rows[0]["marriage_id"]), "Copy " + Str(cur.Rows[0]["copy_type"]),
                Str(cur.Rows[0]["status"]), status, recipient);
        }

        // ================================================================ PSA / OCRG
        /// <summary>
        /// Derived transmittal status of a marriage. Legacy registrations (no CROMS registration
        /// date) are reported as unknown rather than "pending" - CROMS cannot know whether a
        /// migrated record was ever transmitted.
        /// </summary>
        public static string PsaStatusText(string itemStatus, bool legacy)
        {
            if (legacy && string.IsNullOrEmpty(itemStatus)) return "Unknown (legacy record)";
            switch (itemStatus)
            {
                case null: case "": return "Pending Transmittal";
                case "Included": return "In Batch";
                case "Sent": return "Sent to PSA/OCRG";
                case "Acknowledged": return "Acknowledged";
                case "Returned": return "Returned - Needs Correction";
                default: return itemStatus;
            }
        }

        private const string LatestItemSql =
            "(SELECT i.status FROM psa_transmittal_items i WHERE i.record_table='marriages' AND i.record_id=m.id ORDER BY i.id DESC LIMIT 1)";
        private const string LatestBatchSql =
            "(SELECT b.batch_no FROM psa_transmittal_items i JOIN psa_transmittal_batches b ON b.id=i.batch_id " +
            " WHERE i.record_table='marriages' AND i.record_id=m.id ORDER BY i.id DESC LIMIT 1)";

        public static string PsaStatus(int marriageId, out string batchNo)
        {
            DataTable dt = Db.Pull("SELECT date_registered, " + LatestItemSql + " AS st, " + LatestBatchSql + " AS bn FROM marriages m WHERE m.id=@id",
                P("@id", marriageId));
            batchNo = null;
            if (dt.Rows.Count == 0) return "-";
            batchNo = Str(dt.Rows[0]["bn"]);
            return PsaStatusText(Str(dt.Rows[0]["st"]), dt.Rows[0]["date_registered"] == DBNull.Value);
        }

        /// <summary>Registered marriages with their derived PSA status.</summary>
        public static DataTable PsaQueue()
        {
            DataTable dt = Db.Pull(
                "SELECT m.id, m.registry_no AS `Registry No`, " +
                "CONCAT(m.husband_last_name, ', ', m.husband_first_name, '  &  ', m.wife_last_name, ', ', m.wife_first_name) AS `Couple`, " +
                "m.date_of_marriage AS `Marriage Date`, m.date_registered AS `Registered`, m.registration_type AS `Type`, " +
                LatestItemSql + " AS item_status, " + LatestBatchSql + " AS `Batch`, " +
                "(m.date_registered IS NULL) AS legacy, m.psa_available_reference AS psa_ref " +
                "FROM marriages m WHERE m.status='Registered' ORDER BY m.date_registered DESC, m.id DESC");
            dt.Columns.Add("Status", typeof(string));
            foreach (DataRow r in dt.Rows)
                r["Status"] = PsaStatusText(Str(r["item_status"]), Convert.ToInt32(r["legacy"]) != 0);
            return dt;
        }

        /// <summary>Group registered marriages into a draft transmittal batch. Throws on any ineligible record.</summary>
        public static int CreateBatch(IList<int> marriageIds, int year, int month, out string batchNo)
        {
            if (marriageIds == null || marriageIds.Count == 0) throw new ArgumentException("Select at least one registered marriage.");
            DataTable q = PsaQueue();
            foreach (int id in marriageIds)
            {
                DataRow r = q.AsEnumerable().FirstOrDefault(x => Convert.ToInt32(x["id"]) == id);
                if (r == null) throw new InvalidOperationException("Record #" + id + " is not a registered marriage.");
                string st = Str(r["Status"]);
                if (st != "Pending Transmittal" && st != "Returned - Needs Correction")
                    throw new InvalidOperationException(Str(r["Registry No"]) + " is " + st + " and cannot be added to a new batch.");
            }

            string prefix = string.Format("PSA-MAR-{0:D4}-{1:D2}-", year, month);
            string no = null;
            int batchId = 0;
            Tx((c, t) =>
            {
                object n = Scalar(c, t, "SELECT COUNT(*) + 1 FROM psa_transmittal_batches WHERE batch_no LIKE @p FOR UPDATE", P("@p", prefix + "%"));
                no = prefix + Convert.ToInt32(n).ToString("D2");
                batchId = (int)Insert(c, t,
                    "INSERT INTO psa_transmittal_batches (batch_no, record_kind, period_year, period_month, date_prepared, status, prepared_by) " +
                    "VALUES (@no, 'Marriage', @y, @m, CURDATE(), 'Draft', @u)", P("@no", no), P("@y", year), P("@m", month), P("@u", UserId));
                foreach (int id in marriageIds)
                {
                    Exec(c, t, "INSERT INTO psa_transmittal_items (batch_id, record_table, record_id, status) VALUES (@b, 'marriages', @r, 'Included')",
                        P("@b", batchId), P("@r", id));
                    History("Marriage", id, "Included in PSA batch", null, "In Batch", no, c, t);
                }
                History("Batch", batchId, "Batch created", null, "Draft", marriageIds.Count + " record(s)", c, t);
            });
            batchNo = no;
            Audit.Write(Audit.Create, "psa_transmittal_batches", batchId, no);
            return batchId;
        }

        public static void RemoveFromBatch(int batchId, int marriageId)
        {
            DataTable b = Db.Pull("SELECT status, batch_no FROM psa_transmittal_batches WHERE id=@id", P("@id", batchId));
            if (b.Rows.Count == 0 || Str(b.Rows[0]["status"]) != "Draft") throw new InvalidOperationException("Only a draft batch can be changed.");
            Db.Push("DELETE FROM psa_transmittal_items WHERE batch_id=@b AND record_table='marriages' AND record_id=@r",
                P("@b", batchId), P("@r", marriageId));
            History("Marriage", marriageId, "Removed from PSA batch", "In Batch", null, Str(b.Rows[0]["batch_no"]));
        }

        public static void MarkBatchSent(int batchId, DateTime date, string method, string office, string reference, string remarks)
        {
            if (string.IsNullOrWhiteSpace(method)) throw new ArgumentException("Submission method is required.");
            if (string.IsNullOrWhiteSpace(office)) throw new ArgumentException("Receiving office is required.");
            if (date.Date > DateTime.Today) throw new ArgumentException("Submission date cannot be in the future.");
            Tx((c, t) =>
            {
                int n = Exec(c, t,
                    "UPDATE psa_transmittal_batches SET status='Sent', date_sent=@d, method=@m, receiving_office=@o, reference_no=@r, " +
                    "remarks=@rem, sent_by=@u WHERE id=@id AND status='Draft'",
                    P("@d", date.Date), P("@m", method), P("@o", office), P("@r", reference), P("@rem", remarks), P("@u", UserId), P("@id", batchId));
                if (n != 1) throw new InvalidOperationException("Only a draft batch can be marked sent.");
                Exec(c, t, "UPDATE psa_transmittal_items SET status='Sent' WHERE batch_id=@b AND status='Included'", P("@b", batchId));
                Exec(c, t,
                    "UPDATE marriage_copies mc JOIN psa_transmittal_items i ON i.record_table='marriages' AND i.record_id=mc.marriage_id " +
                    "SET mc.status='Sent', mc.recipient=@o, mc.disposition_date=@d, mc.reference_no=@r, mc.staff_id=@u " +
                    "WHERE i.batch_id=@b AND mc.copy_type='Triplicate'",
                    P("@o", office), P("@d", date.Date), P("@r", reference), P("@u", UserId), P("@b", batchId));
                foreach (DataRow r in ItemsOf(c, t, batchId).Rows)
                    History("Marriage", Convert.ToInt32(r["record_id"]), "Sent to PSA/OCRG", "In Batch", "Sent", method + " to " + office, c, t);
                History("Batch", batchId, "Marked sent", "Draft", "Sent", method + " to " + office + (reference == null ? "" : ", ref " + reference), c, t);
            });
            Audit.Write(Audit.Update, "psa_transmittal_batches", batchId, "Marked sent");
        }

        public static void AcknowledgeBatch(int batchId, DateTime date, string reference)
        {
            Tx((c, t) =>
            {
                int n = Exec(c, t, "UPDATE psa_transmittal_batches SET status='Acknowledged', ack_date=@d, ack_reference=@r WHERE id=@id AND status='Sent'",
                    P("@d", date.Date), P("@r", reference), P("@id", batchId));
                if (n != 1) throw new InvalidOperationException("Only a sent batch can be acknowledged.");
                Exec(c, t, "UPDATE psa_transmittal_items SET status='Acknowledged' WHERE batch_id=@b AND status='Sent'", P("@b", batchId));
                Exec(c, t,
                    "UPDATE marriage_copies mc JOIN psa_transmittal_items i ON i.record_table='marriages' AND i.record_id=mc.marriage_id " +
                    "SET mc.status='Acknowledged' WHERE i.batch_id=@b AND i.status='Acknowledged' AND mc.copy_type='Triplicate'", P("@b", batchId));
                foreach (DataRow r in ItemsOf(c, t, batchId).Rows)
                    if (Str(r["status"]) == "Acknowledged")
                        History("Marriage", Convert.ToInt32(r["record_id"]), "PSA/OCRG acknowledged", "Sent", "Acknowledged", reference, c, t);
                History("Batch", batchId, "Acknowledged", "Sent", "Acknowledged", reference, c, t);
            });
            Audit.Write(Audit.Update, "psa_transmittal_batches", batchId, "Acknowledged " + reference);
        }

        /// <summary>PSA/OCRG returned one record for correction. It becomes eligible for a new batch.</summary>
        public static void ReturnItem(int marriageId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("State what PSA/OCRG asked to be corrected.");
            DataTable it = Db.Pull("SELECT id, status FROM psa_transmittal_items WHERE record_table='marriages' AND record_id=@r ORDER BY id DESC LIMIT 1",
                P("@r", marriageId));
            if (it.Rows.Count == 0) throw new InvalidOperationException("This record has not been transmitted.");
            string st = Str(it.Rows[0]["status"]);
            if (st != "Sent" && st != "Acknowledged") throw new InvalidOperationException("Only a sent or acknowledged record can be returned (this one is " + st + ").");
            Db.Push("UPDATE psa_transmittal_items SET status='Returned', return_reason=@r, returned_at=NOW() WHERE id=@id",
                P("@r", reason), P("@id", Convert.ToInt32(it.Rows[0]["id"])));
            History("Marriage", marriageId, "Returned by PSA/OCRG", st, "Returned", reason);
            Audit.Write(Audit.Update, "marriages", marriageId, "Returned by PSA/OCRG: " + reason);
        }

        private static DataTable ItemsOf(MySqlConnection c, MySqlTransaction t, int batchId)
        {
            var dt = new DataTable();
            using (var cmd = new MySqlCommand("SELECT record_id, status FROM psa_transmittal_items WHERE batch_id=@b", c, t))
            {
                cmd.Parameters.Add(P("@b", batchId));
                using (var a = new MySqlDataAdapter(cmd)) a.Fill(dt);
            }
            return dt;
        }

        public static DataTable Batches()
        {
            return Db.Pull(
                "SELECT b.id, b.batch_no AS `Batch`, CONCAT(b.period_year, '-', LPAD(b.period_month,2,'0')) AS `Period`, " +
                "b.status AS `Status`, (SELECT COUNT(*) FROM psa_transmittal_items i WHERE i.batch_id=b.id) AS `Records`, " +
                "b.date_prepared AS `Prepared`, b.date_sent AS `Sent`, b.method AS `Method`, b.receiving_office AS `Receiving Office`, " +
                "b.reference_no AS `Reference`, b.ack_date AS `Acknowledged`, b.ack_reference AS `Ack. Ref.` " +
                "FROM psa_transmittal_batches b ORDER BY b.id DESC");
        }

        public static DataTable BatchItems(int batchId)
        {
            return Db.Pull(
                "SELECT i.record_id AS id, m.registry_no AS `Registry No`, " +
                "CONCAT(m.husband_last_name, ' & ', m.wife_last_name) AS `Couple`, m.date_of_marriage AS `Marriage Date`, " +
                "i.status AS `Item Status`, i.return_reason AS `Return Reason` " +
                "FROM psa_transmittal_items i JOIN marriages m ON m.id=i.record_id " +
                "WHERE i.batch_id=@b AND i.record_table='marriages' ORDER BY m.registry_no", P("@b", batchId));
        }
    }
}

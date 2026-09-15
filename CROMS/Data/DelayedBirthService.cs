using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Every write for a delayed birth registration case. Requirements themselves go through
    /// <see cref="MarriageService"/> unchanged (Requirements/SaveRequirement/SyncRequirements/
    /// Catalog are already generic on owner type) - this class owns only what is specific to a
    /// birth: loading/saving the case facts on <c>births</c>, and the posting/evaluation writes.
    /// </summary>
    public static class DelayedBirthService
    {
        public const string Owner = "Birth";
        public const string EvidenceGroup = "BIRTH_EVIDENCE";

        private static MySqlParameter P(string n, object v) { return new MySqlParameter(n, v ?? DBNull.Value); }
        private static string S(object v) { return v == DBNull.Value ? null : v.ToString(); }
        private static bool? B(object v) { return v == DBNull.Value ? (bool?)null : Convert.ToInt32(v) != 0; }
        private static DateTime? Dt(object v) { return v == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(v); }

        /// <summary>The posting period, from app_settings - a setting, not a constant, so the
        /// office can change it without a rebuild (same reasoning as BREQS/marriage). Falls back
        /// to the statutory 10 days if the setting row is missing.</summary>
        public static int PostingDays
        {
            get
            {
                try
                {
                    DataTable t = Db.Pull("SELECT setting_value FROM app_settings WHERE setting_key = 'BIRTH_DELAYED_POSTING_DAYS'");
                    int n;
                    if (t.Rows.Count > 0 && int.TryParse(t.Rows[0][0].ToString(), out n) && n > 0) return n;
                }
                catch { /* app_settings missing or migration 42 not applied yet */ }
                return 10; // PSA MC 2024-17's own posting period, if the setting row is unreadable
            }
        }

        public static List<ReqType> Catalog()
        {
            return MarriageService.Catalog().Where(t => string.Equals(t.AppliesTo, Owner, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static List<ReqRow> Requirements(int birthId)
        {
            return MarriageService.Requirements(Owner, birthId);
        }

        /// <summary>Recomputes which requirements apply from the case's current facts and adds/
        /// drops rows to match - the same sync the marriage licence runs after every save, so a
        /// case that starts "parents married unknown" and is later corrected doesn't need a
        /// second screen to fix its requirement list.</summary>
        public static void SyncRequirements(DelayedBirthCase c)
        {
            List<Need> needs = DelayedBirthRules.Needs(c, Catalog());
            MarriageService.SyncRequirements(Owner, c.BirthId, needs);
        }

        public static DelayedBirthCase Load(int birthId)
        {
            DataTable t = Db.Pull(
                "SELECT id, CONCAT_WS(' ', first_name, middle_name, last_name) AS child_name, registry_no, date_of_birth, " +
                "date_registered, parents_married, delayed_posting_start, delayed_posting_end, delayed_registrant_deceased, " +
                "delayed_mother_unavailable, delayed_parent_deceased, delayed_evaluation, delayed_evaluation_by, delayed_evaluation_at " +
                "FROM births WHERE id = @id", P("@id", birthId));
            if (t.Rows.Count == 0) return null;
            DataRow r = t.Rows[0];
            var c = new DelayedBirthCase
            {
                BirthId = birthId, ChildName = S(r["child_name"]), RegistryNo = S(r["registry_no"]),
                DateOfBirth = Dt(r["date_of_birth"]), DateRegistered = Dt(r["date_registered"]),
                ParentsMarried = B(r["parents_married"]),
                RegistrantDeceased = B(r["delayed_registrant_deceased"]) == true,
                MotherUnavailable = B(r["delayed_mother_unavailable"]) == true,
                ParentDeceased = B(r["delayed_parent_deceased"]) == true,
                PostingStart = Dt(r["delayed_posting_start"]), PostingEnd = Dt(r["delayed_posting_end"]),
                Evaluation = S(r["delayed_evaluation"]),
                EvaluationBy = r["delayed_evaluation_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["delayed_evaluation_by"]),
                EvaluationAt = Dt(r["delayed_evaluation_at"])
            };
            return c;
        }

        /// <summary>Saves the case's conditional facts (deceased registrant / parents married /
        /// mother unavailable / parent deceased - parents_married lives on births already and is
        /// read, never written, here) and re-syncs the requirement list to match.</summary>
        public static void SaveCase(DelayedBirthCase c, int? userId)
        {
            Db.Push(
                "UPDATE births SET delayed_registrant_deceased=@rd, delayed_mother_unavailable=@mu, delayed_parent_deceased=@pd WHERE id=@id",
                P("@rd", c.RegistrantDeceased ? 1 : 0), P("@mu", c.MotherUnavailable ? 1 : 0), P("@pd", c.ParentDeceased ? 1 : 0), P("@id", c.BirthId));
            SyncRequirements(c);
            Audit.Write(Audit.Update, "births", c.BirthId, "Delayed registration case facts updated.");
        }

        /// <summary>Starts the posting period. Refuses a start date in the future - the posting
        /// is the notice going up TODAY (or a day already passed), not a date not yet reached,
        /// the same rule the marriage licence's own StartPosting enforces.</summary>
        public static void StartPosting(int birthId, DateTime start, int? userId)
        {
            if (start.Date > DateTime.Today)
                throw new InvalidOperationException("The posting start is the day the notice actually goes up - it cannot be in the future.");
            DateTime end = DelayedBirthRules.PostingEnd(start.Date, PostingDays);
            Db.Push("UPDATE births SET delayed_posting_start=@s, delayed_posting_end=@e WHERE id=@id",
                    P("@s", start.Date), P("@e", end), P("@id", birthId));
            Audit.Write(Audit.Update, "births", birthId, "Delayed registration posting started " +
                        start.Date.ToString("yyyy-MM-dd") + " - " + end.ToString("yyyy-MM-dd") + ".");
        }

        /// <summary>The registrar's own finding, in their own words - never a computed verdict.
        /// Whether the case is "ready" is shown to them (AllSatisfied); whether it is APPROVED is
        /// their call, recorded here.</summary>
        public static void SetEvaluation(int birthId, string note, int? userId)
        {
            Db.Push("UPDATE births SET delayed_evaluation=@n, delayed_evaluation_by=@u, delayed_evaluation_at=NOW() WHERE id=@id",
                    P("@n", string.IsNullOrWhiteSpace(note) ? null : note.Trim()), P("@u", userId), P("@id", birthId));
            Audit.Write(Audit.Update, "births", birthId, "Delayed registration evaluation recorded.");
        }

        /// <summary>
        /// ADMIN-ONLY escape hatch: marks every requirement on this case Verified - including one
        /// with no attachment on file - so the checklist reads complete even though the paperwork
        /// was not actually checked, and records who did it and why. This does not change the
        /// underlying documents; it overrides the checklist that is normally the registrar's
        /// evidence of having checked them. Caller (the form) is responsible for re-verifying the
        /// acting user actually holds the Admin role before calling this - this method trusts the
        /// id/username/note it is given and only records them.
        /// </summary>
        public static void AdminOverride(int birthId, string reason, int adminUserId, string adminUsername)
        {
            List<ReqRow> rows = Requirements(birthId);
            foreach (ReqRow r in rows)
            {
                if (r.Status == "Verified") continue;
                r.Status = "Verified";
                MarriageService.SaveRequirement(r);
            }
            string note = "ADMIN OVERRIDE by " + (adminUsername ?? "admin") + " - requirements bypassed without full verification." +
                          (string.IsNullOrWhiteSpace(reason) ? "" : " Reason: " + reason.Trim());
            Db.Push("UPDATE births SET delayed_evaluation=@n, delayed_evaluation_by=@u, delayed_evaluation_at=NOW() WHERE id=@id",
                    P("@n", note), P("@u", adminUserId), P("@id", birthId));
            Audit.Write(Audit.Update, "births", birthId,
                "Delayed registration requirements BYPASSED by admin override (" + (adminUsername ?? "admin") + ").");
        }
    }
}

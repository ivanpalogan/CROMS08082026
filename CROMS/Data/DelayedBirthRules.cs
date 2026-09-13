using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// Facts about ONE birth's delayed-registration case - kept separate from the general
    /// <see cref="Party"/>/<see cref="LicenseFacts"/> shapes because a delayed birth is not a
    /// two-party thing: it is one registrant, whose parents may or may not be married, may not
    /// be reachable, and may themselves be deceased.
    /// </summary>
    public sealed class DelayedBirthCase
    {
        public int BirthId;
        public string ChildName, RegistryNo;
        public DateTime? DateOfBirth, DateRegistered;
        public bool RegistrantDeceased;
        /// <summary>From births.parents_married - null means not stated, which the office's own
        /// item (g) treats as "not yet known" rather than either branch.</summary>
        public bool? ParentsMarried;
        public bool MotherUnavailable;
        public bool ParentDeceased;
        public DateTime? PostingStart, PostingEnd;
        public string Evaluation;
        public int? EvaluationBy;
        public DateTime? EvaluationAt;
    }

    /// <summary>
    /// Rules for PSA MC 2024-17 "Requirements for Delayed Registration [of Birth]" - the office's
    /// own checklist, transcribed into the backlog and the migration 42 seed rows. Pure functions
    /// only; every write lives in <see cref="DelayedBirthService"/>.
    /// </summary>
    public static class DelayedBirthRules
    {
        /// <summary>RA 3753's 30-day reglementary period - past this, a birth is delayed. Matches
        /// the threshold BirthRegistrationForm.RecomputeDelayed already applies to is_delayed, so
        /// this asks the same question the flag already answers rather than a second one.</summary>
        public const int ReglementaryDays = 30;

        public static bool IsDelayed(DateTime dateOfBirth, DateTime on)
        {
            return (on.Date - dateOfBirth.Date).TotalDays > ReglementaryDays;
        }

        public static DateTime PostingEnd(DateTime start, int postingDays)
        {
            return start.AddDays(postingDays);
        }

        /// <summary>
        /// Which of the office's checklist items apply to this case, and why - the "Always" items
        /// every delayed registration needs, plus the conditional ones PSA MC 2024-17 lists under
        /// (f)/(g)/(h): only when the registrant is deceased, only when the parents are or are not
        /// married, only when the mother cannot be reached, only when a parent has died. Nothing
        /// here reads the "any two of eight" evidence group as blocking per row - that group is
        /// evaluated as a whole by <see cref="EvidenceGroupSatisfied"/>, never by demanding all
        /// eight or by silently accepting one.
        /// </summary>
        public static List<Need> Needs(DelayedBirthCase c, IEnumerable<ReqType> catalog)
        {
            var needs = new List<Need>();
            foreach (ReqType t in catalog.Where(x => x.Active && string.Equals(x.AppliesTo, "Birth", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(x => x.Sort))
            {
                switch (t.RuleKey)
                {
                    case "Always":
                        needs.Add(N(t, "Both", t.GroupCode != null
                            ? "Counts toward the any-" + t.GroupMin + "-of-" + GroupSize(catalog, t.GroupCode) + " evidence requirement."
                            : "Required for every delayed registration."));
                        break;
                    case "RegistrantDeceased":
                        if (c.RegistrantDeceased) needs.Add(N(t, "Both", "The registrant is deceased."));
                        break;
                    case "ParentsMarried":
                        if (c.ParentsMarried == true) needs.Add(N(t, "Both", "The parents are married."));
                        break;
                    case "ParentsUnmarried":
                        if (c.ParentsMarried == false) needs.Add(N(t, "Both", "The parents are not married (RA 9255)."));
                        break;
                    case "MotherUnavailable":
                        if (c.MotherUnavailable) needs.Add(N(t, "Both", "The mother is not available."));
                        break;
                    case "ParentDeceased":
                        if (c.ParentDeceased) needs.Add(N(t, "Both", "A parent is deceased."));
                        break;
                }
            }
            return needs;
        }

        private static int GroupSize(IEnumerable<ReqType> catalog, string groupCode)
        {
            return catalog.Count(t => t.GroupCode == groupCode);
        }

        private static Need N(ReqType t, string party, string reason)
        {
            return new Need { Code = t.Code, Label = t.Label, Party = party, Reason = reason, Basis = t.Basis, Blocking = t.Blocking, Sort = t.Sort };
        }

        /// <summary>
        /// The office's item (c): "ANY TWO of the following evidences of birth". Satisfied when
        /// at least <c>GroupMin</c> of the group's rows are Verified - not when every row is
        /// Verified (that would demand all eight) and not when any one row is (that would accept
        /// a single document as if it were two).
        /// </summary>
        public static bool EvidenceGroupSatisfied(IEnumerable<ReqRow> rows, IEnumerable<ReqType> catalog, string groupCode, out int have, out int need)
        {
            var group = catalog.Where(t => string.Equals(t.AppliesTo, "Birth", StringComparison.OrdinalIgnoreCase) && t.GroupCode == groupCode).ToList();
            var codes = new HashSet<string>(group.Select(t => t.Code));
            need = group.Count > 0 ? group[0].GroupMin : 1;
            have = rows.Count(r => codes.Contains(r.Code) && r.Status == "Verified");
            return have >= need;
        }

        /// <summary>Every blocking requirement (outside the evidence group) is Verified, AND the
        /// evidence group itself is satisfied. Used to tell the registrar the case is complete -
        /// never to gate the record from being saved, which stays the registrar's own call.</summary>
        public static bool AllSatisfied(List<Need> needs, List<ReqRow> rows, IEnumerable<ReqType> catalog, string groupCode)
        {
            bool group = EvidenceGroupSatisfied(rows, catalog, groupCode, out int have, out int need);
            bool rest = needs.Where(n => n.Blocking && catalog.FirstOrDefault(t => t.Code == n.Code)?.GroupCode == null)
                              .All(n => { ReqRow r = RowFor(rows, n); return r != null && r.Status == "Verified"; });
            return group && rest;
        }

        private static ReqRow RowFor(IEnumerable<ReqRow> rows, Need n)
        {
            return rows.FirstOrDefault(r => r.Code == n.Code && r.Party == n.Party);
        }

        public static string D(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "-";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// Which supporting documents apply to a petition/case, by its petition_type - reuses the
    /// marriage licence's requirements engine (marriage_requirement_types / marriage_requirements
    /// / RequirementsGrid are already generic on owner_type) with owner_type "Petition", the same
    /// way DelayedBirthRules/DelayedBirthService reused it for owner_type "Birth". Pure rules only
    /// live here; every write lives in <see cref="PetitionDocumentService"/>.
    /// </summary>
    public static class PetitionRules
    {
        /// <summary>A catalog row applies to a petition when it is marked 'Always' (every case
        /// type filed through this tracker) or when its rule_key equals THIS case's own
        /// petition_type code - so a Court-Order-only document never shows up on an RA 9048 case,
        /// and a new type can get its own checklist later by adding rows with its own code as the
        /// rule_key, with no code change here.</summary>
        public static List<Need> Needs(string petitionTypeCode, IEnumerable<ReqType> catalog)
        {
            var needs = new List<Need>();
            foreach (ReqType t in catalog.Where(x => x.Active && string.Equals(x.AppliesTo, "Petition", StringComparison.OrdinalIgnoreCase))
                                          .OrderBy(x => x.Sort))
            {
                if (t.RuleKey == "Always")
                    needs.Add(N(t, "Applies to every case type."));
                else if (string.Equals(t.RuleKey, petitionTypeCode, StringComparison.OrdinalIgnoreCase))
                    needs.Add(N(t, "Required for this case type."));
            }
            return needs;
        }

        private static Need N(ReqType t, string reason)
        {
            return new Need { Code = t.Code, Label = t.Label, Party = "Both", Reason = reason, Basis = t.Basis, Blocking = t.Blocking, Sort = t.Sort };
        }

        /// <summary>Every unmet BLOCKING item, in plain words - built only to tell the case editor
        /// what is still outstanding. Never gates Save/Advance; whether a case proceeds despite an
        /// outstanding document stays the registrar's own call, exactly as it does everywhere else
        /// this engine is used.</summary>
        public static List<string> OutstandingItems(List<Need> needs, List<ReqRow> rows)
        {
            var list = new List<string>();
            foreach (Need n in needs.Where(x => x.Blocking))
            {
                ReqRow r = rows.FirstOrDefault(x => x.Code == n.Code && x.Party == n.Party);
                if (MarriageRules.Satisfied(r)) continue;
                string state = r == null || r.Status == "Missing" ? "is still missing."
                    : r.Status == "Submitted" ? "was submitted but has not been verified."
                    : r.Status == "Rejected" ? "was rejected" + (string.IsNullOrWhiteSpace(r.Notes) ? "." : ": " + r.Notes)
                    : "is " + r.Status.ToLowerInvariant() + ".";
                list.Add(n.Label + " " + state);
            }
            return list;
        }
    }

    /// <summary>Every write for a petition/case's supporting documents. Requirements themselves
    /// go through <see cref="MarriageService"/> unchanged, under owner_type "Petition".</summary>
    public static class PetitionDocumentService
    {
        public const string Owner = "Petition";

        public static List<ReqType> Catalog()
        {
            return MarriageService.Catalog().Where(t => string.Equals(t.AppliesTo, Owner, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static List<ReqRow> Requirements(int petitionId)
        {
            return MarriageService.Requirements(Owner, petitionId);
        }

        /// <summary>Recomputes which documents apply from the case's CURRENT petition_type and
        /// adds/drops rows to match - so changing a case's type on an existing record (rare, but
        /// the editor allows it) doesn't leave stale requirement rows from the old type, and a
        /// case saved before this feature existed gets its rows created on first sync.</summary>
        public static void SyncRequirements(string petitionTypeCode, int petitionId)
        {
            List<Need> needs = PetitionRules.Needs(petitionTypeCode, Catalog());
            MarriageService.SyncRequirements(Owner, petitionId, needs);
        }
    }
}

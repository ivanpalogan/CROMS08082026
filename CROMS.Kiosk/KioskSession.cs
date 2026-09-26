using System.Collections.Generic;

namespace CROMS.Kiosk
{
    /// <summary>
    /// The one client's in-progress request, carried between the kiosk's step forms
    /// (Step 1 = <see cref="ServiceSelectForm"/>, Step 2 = <see cref="DetailsPhotoForm"/>).
    /// Reset between clients so the next person never sees the previous one's details/photo.
    /// </summary>
    public sealed class KioskSession
    {
        // Step 1 — services chosen, in the order tapped.
        public readonly List<string> Selected = new List<string>();

        // Step 2 — identity + contact.
        public string First, Middle, Last, Contact;

        // Step 2 — priority lane (strongest wins in KioskCore.PriorityValue).
        public bool Senior, Pwd, Pregnant;

        // Step 2 — captured photo (JPEG bytes) and the claim-ticket number typed for a pickup.
        public byte[] Photo;
        public string ClaimTicketEntry;

        // Marriage Application / Marriage Registration only: the SECOND person (spouse). First/
        // Middle/Last/Photo above are the husband's when HasMarriage is true; these are the
        // wife's. Both are captured on the same kiosk screen, one camera, toggled between them.
        public string First2, Middle2, Last2;
        public byte[] Photo2;

        // Step 2 — the kind of ID the client says they will present (optional). The ID PHOTO
        // itself is not browsed from a file on the kiosk PC — it comes through the same
        // claimapp QR every visit now shows (scan with your own phone, upload from there).
        public string IdType;

        // Marriage Registration only — answered by MarriageLicenseCheckForm before the ticket
        // is issued. Null while unanswered / after the client said No and was routed to
        // Marriage Application instead (see MarriageLicenseCheckForm.Continue_Click).
        public string MarriageLicenseNo;

        // Lazily-created claimapp QR — created for EVERY visit's "Upload Your ID" step, not
        // only a Release & Claim pickup (see KioskCore.EnsureClaimRequest).
        public string ClaimQrToken;
        public string ClaimQrNo;   // CLM-YYYY-####

        // PSA Copy (BREQS) only - what PSA is being asked for (BreqsDetailsForm). The generic
        // Owner/Spouse/Event fields follow the certificate type, exactly as breqs_requests does.
        public string BreqsDocType, BreqsPurpose, BreqsRelationship, IdNo;
        public int BreqsCopies = 1;
        public string OwnerFirst, OwnerMiddle, OwnerLast, SpouseFirst, SpouseMiddle, SpouseLast;
        public System.DateTime? EventDate;
        public string EventCity, EventProvince, FatherName, MotherMaidenName;

        // Certified True Copy only — captured once at the kiosk and carried into the
        // staff Certificate Request form with the queue task.
        //
        // These are deliberately SEPARATE from the Breqs*/Owner*/Event* fields above even
        // though they ask similar things: one visit can request a PSA copy AND a local
        // certified true copy of two different records, so they cannot share storage.
        // CtcDetails is no longer the whole request — it is the leftover note beside the
        // structured fields (see migration 55).
        public string CtcDocumentType, CtcDetails, CtcPurpose, CtcRelationship, CtcRegistryNo;
        public int CtcCopies = 1;
        public string CtcOwnerFirst, CtcOwnerMiddle, CtcOwnerLast;
        public string CtcSpouseFirst, CtcSpouseMiddle, CtcSpouseLast;
        public System.DateTime? CtcEventDate;
        public string CtcEventCity, CtcEventProvince, CtcFatherName, CtcMotherMaidenName;

        public bool HasClaim => Selected.Contains("CLAIM");
        public bool HasBreqs => Selected.Contains("BREQS");
        public bool HasMarriage => Selected.Contains("MARRIAGE_APP") || Selected.Contains("MARRIAGE_REG");
        public bool HasCtc => Selected.Contains("CTC");

        // Marriage Registration presumes a licence already exists — this is unanswered until
        // MarriageLicenseCheckForm resolves it. A "No" answer removes MARRIAGE_REG from
        // Selected (routed to MARRIAGE_APP instead), which is what makes this false again.
        public bool NeedsMarriageLicenseCheck => Selected.Contains("MARRIAGE_REG");

        public string[] StepLabels()
        {
            var steps = new List<string> { "Select Services" };
            if (NeedsMarriageLicenseCheck) steps.Add("Marriage License");
            if (HasBreqs) steps.Add("PSA Document");
            if (HasCtc) steps.Add("CTC Details");
            steps.Add("Personal Info & Photo");
            steps.Add("Review");
            return steps.ToArray();
        }

        public int DetailsStepIndex() =>
            1 + (NeedsMarriageLicenseCheck ? 1 : 0) + (HasBreqs ? 1 : 0) + (HasCtc ? 1 : 0);

        /// <summary>Fresh start for the next client.</summary>
        public void Reset()
        {
            Selected.Clear();
            First = Middle = Last = Contact = null;
            Senior = Pwd = Pregnant = false;
            Photo = null;
            ClaimTicketEntry = null;
            IdType = null;
            MarriageLicenseNo = null;
            First2 = Middle2 = Last2 = null;
            Photo2 = null;
            ClaimQrToken = null;
            ClaimQrNo = null;
            BreqsDocType = BreqsPurpose = BreqsRelationship = IdNo = null;
            BreqsCopies = 1;
            OwnerFirst = OwnerMiddle = OwnerLast = SpouseFirst = SpouseMiddle = SpouseLast = null;
            EventDate = null;
            EventCity = EventProvince = FatherName = MotherMaidenName = null;
            CtcDocumentType = CtcDetails = CtcPurpose = CtcRelationship = CtcRegistryNo = null;
            CtcCopies = 1;
            CtcOwnerFirst = CtcOwnerMiddle = CtcOwnerLast = null;
            CtcSpouseFirst = CtcSpouseMiddle = CtcSpouseLast = null;
            CtcEventDate = null;
            CtcEventCity = CtcEventProvince = CtcFatherName = CtcMotherMaidenName = null;
        }
    }
}

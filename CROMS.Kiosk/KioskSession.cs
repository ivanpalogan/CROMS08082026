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

        // Lazily-created claimapp QR — created for EVERY visit's "Upload Your ID" step, not
        // only a Release & Claim pickup (see KioskCore.EnsureClaimRequest).
        public string ClaimQrToken;
        public string ClaimQrNo;   // CLM-YYYY-####

        public bool HasClaim => Selected.Contains("CLAIM");
        public bool HasMarriage => Selected.Contains("MARRIAGE_APP") || Selected.Contains("MARRIAGE_REG");

        /// <summary>Fresh start for the next client.</summary>
        public void Reset()
        {
            Selected.Clear();
            First = Middle = Last = Contact = null;
            Senior = Pwd = Pregnant = false;
            Photo = null;
            ClaimTicketEntry = null;
            IdType = null;
            First2 = Middle2 = Last2 = null;
            Photo2 = null;
            ClaimQrToken = null;
            ClaimQrNo = null;
        }
    }
}

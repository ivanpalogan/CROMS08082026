using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// Entry point Birth/Marriage/Death Registration call to print the Acknowledgment of
    /// Submission slip for a transaction that has been submitted but is still pending
    /// verification. The actual layout lives in <see cref="AcknowledgmentSlipCert"/>, which
    /// is registered in <see cref="TemplateStore.KnownForms"/> — so the slip prints from the
    /// operator's saved, EDITABLE template (Administration &gt; Certificate Templates) the
    /// same way every other certificate in this app does, with a built-in direct-draw
    /// fallback if no template exists yet or one fails to render.
    /// </summary>
    public static class AcknowledgmentSlip
    {
        public static void Print(IWin32Window owner, string referenceNo, string registrantName,
            string registrationType, DateTime submittedDate, string statusText,
            IEnumerable<string> documentsReceived, string receivingStaff)
        {
            try
            {
                var t = AcknowledgmentSlipCert.BuildTable(referenceNo, registrantName,
                    registrationType, submittedDate, statusText, documentsReceived, receivingStaff);
                AcknowledgmentSlipCert.Show(t, owner);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message, "Print Acknowledgment Slip",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

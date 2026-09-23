using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// A one-page receipt for a registration that has been SUBMITTED but is still pending
    /// verification — Birth "Pending Approval"/"Delayed Posting", Marriage "For Review",
    /// Death "Pending Verification". It states, twice and in bold, that it is NOT a
    /// Certificate of Registration or a civil registry certificate: it only acknowledges
    /// that the transaction was received. Deliberately carries no fee/amount/O.R. field —
    /// receiving a submission and assessing a fee are two separate events in this app
    /// (Fees & Payments), and this slip must never be mistaken for either a certificate or
    /// a receipt of payment.
    /// <para/>
    /// Shared by Birth, Marriage and Death Registration so the three modules print the same
    /// document shape rather than three near-copies.
    /// </summary>
    public static class AcknowledgmentSlip
    {
        public static void Print(IWin32Window owner, string referenceNo, string registrantName,
            string registrationType, DateTime submittedDate, string statusText,
            IEnumerable<string> documentsReceived, string receivingStaff)
        {
            try
            {
                using (var doc = new PrintDocument())
                {
                    doc.DocumentName = "Acknowledgment Slip - " + referenceNo;
                    doc.PrintPage += (s, e) =>
                    {
                        Draw(e, referenceNo, registrantName, registrationType, submittedDate,
                            statusText, documentsReceived, receivingStaff);
                        e.HasMorePages = false;
                    };
                    using (var dlg = new PrintDialog { Document = doc, UseEXDialog = true })
                    {
                        if (dlg.ShowDialog(owner) == DialogResult.OK) doc.Print();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, ex.Message, "Print Acknowledgment Slip",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void Draw(PrintPageEventArgs e, string referenceNo, string registrantName,
            string registrationType, DateTime submittedDate, string statusText,
            IEnumerable<string> documentsReceived, string receivingStaff)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float left = e.MarginBounds.Left, width = e.MarginBounds.Width, right = e.MarginBounds.Right;
            float y = e.MarginBounds.Top;

            OfficeProfile p = OfficeAssets.Profile;

            using (var fHead = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fSub = new Font("Segoe UI", 9F))
            using (var fTitle = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (var fLabel = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fVal = new Font("Segoe UI", 10F))
            using (var fSmall = new Font("Segoe UI", 8F))
            using (var fNote = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var fDoc = new Font("Segoe UI", 9.5F))
            using (var center = new StringFormat { Alignment = StringAlignment.Center })
            using (var noteBrush = new SolidBrush(Color.FromArgb(150, 20, 20)))
            using (var notePen = new Pen(Color.FromArgb(150, 20, 20), 1.4f))
            {
                void Mid(string t, Font f, float dy)
                {
                    g.DrawString(t, f, Brushes.Black, new RectangleF(left, y, width, f.GetHeight() + 4), center);
                    y += f.GetHeight() + dy;
                }
                void Rule() { using (var pen = new Pen(Color.Black)) g.DrawLine(pen, left, y + 2, right, y + 2); y += 12; }
                void Field(string label, string val)
                {
                    g.DrawString(label, fLabel, Brushes.Black, left, y);
                    g.DrawString(string.IsNullOrEmpty(val) ? "—" : val, fVal, Brushes.Black, left + 190, y);
                    y += 24;
                }
                void Note(string text)
                {
                    SizeF sz = g.MeasureString(text, fNote, (int)width);
                    var r = new RectangleF(left, y, width, sz.Height + 12);
                    g.DrawRectangle(notePen, r.X, r.Y, r.Width, r.Height);
                    g.DrawString(text, fNote, noteBrush, new RectangleF(r.X + 6, r.Y + 6, r.Width - 12, sz.Height), center);
                    y += r.Height + 14;
                }

                Mid("Republic of the Philippines", fSub, 2);
                Mid(p.MunicipalityForPrint + ", " + p.ProvinceForPrint, fHead, 2);
                Mid(p.OfficeName, fSub, 10);
                Mid("ACKNOWLEDGMENT OF SUBMISSION", fTitle, 2);
                Mid("(Transaction Receipt — " + registrationType + ")", fSmall, 10);
                Rule();

                Note("This document acknowledges receipt of transaction only. It is NOT a " +
                     "Certificate of Registration or a Civil Registry certificate.");

                Field("Transaction / Reference No.", referenceNo);
                Field("Registrant Name", registrantName);
                Field("Registration Type", registrationType);
                Field("Date Submitted", submittedDate.ToString("dd MMMM yyyy, h:mm tt"));
                Field("Status", statusText);
                y += 4; Rule();

                g.DrawString("Documents Received:", fLabel, Brushes.Black, left, y);
                y += 22;
                bool any = false;
                if (documentsReceived != null)
                {
                    foreach (string d in documentsReceived)
                    {
                        if (string.IsNullOrWhiteSpace(d)) continue;
                        any = true;
                        g.DrawString("•  " + d, fDoc, Brushes.Black, left + 12, y);
                        y += 20;
                    }
                }
                if (!any)
                {
                    g.DrawString("—  none listed", fDoc, Brushes.Gray, left + 12, y);
                    y += 20;
                }
                y += 8; Rule();

                Field("Received By", receivingStaff);
                y += 30;
                g.DrawString("_______________________________", fVal, Brushes.Black, left, y); y += 20;
                g.DrawString("Signature over printed name", fSmall, Brushes.Gray, left, y);
                y += 34;

                Note("This document acknowledges receipt of transaction only. It is NOT a " +
                     "Certificate of Registration or a Civil Registry certificate. Present " +
                     "this slip when following up on this submission.");

                float fy = e.MarginBounds.Bottom - 16;
                g.DrawString("Printed " + DateTime.Now.ToString("ddd, dd MMM yyyy  h:mm tt") + " · CROMS",
                    fSmall, Brushes.Gray, left, fy);
            }
        }
    }
}

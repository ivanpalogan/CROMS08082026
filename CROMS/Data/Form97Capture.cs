using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Mobile Capture for Marriage Registration (Form 97). Desktop side of a short-lived,
    /// QR-linked token: the phone gets a lightweight page (hosted by the save-API on port
    /// 3000, next to the phone's own /api/scans upload) that shows the transaction/couple
    /// and lets the client photograph the paper certificate, one page at a time. Nothing
    /// here reads or writes the phone's page - this only creates/polls/consumes the token
    /// row the save-API writes into.
    /// </summary>
    public static class Form97Capture
    {
        /// <summary>The routine pre-registration capture (Steps 7-8) - photograph the paper
        /// certificate before OCR runs. Written into marriages.scan_image.</summary>
        public const string PurposeIncoming = "INCOMING_FORM_97";

        /// <summary>STEP 10 - the FINAL, physically signed/stamped copy, captured only after
        /// registration information is saved. Written into marriages.final_scan_image, never
        /// scan_image - the two are different images with different meanings.</summary>
        public const string PurposeFinalRegistered = "FINAL_REGISTERED_FORM_97";

        /// <summary>
        /// A capture started from the Intelligent Document Processing window itself, outside
        /// any registration or queue task - no couple or transaction to show on the phone.
        /// The photographed certificate goes straight into that window for OCR.
        /// </summary>
        public const string PurposeOcrCapture = "OCR_CAPTURE";

        // Which DOCUMENT a page is (migration 66). The certificate is what OCR reads; the
        // licence is kept as a picture only. A page with no role (uploaded before 66, or a
        // final-copy capture) is read as the certificate.
        public const string RoleCertificate = "CERTIFICATE";
        public const string RoleLicense = "LICENSE";

        public sealed class Status
        {
            public string State = "Pending";     // Pending / Uploaded / Completed / Expired
            public int PageCount;
            public int CertificatePages;
            public int LicensePages;
            public bool Expired;
            public DateTime? LastUploadAt;
        }

        /// <summary>Every page of one capture session, split by the document it shows.</summary>
        public sealed class CapturedDocs
        {
            public List<byte[]> Certificate = new List<byte[]>();
            public List<byte[]> License = new List<byte[]>();

            /// <summary>The page OCR should read: the newest certificate page (a retake supersedes the first).</summary>
            public byte[] PrimaryCertificate => Certificate.Count == 0 ? null : Certificate[Certificate.Count - 1];
            public byte[] PrimaryLicense => License.Count == 0 ? null : License[License.Count - 1];
        }

        /// <summary>
        /// Opens a fresh token for the couple/transaction currently on screen. husband/wife/
        /// txnCode are a SNAPSHOT (the record may not be saved yet), so the mobile page has
        /// something to show even before marriages.id exists. `purpose` decides what the phone
        /// page shows and how the uploaded image is labelled (migration 63) - the token/QR
        /// mechanism is shared, the purpose it serves is not.
        /// </summary>
        public static string CreateToken(int? marriageId, int? txnId, string husband, string wife,
            string txnCode, int minutesValid = 20, string purpose = PurposeIncoming)
        {
            string token = Guid.NewGuid().ToString("N"); // 32 hex chars, matches token CHAR(32)
            var ps = new[]
            {
                new MySqlParameter("@t", token),
                new MySqlParameter("@mid", (object)marriageId ?? DBNull.Value),
                new MySqlParameter("@tid", (object)txnId ?? DBNull.Value),
                new MySqlParameter("@h", (object)husband ?? DBNull.Value),
                new MySqlParameter("@w", (object)wife ?? DBNull.Value),
                new MySqlParameter("@c", (object)txnCode ?? DBNull.Value),
                new MySqlParameter("@p", purpose),
                new MySqlParameter("@m", minutesValid),
                new MySqlParameter("@by", Session.UserIdParam)
            };
            try
            {
                Db.Insert(
                    "INSERT INTO form97_capture_tokens " +
                    "(token, marriage_id, transaction_id, husband_name, wife_name, txn_code, purpose, status, expires_at, created_by) " +
                    "VALUES (@t, @mid, @tid, @h, @w, @c, @p, 'Pending', DATE_ADD(NOW(), INTERVAL @m MINUTE), @by)", ps);
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                // migration 63 (purpose column) not applied yet - fall back to the original
                // columns so the ordinary pre-registration capture keeps working; a caller that
                // asked for PurposeFinalRegistered on an unmigrated database gets the token but
                // the phone page/label will read as the default until 63 is applied.
                Db.Insert(
                    "INSERT INTO form97_capture_tokens " +
                    "(token, marriage_id, transaction_id, husband_name, wife_name, txn_code, status, expires_at, created_by) " +
                    "VALUES (@t, @mid, @tid, @h, @w, @c, 'Pending', DATE_ADD(NOW(), INTERVAL @m MINUTE), @by)", ps);
            }
            // STEP 13 audit trail: "Mobile Capture session generated" / "Final Capture session
            // generated" - only when the record already exists (a brand new, unsaved draft has
            // no id yet); best-effort so a logging hiccup can never block the capture itself.
            if (marriageId.HasValue)
            {
                try
                {
                    MarriageService.History("Marriage", marriageId.Value,
                        purpose == PurposeFinalRegistered ? "Final Capture session generated" : "Mobile Capture session generated",
                        null, null, "token " + token.Substring(0, 8));
                }
                catch { }
            }
            return token;
        }

        /// <summary>Live poll target for the desktop dialog - never throws (a DB hiccup just reads as "no change yet").</summary>
        public static Status GetStatus(string token)
        {
            var s = new Status();
            try
            {
                DataTable dt = Db.Pull(
                    "SELECT status, (expires_at < NOW()) AS is_expired, " +
                    "(SELECT COUNT(*) FROM form97_capture_images i WHERE i.token_id = t.id) AS pages, " +
                    "(SELECT MAX(i.uploaded_at) FROM form97_capture_images i WHERE i.token_id = t.id) AS last_upload " +
                    "FROM form97_capture_tokens t WHERE token=@t",
                    new MySqlParameter("@t", token));
                if (dt.Rows.Count == 0) { s.Expired = true; return s; }
                DataRow r = dt.Rows[0];
                s.State = Convert.ToString(r["status"]);
                s.Expired = Convert.ToBoolean(r["is_expired"]) || s.State == "Expired";
                s.PageCount = Convert.ToInt32(r["pages"]);
                s.LastUploadAt = r["last_upload"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(r["last_upload"]);
                s.CertificatePages = s.PageCount;
                try
                {
                    DataTable roles = Db.Pull(
                        "SELECT SUM(COALESCE(i.doc_role,'CERTIFICATE') = 'LICENSE') AS lic " +
                        "FROM form97_capture_images i JOIN form97_capture_tokens t ON t.id = i.token_id WHERE t.token=@t",
                        new MySqlParameter("@t", token));
                    int lic = roles.Rows.Count == 0 || roles.Rows[0]["lic"] == DBNull.Value ? 0 : Convert.ToInt32(roles.Rows[0]["lic"]);
                    s.LicensePages = lic;
                    s.CertificatePages = s.PageCount - lic;
                }
                catch (MySqlException ex) when (ex.Number == 1054) { /* migration 66 not applied: every page is a certificate */ }
            }
            catch { /* transient DB blip - dialog just polls again */ }
            return s;
        }

        /// <summary>
        /// Every page of the session, sorted into certificate and licence pages (migration 66).
        /// On a database without doc_role every page is treated as a certificate - the same
        /// behaviour as before the licence step existed.
        /// </summary>
        public static CapturedDocs FetchDocs(string token)
        {
            var docs = new CapturedDocs();
            DataTable dt;
            try
            {
                dt = Db.Pull(
                    "SELECT i.image, i.doc_role FROM form97_capture_images i " +
                    "JOIN form97_capture_tokens t ON t.id = i.token_id " +
                    "WHERE t.token=@t ORDER BY i.page_no",
                    new MySqlParameter("@t", token));
            }
            catch (MySqlException ex) when (ex.Number == 1054)
            {
                foreach (byte[] b in FetchImages(token)) docs.Certificate.Add(b);
                return docs;
            }
            foreach (DataRow r in dt.Rows)
            {
                if (r["image"] == DBNull.Value) continue;
                string role = r["doc_role"] as string;
                if (string.Equals(role, RoleLicense, StringComparison.OrdinalIgnoreCase)) docs.License.Add((byte[])r["image"]);
                else docs.Certificate.Add((byte[])r["image"]);
            }
            return docs;
        }

        /// <summary>Every page captured on the phone, oldest first (page 1 = the primary/front image).</summary>
        public static List<byte[]> FetchImages(string token)
        {
            var list = new List<byte[]>();
            DataTable dt = Db.Pull(
                "SELECT i.image FROM form97_capture_images i " +
                "JOIN form97_capture_tokens t ON t.id = i.token_id " +
                "WHERE t.token=@t ORDER BY i.page_no",
                new MySqlParameter("@t", token));
            foreach (DataRow r in dt.Rows)
                if (r["image"] != DBNull.Value) list.Add((byte[])r["image"]);
            return list;
        }

        /// <summary>Links a since-saved marriage record back onto the token, so the audit trail names the record it produced.</summary>
        public static void AttachMarriageId(string token, int marriageId)
        {
            try
            {
                Db.Push("UPDATE form97_capture_tokens SET marriage_id=@m WHERE token=@t",
                    new MySqlParameter("@m", marriageId), new MySqlParameter("@t", token));
            }
            catch { /* best-effort - the scan is already attached to the form either way */ }
        }

        /// <summary>
        /// Keeps a still-in-use token alive (Add Page reopens the SAME token rather than
        /// starting a fresh capture set, so pages accumulate under one "Pages: N" count).
        /// </summary>
        public static void Touch(string token, int minutesValid = 20)
        {
            try
            {
                Db.Push("UPDATE form97_capture_tokens SET expires_at=DATE_ADD(NOW(), INTERVAL @m MINUTE) " +
                        "WHERE token=@t AND status <> 'Completed'",
                        new MySqlParameter("@m", minutesValid), new MySqlParameter("@t", token));
            }
            catch { }
        }

        /// <summary>Desktop-side close-out: a token the operator is done with can't be scanned again.</summary>
        public static void Complete(string token)
        {
            try
            {
                Db.Push("UPDATE form97_capture_tokens SET status='Completed', completed_at=NOW() " +
                        "WHERE token=@t AND status <> 'Completed'", new MySqlParameter("@t", token));
            }
            catch { }
        }

        /// <summary>
        /// The mobile page's URL - hosted by the save-API's HTTPS listener (port 3443,
        /// same process/app as the plain-HTTP :3000 API). HTTPS is required here because
        /// this page's Live Camera (getUserMedia) is refused by browsers outside a secure
        /// context - :3000 stays plain HTTP for the ng-serve dev-proxy and the desktop
        /// dashboard, which don't need the camera.
        /// </summary>
        public static string BuildMobileUrl(string token)
        {
            // Stable trusted hostname when the certificate is live (no phone warning, camera works).
            if (TrustedHost.CertReady)
                return "https://" + TrustedHost.Host + ":3443/form97-capture.html?token=" + token;
            string ip = IonicServerManager.DetectLanIp();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
            return "http://" + ip + ":3000/form97-capture.html?token=" + token;
        }
    }
}

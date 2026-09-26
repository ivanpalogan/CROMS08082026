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

        public sealed class Status
        {
            public string State = "Pending";     // Pending / Uploaded / Completed / Expired
            public int PageCount;
            public bool Expired;
            public DateTime? LastUploadAt;
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
            }
            catch { /* transient DB blip - dialog just polls again */ }
            return s;
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
        /// The mobile page's URL - hosted by the save-API (port 3000), the same server the
        /// phone already reaches for /api/scans and the claimapp ID upload.
        /// </summary>
        public static string BuildMobileUrl(string token)
        {
            string ip = IonicServerManager.DetectLanIp();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
            return "http://" + ip + ":3000/form97-capture.html?token=" + token;
        }
    }
}

using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// The per-field audit trail for Intelligent Document Processing. A civil-registry
    /// record that was produced by a machine has to be answerable later: what did OCR
    /// actually read, what was it corrected to, how sure was the engine, who accepted it
    /// and when. One row per field per disposition, written at the moment the operator
    /// commits, drafts, auto-fills or sends the scan to manual review.
    /// <para/>
    /// The page's whole original OCR text is kept once in `ocr_batch.raw_text`; this table
    /// is the field-by-field companion to it.
    /// </summary>
    public static class OcrAudit
    {
        public const string Committed = "Committed";
        public const string Draft = "Draft";
        public const string AutoFilled = "Auto-Filled";
        public const string ManualReview = "Manual Review";

        /// <summary>
        /// Write every field of a result. Never throws into the caller's workflow: a
        /// failed audit write is reported through <paramref name="error"/> so the screen
        /// can tell the operator, but it does not roll back the record they just saved.
        /// </summary>
        public static bool WriteFields(string scanId, DocAiResult result, string action, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(scanId) || result == null) return false;

            string username = Session.User?.Username ?? "(unknown)";
            try
            {
                foreach (DocField f in result.Fields)
                {
                    Db.Push(
                        "INSERT INTO ocr_field_audit " +
                        "(scan_id, field_key, field_label, ocr_value, final_value, confidence, " +
                        " status, issue, auto_corrected, edited_by_user, action, username) " +
                        "VALUES (@scan, @key, @label, @ocr, @final, @conf, @status, @issue, " +
                        " @corrected, @edited, @action, @user)",
                        new MySqlParameter("@scan", scanId),
                        new MySqlParameter("@key", f.Key),
                        new MySqlParameter("@label", f.Label),
                        new MySqlParameter("@ocr", (object)f.OcrValue ?? DBNull.Value),
                        new MySqlParameter("@final", (object)f.Value ?? DBNull.Value),
                        new MySqlParameter("@conf", f.Confidence),
                        new MySqlParameter("@status", f.Status.ToString()),
                        new MySqlParameter("@issue", string.IsNullOrEmpty(f.Issue) ? (object)DBNull.Value : f.Issue),
                        new MySqlParameter("@corrected", f.Corrected ? 1 : 0),
                        new MySqlParameter("@edited", f.EditedByUser ? 1 : 0),
                        new MySqlParameter("@action", action),
                        new MySqlParameter("@user", username));
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Close the loop between an OCR upload and the record it produced. Called by the
        /// birth/marriage/death module itself, right after it saves the record — never by
        /// this screen, which has usually already closed by the time that save happens
        /// (Auto-Fill hands the values to the target module and its own Save is a separate,
        /// later click). Matches on <paramref name="scanId"/>, the upload's own stable id,
        /// so the row a client's phone created is the SAME row this update finds — nothing
        /// here substitutes the upload id for a registry number; a blank
        /// <paramref name="registryNo"/> is written as NULL, not as the scan id.
        /// <para/>
        /// Best-effort, like every other audit write in this project: a failure here must
        /// never roll back or block the real record that was just saved.
        /// </summary>
        public static void MarkProcessed(string scanId, string table, long recordId, string registryNo)
        {
            if (string.IsNullOrEmpty(scanId)) return;
            try
            {
                Db.Push(
                    "UPDATE ocr_batch SET status = 'Processed', record_table = @table, " +
                    "record_id = @rid, final_registry_no = @reg WHERE scan_id = @scan",
                    new MySqlParameter("@table", table),
                    new MySqlParameter("@rid", recordId),
                    new MySqlParameter("@reg", string.IsNullOrWhiteSpace(registryNo)
                        ? (object)DBNull.Value : registryNo.Trim()),
                    new MySqlParameter("@scan", scanId));
            }
            catch { /* the record itself is already saved; a missed link is not fatal */ }
        }

        /// <summary>The audit trail for one scan, newest disposition first, for display.</summary>
        public static System.Data.DataTable ForScan(string scanId)
        {
            return Db.Pull(
                "SELECT field_label AS 'Field', ocr_value AS 'OCR Read', final_value AS 'Saved As', " +
                "CONCAT(confidence, '%') AS 'Conf', status AS 'Status', " +
                "CASE WHEN edited_by_user = 1 THEN 'operator' " +
                "     WHEN auto_corrected = 1 THEN 'auto-corrected' ELSE '' END AS 'Changed By', " +
                "action AS 'Action', username AS 'User', created_at AS 'When' " +
                "FROM ocr_field_audit WHERE scan_id = @s ORDER BY id",
                new MySqlParameter("@s", scanId));
        }
    }
}

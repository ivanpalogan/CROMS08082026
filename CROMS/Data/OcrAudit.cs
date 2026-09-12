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

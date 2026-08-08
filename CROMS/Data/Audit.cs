using System;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// Writes tamper-evidence rows to `audit_log`. Every create / update / delete and
    /// every login / logout should call <see cref="Write"/> so the office has a trail of
    /// who did what. The user id comes from <see cref="Session"/>. Auditing must never
    /// block the real operation, so failures here are swallowed.
    /// </summary>
    public static class Audit
    {
        // Matches the audit_log.action ENUM('Create','Update','Delete','Login','Logout').
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Login = "Login";
        public const string Logout = "Logout";

        public static void Write(string action, string tableName, object recordId, string details = null)
        {
            try
            {
                Db.Push(
                    "INSERT INTO audit_log (user_id, action, table_name, record_id, details) " +
                    "VALUES (@u, @a, @t, @r, @d)",
                    new MySqlParameter("@u", Session.UserIdParam),
                    new MySqlParameter("@a", action),
                    new MySqlParameter("@t", (object)tableName ?? DBNull.Value),
                    new MySqlParameter("@r", recordId == null ? (object)DBNull.Value : recordId.ToString()),
                    new MySqlParameter("@d", (object)details ?? DBNull.Value));
            }
            catch { /* never let auditing break the operation it records */ }
        }
    }
}

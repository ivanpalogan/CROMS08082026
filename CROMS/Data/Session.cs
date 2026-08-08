using System;

namespace CROMS.Data
{
    /// <summary>The user currently signed in to CROMS (set at login).</summary>
    public class CurrentUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    /// <summary>
    /// Holds the signed-in user for the life of the app session. Set once by
    /// <c>LoginForm</c>; read everywhere that needs to stamp who did an action
    /// (transactions.created_by, payments.cashier_id, releases.released_by, audit_log).
    /// </summary>
    public static class Session
    {
        public static CurrentUser User { get; set; }

        public static bool IsLoggedIn => User != null;
        public static bool IsAdmin => User != null && User.Role == "Admin";

        /// <summary>The current user's id as a SQL parameter value (DBNull if none).</summary>
        public static object UserIdParam => User != null ? (object)User.Id : DBNull.Value;

        /// <summary>
        /// The service window this operator claimed at login (0 = none / skipped).
        /// Set by <c>WindowAssignmentForm</c>; used by Queue Management to route
        /// tickets and to keep the window's Online presence / heartbeat.
        /// </summary>
        public static int WindowId { get; set; }
        public static string WindowName { get; set; }

        public static bool HasWindow => WindowId > 0;
    }
}

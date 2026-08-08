using System;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;

namespace CROMS.Data
{
    /// <summary>
    /// One-time, short-lived "resume session" token used ONLY by the in-app updater.
    /// When the ⟳ Update button restarts CROMS to apply a new build, the operator who
    /// just authorized that update is kept signed in (and their service window
    /// reclaimed) so they don't have to re-type the password for a restart they
    /// triggered themselves.
    /// <para/>
    /// Safeguards so a normal launch / logout / crash never auto-signs-in:
    ///   • the token is written only right before the updater restart (nowhere else);
    ///   • it is consumed (deleted) on the first read, valid attempt or not;
    ///   • it expires after a few minutes;
    ///   • it is bound to this Windows account + machine name;
    ///   • the user is re-validated as still existing + active in the database.
    /// It is a same-PC continuation aid, not a general credential store.
    /// </summary>
    public static class SessionResume
    {
        // Generous enough to cover the file-copy + relaunch on a slow LAN, still short.
        private const int TtlSeconds = 300;

        private static string TokenPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CROMS", "resume.session");

        /// <summary>Writes the token — called by the updater just before it restarts CROMS.</summary>
        public static void Write()
        {
            if (Session.User == null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(TokenPath));
                File.WriteAllLines(TokenPath, new[]
                {
                    Session.User.Id.ToString(),
                    Session.WindowId.ToString(),
                    Session.WindowName ?? "",
                    DateTime.UtcNow.AddSeconds(TtlSeconds).Ticks.ToString(),
                    Environment.UserName,
                    Environment.MachineName
                });
            }
            catch { /* non-fatal — worst case the operator just signs in again */ }
        }

        /// <summary>
        /// If a valid, unexpired, same-account token exists, restores <see cref="Session"/>
        /// (and reclaims the window) and returns true so the caller can skip the login +
        /// window-assignment screens. Always deletes the token (one-time). Returns false
        /// when there is none / it is invalid / expired / the user is inactive.
        /// </summary>
        public static bool TryConsume()
        {
            string path = TokenPath;
            string[] lines;
            try
            {
                if (!File.Exists(path)) return false;
                lines = File.ReadAllLines(path);
            }
            catch { return false; }
            finally { try { if (File.Exists(path)) File.Delete(path); } catch { } }

            try
            {
                if (lines.Length < 6) return false;
                int userId = int.Parse(lines[0]);
                int windowId = int.Parse(lines[1]);
                string windowName = lines[2];
                long expiryTicks = long.Parse(lines[3]);
                string winUser = lines[4];
                string machine = lines[5];

                if (DateTime.UtcNow.Ticks > expiryTicks) return false;   // expired
                if (!string.Equals(winUser, Environment.UserName, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase)) return false;

                // Re-validate the user still exists and is active (defence against a
                // stale/forged token naming a now-disabled or deleted account).
                DataTable u = Db.Pull(
                    "SELECT username, full_name, role FROM users WHERE id = @id AND is_active = 1",
                    new MySqlParameter("@id", userId));
                if (u.Rows.Count == 0) return false;

                Session.User = new CurrentUser
                {
                    Id = userId,
                    Username = u.Rows[0]["username"].ToString(),
                    FullName = u.Rows[0]["full_name"].ToString(),
                    Role = u.Rows[0]["role"].ToString()
                };

                // Reclaim the same service window if it is still free (or was ours). The
                // window's transaction assignments in window_transactions survive a
                // release, so only the Online presence needs re-stamping.
                if (windowId > 0)
                {
                    try
                    {
                        DataTable busy = Db.Pull(
                            "SELECT current_operator FROM windows WHERE id = @id AND status = 'Active' " +
                            "AND current_operator IS NOT NULL AND current_operator <> @me " +
                            "AND last_heartbeat > (NOW() - INTERVAL " +
                            CROMS.Forms.WindowAssignmentForm.StaleMinutes + " MINUTE)",
                            new MySqlParameter("@id", windowId), new MySqlParameter("@me", userId));
                        if (busy.Rows.Count == 0)
                        {
                            Db.Push(
                                "UPDATE windows SET current_operator = @op, operator_name = @nm, " +
                                "last_heartbeat = NOW() WHERE id = @id AND status = 'Active'",
                                new MySqlParameter("@op", userId),
                                new MySqlParameter("@nm", Session.User.FullName),
                                new MySqlParameter("@id", windowId));
                            Session.WindowId = windowId;
                            Session.WindowName = string.IsNullOrEmpty(windowName)
                                ? "Window " + windowId : windowName;
                        }
                    }
                    catch { /* window reclaim is best-effort — proceed without it */ }
                }

                Audit.Write(Audit.Login, "users", userId, "Resumed session after in-app update");
                return true;
            }
            catch { return false; }
        }
    }
}

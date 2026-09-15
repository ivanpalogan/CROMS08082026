using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// One-click LAN app-update for ALL THREE CROMS apps (Main, Kiosk, Display).
    /// The server PC shares a release folder ("CROMSRelease") with sub-folders
    /// Main / Kiosk / Display, each holding that app's latest build. A client
    /// checks the share for newer exes and, if found, pulls the new files for
    /// every app it has installed and restarts. Config files are never overwritten
    /// (each PC keeps its own connection settings). Fully offline (LAN only).
    ///
    /// The share is reached at <c>\\&lt;server-ip&gt;\CROMSRelease</c> using the
    /// same server IP CROMS already uses for the database, so nothing extra to set.
    ///
    /// Client install layout (so the Kiosk/Display folders can be found): the three
    /// apps sit in sibling folders under one parent, e.g.
    ///   ...\CROMS\Main\CROMS.exe, ...\CROMS\Kiosk\CROMS.Kiosk.exe, ...\CROMS\Display\CROMS.Display.exe
    /// (The combined installer zip lays them out this way.)
    /// </summary>
    public static class AppUpdater
    {
        public const string ShareName = "CROMSRelease";

        private class AppTarget
        {
            public string Exe;       // e.g. CROMS.Kiosk.exe
            public string Sub;       // share sub-folder: Main / Kiosk / Display
            public string LocalDir;  // where it's installed on this PC
            public bool WasRunning;  // relaunch after update if it was open
            public bool IsNewInstall; // app absent on this PC — copy it in for the first time
        }

        /// <summary>UNC path to the release share, or null on the server PC itself.</summary>
        public static string ShareRoot
        {
            get
            {
                string host = ServerConfig.EffectiveHost;
                if (string.IsNullOrWhiteSpace(host) ||
                    host == "localhost" || host == "127.0.0.1" || host == "::1")
                    return null;
                return @"\\" + host + @"\" + ShareName;
            }
        }

        /// <summary>
        /// Establishes an authenticated SMB session to the release share, so reading it
        /// works WITHOUT the operator running <c>net use</c> in a command prompt first.
        /// Does the equivalent of:
        ///   net use \\&lt;server&gt;\CROMSRelease /user:&lt;server&gt;\cromsshare &lt;password&gt; /persistent:yes
        /// Credentials are configurable in App.config (ReleaseShareUser / ReleaseSharePassword
        /// / ReleaseShareDomain); the domain defaults to the server IP so a local account on
        /// the server authenticates without needing the server's machine name. If the share is
        /// already reachable (session already open, or same-user) it does nothing. Never throws.
        /// </summary>
        public static bool EnsureShareConnection(out string message)
        {
            message = "";
            string share = ShareRoot;
            if (share == null) return true;                 // server PC — no share to map
            try { if (Directory.Exists(share)) return true; } catch { /* fall through and map */ }

            string host = ServerConfig.EffectiveHost;
            string user = (ConfigurationManager.AppSettings["ReleaseShareUser"] ?? "cromsshare").Trim();
            string pass = ConfigurationManager.AppSettings["ReleaseSharePassword"] ?? "hAjuTRW0WIUNwc0P43BE";
            string domainCfg = ConfigurationManager.AppSettings["ReleaseShareDomain"];
            string domain = string.IsNullOrWhiteSpace(domainCfg) ? (host ?? "") : domainCfg.Trim();  // blank → server IP
            if (user.Length > 0 && !user.Contains("\\") && domain.Length > 0)
                user = domain + "\\" + user;                // -> 192.168.1.234\cromsshare

            // Drop any stale/mismatched session to this exact share first (error 1219 guard),
            // then connect persistently with the stored credentials.
            RunNet("use \"" + share + "\" /delete /y", out _);
            bool ok = RunNet("use \"" + share + "\" /user:" + user + " " + pass + " /persistent:yes", out string err);

            try { if (Directory.Exists(share)) return true; } catch { }
            if (!ok) message = string.IsNullOrWhiteSpace(err) ? "Could not connect to " + share + "." : err.Trim();
            return ok;
        }

        /// <summary>Runs net.exe with the given arguments, hidden; true on exit code 0.</summary>
        private static bool RunNet(string args, out string err)
        {
            err = "";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "net.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (var p = Process.Start(psi))
                {
                    string so = p.StandardOutput.ReadToEnd();
                    err = p.StandardError.ReadToEnd();
                    if (!p.WaitForExit(15000)) { try { p.Kill(); } catch { } err = "net use timed out."; return false; }
                    if (p.ExitCode == 0) { err = ""; return true; }
                    if (string.IsNullOrWhiteSpace(err)) err = so;
                    return false;
                }
            }
            catch (Exception ex) { err = ex.Message; return false; }
        }

        // The apps installed on this PC (Main is always this exe; Kiosk/Display if found nearby).
        private static List<AppTarget> Targets()
        {
            var list = new List<AppTarget>();
            string mainDir = Path.GetDirectoryName(Application.ExecutablePath);
            list.Add(new AppTarget { Exe = "CROMS.exe", Sub = "Main", LocalDir = mainDir });

            string baseDir = Directory.GetParent(mainDir)?.FullName;

            // Kiosk/Display: update them where they already are; if this PC never got them,
            // INSTALL them next to CROMS.exe (the launcher looks in its own folder first).
            // Without this a client that only received CROMS.exe could never obtain the other
            // two apps, and the launcher kept reporting "Could not find CROMS.Display.exe".
            string kiosk = baseDir != null ? FindExeDir(baseDir, "CROMS.Kiosk.exe") : null;
            list.Add(new AppTarget
            {
                Exe = "CROMS.Kiosk.exe", Sub = "Kiosk",
                LocalDir = kiosk ?? mainDir, IsNewInstall = kiosk == null
            });
            string disp = baseDir != null ? FindExeDir(baseDir, "CROMS.Display.exe") : null;
            list.Add(new AppTarget
            {
                Exe = "CROMS.Display.exe", Sub = "Display",
                LocalDir = disp ?? mainDir, IsNewInstall = disp == null
            });

            // Mark which are currently running (so we relaunch them afterwards).
            foreach (var t in list)
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(t.Exe);
                    t.WasRunning = Process.GetProcessesByName(name).Length > 0;
                }
                catch { }
            }
            return list;
        }

        // Look for an exe in baseDir itself or any immediate sub-folder.
        private static string FindExeDir(string baseDir, string exe)
        {
            try
            {
                if (File.Exists(Path.Combine(baseDir, exe))) return baseDir;
                foreach (var d in Directory.GetDirectories(baseDir))
                    if (File.Exists(Path.Combine(d, exe))) return d;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Is a newer build of ANY installed app on the server share? Compares each
        /// app's share exe timestamp to the local one. Never throws.
        /// </summary>
        public static bool IsUpdateAvailable(out string detail)
        {
            detail = "";
            try
            {
                string share = ShareRoot;
                if (share == null) { detail = "This is the server PC — no update needed here."; return false; }

                // Auto-authenticate to the share (no manual `net use` needed).
                EnsureShareConnection(out string connErr);
                if (!Directory.Exists(share))
                {
                    detail = "Update share not found at " + share + "." +
                             (string.IsNullOrEmpty(connErr) ? "" : " (" + connErr + ")");
                    return false;
                }

                var newer = new List<string>();
                foreach (var t in Targets())
                {
                    string remoteExe = Path.Combine(share, t.Sub, t.Exe);
                    string localExe = Path.Combine(t.LocalDir, t.Exe);
                    if (!File.Exists(remoteExe)) continue;               // not published — nothing to pull
                    if (!File.Exists(localExe)) { newer.Add(t.Sub + " (new)"); continue; }
                    if (File.GetLastWriteTimeUtc(remoteExe) > File.GetLastWriteTimeUtc(localExe).AddSeconds(2))
                        newer.Add(t.Sub);
                }

                if (newer.Count > 0)
                {
                    detail = "Update available for: " + string.Join(", ", newer.ToArray()) + ".";
                    return true;
                }
                detail = "All apps are up to date.";
                return false;
            }
            catch (Exception ex)
            {
                detail = "Couldn't check for updates: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Stage the new builds for every installed app, then launch a helper that
        /// closes the apps, copies the files (excluding *.config), and relaunches
        /// them. Returns false with a message on failure; on success the caller
        /// should exit so the helper can do the swap.
        /// </summary>
        public static bool ApplyUpdate(out string error)
        {
            error = "";
            try
            {
                string share = ShareRoot;
                if (share == null) { error = "No server share to update from."; return false; }

                // Auto-authenticate to the share (no manual `net use` needed).
                EnsureShareConnection(out string connErr);
                if (!Directory.Exists(share))
                {
                    error = "Update share not found (" + share + ")." +
                            (string.IsNullOrEmpty(connErr) ? "" : " " + connErr);
                    return false;
                }

                var targets = Targets();
                string stageRoot = Path.Combine(Path.GetTempPath(), "croms_update");
                if (Directory.Exists(stageRoot)) Directory.Delete(stageRoot, true);
                Directory.CreateDirectory(stageRoot);

                var staged = new List<AppTarget>();
                foreach (var t in targets)
                {
                    string remoteSub = Path.Combine(share, t.Sub);
                    if (!File.Exists(Path.Combine(remoteSub, t.Exe))) continue;   // not published — skip
                    string stageSub = Path.Combine(stageRoot, t.Sub);
                    CopyDir(remoteSub, stageSub);   // excludes *.config
                    // A first-time install has no local config to preserve, so it must take the
                    // server's one — otherwise the new exe starts with no server IP at all.
                    // Copy it straight across now (nothing is holding the file yet).
                    if (t.IsNewInstall) CopyConfigs(remoteSub, t.LocalDir);
                    staged.Add(t);
                }
                if (staged.Count == 0) { error = "No app builds found on the server share."; return false; }

                string bat = Path.Combine(Path.GetTempPath(), "croms_apply_update.bat");
                string excl = Path.Combine(Path.GetTempPath(), "croms_upd_exclude.txt");
                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("setlocal");
                // Close the aux apps so their files can be replaced (Main closes itself).
                foreach (var t in staged)
                    if (!t.Exe.Equals("CROMS.exe", StringComparison.OrdinalIgnoreCase))
                        sb.AppendLine("taskkill /IM " + t.Exe + " /F >nul 2>&1");
                // Wait for the main app (this process) to exit.
                sb.AppendLine(":wait");
                sb.AppendLine("tasklist /FI \"IMAGENAME eq CROMS.exe\" | find /I \"CROMS.exe\" >nul");
                sb.AppendLine("if not errorlevel 1 ( timeout /t 1 /nobreak >nul & goto wait )");
                // Keep each app's own *.config on copy.
                sb.AppendLine("echo .config> \"" + excl + "\"");
                foreach (var t in staged)
                    sb.AppendLine("xcopy \"" + Path.Combine(stageRoot, t.Sub) + "\\*\" \"" + t.LocalDir + "\\\" /E /Y /I /EXCLUDE:" + excl + " >nul");
                // Relaunch: Main always; Kiosk/Display only if they were running.
                foreach (var t in staged)
                {
                    bool relaunch = t.Exe.Equals("CROMS.exe", StringComparison.OrdinalIgnoreCase) || t.WasRunning;
                    if (relaunch)
                        sb.AppendLine("start \"\" \"" + Path.Combine(t.LocalDir, t.Exe) + "\"");
                }
                sb.AppendLine("rmdir /S /Q \"" + stageRoot + "\" >nul 2>&1");
                sb.AppendLine("del \"" + excl + "\" >nul 2>&1");
                sb.AppendLine("del \"%~f0\" >nul 2>&1");
                File.WriteAllText(bat, sb.ToString());

                Process.Start(new ProcessStartInfo
                {
                    FileName = bat,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Copies only the *.config files of a published app (first-time install).</summary>
        private static void CopyConfigs(string src, string dst)
        {
            try
            {
                Directory.CreateDirectory(dst);
                foreach (var f in Directory.GetFiles(src, "*.config"))
                {
                    string target = Path.Combine(dst, Path.GetFileName(f));
                    if (!File.Exists(target)) File.Copy(f, target, false);   // never clobber a local config
                }
            }
            catch { }
        }

        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                if (f.EndsWith(".config", StringComparison.OrdinalIgnoreCase)) continue;  // keep local config
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
            }
            foreach (var d in Directory.GetDirectories(src))
                CopyDir(d, Path.Combine(dst, Path.GetFileName(d)));
        }
    }
}

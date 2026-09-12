using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// Server-side "Publish New Release": (re)creates the LAN update share
    /// <c>\\&lt;server-ip&gt;\CROMSRelease</c> that client PCs pull from with their
    /// one-click Update button (see <see cref="AppUpdater"/>).
    ///
    /// It JUNCTIONS the three apps' build outputs (Main / Kiosk / Display) into
    /// C:\CROMSRelease, so the share always shows whatever Visual Studio last built -
    /// publish once, then every rebuild reaches clients with no second step.
    ///
    /// WHY THE EARLIER JUNCTION ATTEMPT FAILED, and what actually fixes it. The build
    /// outputs live under the user profile (...\OneDrive\...\bin\Debug), and that
    /// folder grants read to nobody but SYSTEM, Administrators and the owner. A junction
    /// is a reparse point: SMB resolves it on the server and then access-checks the
    /// TARGET, so granting Everyone read on C:\CROMSRelease - which is all the first
    /// attempt did - grants nothing at all, and the client gets "access denied". The fix
    /// is to grant read on the SOURCE folders as well. Intermediate profile directories
    /// do NOT need grants: Everyone holds SeChangeNotifyPrivilege (bypass traverse
    /// checking) by default, so only the final folder's ACL is evaluated.
    /// VERIFIED before shipping, not assumed: with the source granted, the share account
    /// reads CROMS.exe through the junction over SMB.
    ///
    /// The trade-off, stated plainly: a junction exposes the LIVE build folder, so a
    /// client updating in the middle of a rebuild can pull a half-written exe. That is
    /// the price of never re-publishing; the copy fallback below is the safe alternative.
    ///
    /// `net share` needs admin, so the work runs through one elevated batch (a single UAC
    /// prompt). If a junction cannot be created the batch copies instead, so a failure
    /// degrades to the old behaviour rather than leaving an empty share.
    /// </summary>
    public static class ReleasePublisher
    {
        public const string ReleaseDir = @"C:\CROMSRelease";

        /// <summary>
        /// Builds + runs an elevated batch that creates the release folder, junctions
        /// each app's build output into it, and shares it (Everyone = Read). Returns
        /// true on success; <paramref name="message"/> always carries a status/share path.
        /// </summary>
        public static bool Publish(out string message)
        {
            try
            {
                string mainDir = Path.GetDirectoryName(Application.ExecutablePath);
                string kioskDir = FindAppDir(mainDir, "CROMS.Kiosk.exe", "CROMS.Kiosk");
                string displayDir = FindAppDir(mainDir, "CROMS.Display.exe", "CROMS.Display");

                if (mainDir == null || !File.Exists(Path.Combine(mainDir, "CROMS.exe")))
                {
                    message = "Could not locate this app's own build folder.";
                    return false;
                }

                var sb = new StringBuilder();
                sb.AppendLine("@echo off");
                sb.AppendLine("if not exist \"" + ReleaseDir + "\" mkdir \"" + ReleaseDir + "\"");
                // Link the build outputs into C:\CROMSRelease so the share tracks every
                // later rebuild. Each one grants read on its SOURCE first - without that
                // the junction resolves to a folder the share account cannot open.
                LinkLines(sb, "Main", mainDir, "CROMS.exe");
                if (kioskDir != null) LinkLines(sb, "Kiosk", kioskDir, "CROMS.Kiosk.exe");
                if (displayDir != null) LinkLines(sb, "Display", displayDir, "CROMS.Display.exe");
                // Grant NTFS read to Everyone on the release folder so the SMB share is
                // actually readable from other PCs (share grant alone is not enough).
                sb.AppendLine("icacls \"" + ReleaseDir + "\" /grant *S-1-1-0:(OI)(CI)R /T >nul 2>&1");
                // (Re)create the share, Everyone = Read.
                sb.AppendLine("net share CROMSRelease /DELETE /Y >nul 2>&1");
                sb.AppendLine("net share CROMSRelease=\"" + ReleaseDir + "\" /GRANT:Everyone,READ");
                sb.AppendLine("exit /b 0");
                bool claimShared = false;

                string bat = Path.Combine(Path.GetTempPath(), "croms_publish_release.bat");
                File.WriteAllText(bat, sb.ToString());

                var psi = new ProcessStartInfo
                {
                    FileName = bat,
                    UseShellExecute = true,
                    Verb = "runas",                 // elevate — net share needs admin
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                using (var p = Process.Start(psi))
                    p.WaitForExit(30000);

                try { File.Delete(bat); } catch { }

                // Verify the EXE is actually reachable, not merely that a folder exists.
                // A failed mklink leaves an empty directory, which passes a Directory.Exists
                // check and would report success over an empty share.
                bool ok = File.Exists(Path.Combine(ReleaseDir, "Main", "CROMS.exe"));
                string ip = IonicServerManager.DetectLanIp();
                if (ok)
                {
                    bool live = IsJunction(Path.Combine(ReleaseDir, "Main"));
                    message = "Release published. Clients on this Wi-Fi update from:\r\n" +
                              @"\\" + ip + @"\CROMSRelease" +
                              "\r\n\r\nOn each client PC: open CROMS and click Update." +
                              (live
                                ? "\r\n\r\nThe share is LINKED to your build folders, so every later " +
                                  "rebuild in Visual Studio reaches clients on its own - you only need " +
                                  "to publish again if the build folders move."
                                : "\r\n\r\nNOTE: the build folder could not be linked, so this is a " +
                                  "COPY. Publish again after each rebuild.") +
                              (claimShared
                                ? "\r\n\r\nclaimapp is also on the share (\\...\\CROMSRelease\\ClaimApp) for a " +
                                  "mobile-serving PC — staff desktops don't need it."
                                : "");
                    return true;
                }
                message = "Publish did not complete (was the admin prompt approved?).";
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User cancelled the UAC elevation prompt.
                message = "Publishing needs administrator approval — the prompt was cancelled.";
                return false;
            }
            catch (Exception ex)
            {
                message = "Could not publish the release: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Points one share sub-folder at an app's live build output.
        /// Order matters. The SOURCE is granted read first, because a junction is
        /// access-checked against its target and without that grant the link is useless.
        /// The old entry then has to go, since mklink refuses an existing name: a bare
        /// rmdir removes a junction and never touches what it points at, and the /S form
        /// afterwards clears a real folder left behind by an older copy-style publish.
        /// If mklink fails for any reason we copy instead, so the share is never empty.
        /// </summary>
        private static void LinkLines(StringBuilder sb, string sub, string sourceDir, string exe)
        {
            string dst = Path.Combine(ReleaseDir, sub);
            // *S-1-1-0 is the Everyone SID - the literal name is localised, so spelling it
            // out would silently fail to match on a non-English Windows.
            sb.AppendLine("icacls \"" + sourceDir + "\" /grant *S-1-1-0:(OI)(CI)RX /T /C >nul 2>&1");
            sb.AppendLine("if exist \"" + dst + "\" rmdir \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("if exist \"" + dst + "\" rmdir /S /Q \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("mklink /J \"" + dst + "\" \"" + sourceDir + "\" >nul 2>&1");
            sb.AppendLine("if not exist \"" + Path.Combine(dst, exe) + "\" (");
            sb.AppendLine("  if exist \"" + dst + "\" rmdir /S /Q \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("  mkdir \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("  xcopy \"" + sourceDir + "\\*\" \"" + dst + "\\\" /E /Y /I >nul");
            sb.AppendLine(")");
        }

        /// <summary>True when the path is a reparse point - a live link, not a copy.</summary>
        private static bool IsJunction(string path)
        {
            try
            {
                var di = new DirectoryInfo(path);
                return di.Exists && (di.Attributes & FileAttributes.ReparsePoint) != 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// Finds an app's build folder from either layout: the deployed sibling layout
        /// (Kiosk/Display next to Main under one parent) or the dev/repo layout
        /// (…\CROMS.Kiosk\bin\Debug next to the .sln). Returns null if not found.
        /// </summary>
        private static string FindAppDir(string mainDir, string exe, string projectName)
        {
            try
            {
                // Deployment: exe in a sibling folder of Main's parent.
                string parent = Directory.GetParent(mainDir)?.FullName;
                if (parent != null)
                {
                    if (File.Exists(Path.Combine(parent, exe))) return parent;
                    foreach (var d in Directory.GetDirectories(parent))
                        if (File.Exists(Path.Combine(d, exe))) return d;
                }

                // Dev/repo: walk up to the folder that has the .sln, then <project>\bin\<config>.
                string dir = mainDir;
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    if (Directory.GetFiles(dir, "*.sln").Length > 0)
                    {
                        string config = Path.GetFileName(mainDir);   // Debug / Release
                        string cand = Path.Combine(dir, projectName, "bin", config);
                        if (File.Exists(Path.Combine(cand, exe))) return cand;
                        break;
                    }
                    dir = Directory.GetParent(dir)?.FullName;
                }
            }
            catch { }
            return null;
        }
    }
}

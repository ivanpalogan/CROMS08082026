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
    /// It COPIES the current build outputs of the three apps (Main / Kiosk / Display)
    /// into C:\CROMSRelease — a plain folder OUTSIDE the user profile, so network
    /// clients can read it (a junction into OneDrive/profile is blocked → "access
    /// denied"). Grants Everyone NTFS read + shares it. `net share`/`icacls` need
    /// admin, so the work runs through one elevated batch (a single UAC prompt).
    /// Re-run Publish after each rebuild to refresh the shared build.
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
                // COPY the build outputs into C:\CROMSRelease (a plain folder OUTSIDE the
                // user profile / OneDrive). Junctions into the profile are blocked for
                // network users ("access denied"), so we copy real files instead.
                CopyLines(sb, "Main", mainDir);
                if (kioskDir != null) CopyLines(sb, "Kiosk", kioskDir);
                if (displayDir != null) CopyLines(sb, "Display", displayDir);
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

                // Verify the junction landed (the elevated process ran).
                bool ok = Directory.Exists(Path.Combine(ReleaseDir, "Main"));
                string ip = IonicServerManager.DetectLanIp();
                if (ok)
                {
                    message = "Release published. Clients on this Wi-Fi update from:\r\n" +
                              @"\\" + ip + @"\CROMSRelease" +
                              "\r\n\r\nOn each client PC: open CROMS and click Update." +
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

        private static void CopyLines(StringBuilder sb, string sub, string sourceDir)
        {
            string dst = Path.Combine(ReleaseDir, sub);
            // Clear any old junction/folder, then copy the fresh build output as real files.
            sb.AppendLine("if exist \"" + dst + "\" rmdir /S /Q \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("mkdir \"" + dst + "\" >nul 2>&1");
            sb.AppendLine("xcopy \"" + sourceDir + "\\*\" \"" + dst + "\\\" /E /Y /I >nul");
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

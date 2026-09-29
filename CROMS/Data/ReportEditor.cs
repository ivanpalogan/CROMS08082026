using System;
using System.Diagnostics;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Opens a .rpt in the Crystal designer so its layout can be edited from the preview window.
    /// <para/>
    /// Deliberately mentions NO Crystal types, so it loads on a PC without the runtime. The
    /// designer itself is Visual Studio's Crystal Reports package (<c>devenv.exe file.rpt</c>);
    /// where it is not installed, the .rpt's folder is shown instead so the file can be copied
    /// to a PC that has it.
    /// <para/>
    /// On a development machine the .rpt that gets EDITED is the one in the repo
    /// (<c>CROMS\Reports</c>), not the build-output copy, so the edit survives a rebuild and is
    /// what gets committed. <see cref="SyncFromSource"/> then carries it into the copy the
    /// viewer loads the next time the report opens.
    /// </summary>
    internal static class ReportEditor
    {
        /// <summary>The repo copy of a deployed .rpt, or null on a machine without the source tree.</summary>
        public static string SourceCopy(string loadedPath)
        {
            try
            {
                string name = Path.GetFileName(loadedPath);
                string loaded = Path.GetFullPath(loadedPath);
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
                {
                    foreach (string rel in new[] { @"CROMS\Reports", "Reports" })
                    {
                        string p = Path.Combine(dir, rel, name);
                        if (File.Exists(p) && !string.Equals(Path.GetFullPath(p), loaded, StringComparison.OrdinalIgnoreCase)
                            && Directory.GetFiles(Path.GetDirectoryName(p), "README.md").Length > 0)
                            return p;
                    }
                    dir = Path.GetDirectoryName(dir.TrimEnd('\\'));
                }
            }
            catch { }
            return null;
        }

        /// <summary>The file the designer should open: the repo copy if there is one, else the loaded one.</summary>
        public static string EditPath(string loadedPath)
        {
            return SourceCopy(loadedPath) ?? loadedPath;
        }

        /// <summary>Copies a newer repo copy over the deployed one so an edit shows on the next preview.</summary>
        public static void SyncFromSource(string loadedPath)
        {
            try
            {
                string src = SourceCopy(loadedPath);
                if (src != null && File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(loadedPath))
                    File.Copy(src, loadedPath, true);
            }
            catch { /* a locked or read-only output copy just keeps the old layout */ }
        }

        /// <summary>Launches the designer on the report. Returns a message for the operator when it cannot.</summary>
        public static string Open(string loadedPath)
        {
            string path = EditPath(loadedPath);
            if (!File.Exists(path)) return "The report file was not found:\n" + path;

            string devenv = FindDevenv();
            try
            {
                if (devenv != null)
                {
                    Process.Start(new ProcessStartInfo(devenv, "\"" + path + "\"") { UseShellExecute = false });
                    return null;
                }
                // Not installed here: at least hand over the file.
                Process.Start("explorer.exe", "/select,\"" + path + "\"");
                return "The Crystal Reports designer (Visual Studio) is not installed on this PC.\n\n" +
                       "The report's folder was opened instead. Copy the file to a PC that has Visual Studio " +
                       "with Crystal Reports, edit it there, and put it back:\n" + path;
            }
            catch (Exception ex) { return "Could not open the designer: " + ex.Message; }
        }

        private static string FindDevenv()
        {
            string[] roots =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            foreach (string root in roots)
                foreach (string year in new[] { "2022", "2019", "2017" })
                    foreach (string ed in new[] { "Community", "Professional", "Enterprise" })
                    {
                        string p = Path.Combine(root, "Microsoft Visual Studio", year, ed, @"Common7\IDE\devenv.exe");
                        if (File.Exists(p)) return p;
                    }
            return null;
        }
    }
}

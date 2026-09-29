using System;
using System.Diagnostics;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Finds the Mobile Capture runtime (the Node save-API folder and node.exe) relative to THIS
    /// install, so CROMS can be copied to another office PC without editing a path or installing
    /// Node.js: a release bundle carries <c>MobileApp\</c> (server, ssl, node_modules) and
    /// <c>MobileApp\node\node.exe</c> beside CROMS.exe (built by Scripts\Build-MobileRuntime.ps1).
    /// On a development PC the configured folder and a system-wide Node are used as before.
    /// </summary>
    internal static class MobileRuntime
    {
        public static string ExeDir { get { return AppDomain.CurrentDomain.BaseDirectory; } }

        /// <summary>First existing folder of: the configured path, the bundled folder beside the
        /// exe, the development default. Falls back to the configured/default text when none exist,
        /// so the caller's own "folder not found" message still names something meaningful.</summary>
        public static string ResolveDir(string configured, string devDefault, string bundledFolder)
        {
            configured = (configured ?? "").Trim();
            string bundled = Path.Combine(ExeDir, bundledFolder);
            if (configured.Length > 0 && Directory.Exists(configured)) return configured;
            if (Directory.Exists(bundled)) return bundled;
            if (Directory.Exists(devDefault)) return devDefault;
            return configured.Length > 0 ? configured : devDefault;
        }

        /// <summary>Full path of node.exe (bundled first, then PATH, then the standard install
        /// folder), or null when Node.js is not available on this PC.</summary>
        public static string FindNode()
        {
            string[] bundled =
            {
                Path.Combine(ExeDir, "node", "node.exe"),
                Path.Combine(ExeDir, "MobileApp", "node", "node.exe"),
            };
            foreach (string b in bundled) if (File.Exists(b)) return b;

            try
            {
                string path = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in path.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(dir)) continue;
                    try
                    {
                        string c = Path.Combine(dir.Trim().Trim('"'), "node.exe");
                        if (File.Exists(c)) return c;
                    }
                    catch { }
                }
            }
            catch { }

            string std = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
            return File.Exists(std) ? std : null;
        }

        /// <summary>Puts node.exe's folder first on the child process's PATH, so a command such as
        /// "node index.js" run through cmd finds the bundled copy.</summary>
        public static void PrepareEnvironment(ProcessStartInfo psi)
        {
            try
            {
                string node = FindNode();
                if (node == null) return;
                string dir = Path.GetDirectoryName(node);
                string cur = psi.EnvironmentVariables["PATH"] ?? "";
                if (cur.IndexOf(dir, StringComparison.OrdinalIgnoreCase) < 0)
                    psi.EnvironmentVariables["PATH"] = dir + Path.PathSeparator + cur;
            }
            catch { }
        }
    }
}

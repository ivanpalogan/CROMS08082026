using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CROMS.Modules;

namespace CROMS.Forms
{
    /// <summary>What the operator chose on the launcher.</summary>
    public enum LauncherChoice { Exit, Admin, Display, Kiosk, All }

    /// <summary>
    /// Startup chooser shown when CROMS.exe runs: pick which app to open on THIS computer —
    /// Admin (this app), the public Display board, or the client Kiosk — or "Run All Three".
    /// Admin runs in this process; Display/Kiosk are launched as their own sibling .exe.
    /// Lets one machine (dev / demo) decide exactly what opens instead of everything at once.
    /// <para/>
    /// Visual style matches LoginForm: Navy Blue (light), rounded cards, minimal single-color
    /// line icons (no emoji) — see Modules/UiTheme.cs for the palette used app-wide.
    /// </summary>
    public sealed partial class LauncherForm : Form
    {
        public LauncherChoice Choice { get; private set; } = LauncherChoice.Exit;

        public LauncherForm()
        {
            InitializeComponent();
        }

        private void Pick(LauncherChoice c) { Choice = c; DialogResult = DialogResult.OK; Close(); }

        // ------------------------------------------------------------ static API
        /// <summary>Shows the chooser and returns the operator's choice.</summary>
        public static LauncherChoice Ask()
        {
            using (var f = new LauncherForm())
            {
                f.ShowDialog();
                return f.Choice;
            }
        }

        /// <summary>Starts a sibling app (CROMS.Display / CROMS.Kiosk) as its own process.</summary>
        public static void Start(string projectName)
        {
            string exe = ResolveExe(projectName);
            if (exe == null)
            {
                MessageBox.Show(
                    "Could not find " + projectName + ".exe.\r\n\r\nLooked in:\r\n  " + Application.StartupPath +
                    "\r\n\r\nOn a client PC all three apps must sit in the SAME folder - copy " +
                    projectName + ".exe and its files next to CROMS.exe. Scripts\\Build-Bundle.ps1 makes that folder.",
                    "CROMS Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try { Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) }); }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start " + projectName + ": " + ex.Message,
                    "CROMS Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Finds a sibling app's .exe. Tries the same folder (an all-in-one deployment), then
        /// the dev solution layout &lt;solution&gt;\&lt;project&gt;\bin\&lt;config&gt;\&lt;project&gt;.exe.
        /// </summary>
        private static string ResolveExe(string projectName)
        {
            string exeName = projectName + ".exe";
            string here = Application.StartupPath;   // ...\CROMS\bin\Debug

            string sameFolder = Path.Combine(here, exeName);
            if (File.Exists(sameFolder)) return sameFolder;

            // Sibling-folder deployment (what AppUpdater installs into):
            //   ...\CROMS\Main\CROMS.exe + ...\CROMS\Kiosk\CROMS.Kiosk.exe + ...\Display\...
            try
            {
                string parent = Directory.GetParent(here)?.FullName;
                if (parent != null)
                {
                    string atParent = Path.Combine(parent, exeName);
                    if (File.Exists(atParent)) return atParent;
                    foreach (string d in Directory.GetDirectories(parent))
                    {
                        string p = Path.Combine(d, exeName);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { }

            // Dev layout: up from <project>\bin\<config> to the solution root, then across.
            try
            {
                string config = new DirectoryInfo(here).Name;                 // Debug / Release
                string solutionRoot = Directory.GetParent(here)?.Parent?.Parent?.FullName;
                if (solutionRoot != null)
                {
                    string devPath = Path.Combine(solutionRoot, projectName, "bin", config, exeName);
                    if (File.Exists(devPath)) return devPath;
                    // Fall back to the other config if the chosen one isn't built.
                    foreach (string cfg in new[] { "Debug", "Release" })
                    {
                        string p = Path.Combine(solutionRoot, projectName, "bin", cfg, exeName);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch { /* fall through to null */ }
            return null;
        }
    }
}

using System;
using System.IO;
using System.Windows.Forms;

namespace CROMS.Data
{
    /// <summary>
    /// Creates a "CROMS" shortcut on the Windows desktop the first time the app
    /// runs, so staff have a clickable icon instead of hunting for CROMS.exe in a
    /// folder. Uses the Windows Script Host COM object by ProgID (late-bound), so
    /// no COM reference is needed in the project.
    /// </summary>
    public static class DesktopShortcut
    {
        /// <summary>
        /// Ensure a desktop shortcut to this exe exists. Runs once (skips if the
        /// shortcut is already there). Best-effort — never throws.
        /// </summary>
        public static void Ensure()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string linkPath = Path.Combine(desktop, "CROMS.lnk");
                if (File.Exists(linkPath)) return;   // already created / user has it

                string exe = Application.ExecutablePath;
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;

                dynamic shell = Activator.CreateInstance(shellType);
                dynamic sc = shell.CreateShortcut(linkPath);
                sc.TargetPath = exe;
                sc.WorkingDirectory = Path.GetDirectoryName(exe);
                sc.IconLocation = exe + ",0";
                sc.Description = "CROMS — Civil Registry Operations Management System";
                sc.Save();
            }
            catch { /* shortcut is a convenience; ignore any failure */ }
        }
    }
}

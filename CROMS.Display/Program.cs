using System;
using System.Windows.Forms;

namespace CROMS.Display
{
    /// <summary>
    /// Entry point for the public "Now Serving" display — a separate, view-only
    /// program that shares the CROMS database. Run this on the waiting-area PC/TV;
    /// staff drive the queue from the main CROMS app.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DisplayForm());
        }
    }
}

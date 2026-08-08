using System;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Entry point for the client self-service kiosk. Clients fill in what they need
    /// and submit; a queue ticket is created in the shared CROMS database and the
    /// client is given a number to wait for. Staff serve them from the main app.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new KioskForm());
        }
    }
}

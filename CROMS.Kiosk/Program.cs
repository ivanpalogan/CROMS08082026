using System;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Entry point for the client self-service kiosk. The wizard is split into separate
    /// full-screen windows — <see cref="WelcomeForm"/> (attract / touch to start),
    /// <see cref="ServiceSelectForm"/> (Step 1) and <see cref="DetailsPhotoForm"/> (Step 2) —
    /// and this controller drives the flow between them, carrying one <see cref="KioskSession"/>
    /// across the handoffs. Closing a window (Alt+F4 / X) quits the kiosk.
    /// <para/>
    /// The kiosk always comes to rest on the Welcome screen, and only leaves it on a deliberate
    /// tap. That is what guarantees a clean session boundary: a client can never arrive at a
    /// screen still carrying the previous person's choices.
    /// </summary>
    internal static class Program
    {
        private enum Step { Welcome, ChooseServices, Details }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            RunFlow();
        }

        private static void RunFlow()
        {
            var session = new KioskSession();
            var step = Step.Welcome;

            while (true)
            {
                switch (step)
                {
                    case Step.Welcome:
                        using (var f = new WelcomeForm())
                        {
                            f.ShowDialog();
                            session.Reset();              // always start a client from zero
                            step = Step.ChooseServices;
                        }
                        break;

                    case Step.ChooseServices:
                        using (var f = new ServiceSelectForm(session))
                        {
                            step = f.ShowDialog() == DialogResult.OK
                                ? Step.Details                // Next -> details
                                : Step.Welcome;               // idle timeout -> back to attract
                        }
                        break;

                    default:
                        using (var f = new DetailsPhotoForm(session))
                        {
                            DialogResult r = f.ShowDialog();
                            if (r == DialogResult.Cancel)
                            {
                                // Back — keep the session so Step 1 re-highlights their choices.
                                step = Step.ChooseServices;
                            }
                            else
                            {
                                // Submitted, or idled out — done with this client.
                                session.Reset();
                                step = Step.Welcome;
                            }
                        }
                        break;
                }
            }
        }
    }
}

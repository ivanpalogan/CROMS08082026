using System.Windows.Forms;

namespace CROMS.Forms
{
    /// <summary>
    /// Incoming / Outgoing module form. Open in the WinForms Designer and drag controls
    /// (Label, Panel, TextBox, Button...) onto it. Embedded in the MainForm content
    /// panel at runtime (TopLevel = false), so it shows inside the app shell, not as
    /// a separate window. Wire to the data layer when built.
    /// </summary>
    public partial class IncomingOutgoingForm : Form
    {
        public IncomingOutgoingForm()
        {
            InitializeComponent();
        }
    }
}

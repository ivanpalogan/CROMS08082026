using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Sets MaxLength on text boxes / editable combos so a screen cannot take more than the
    /// database column behind it can store (typing and pasting both stop at the limit).
    /// One call per width instead of a literal on every control keeps the widths readable
    /// next to the column they mirror.
    /// </summary>
    internal static class FieldLimit
    {
        public static void Cap(int length, params Control[] controls)
        {
            if (controls == null) return;
            foreach (Control c in controls)
            {
                TextBox t = c as TextBox;
                if (t != null) { t.MaxLength = length; continue; }
                ComboBox b = c as ComboBox;
                if (b != null) b.MaxLength = length;
            }
        }
    }
}

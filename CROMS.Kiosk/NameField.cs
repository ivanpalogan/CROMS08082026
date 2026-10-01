using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// One rule for every name box on the kiosk (First / Middle / Last / Suffix): the box takes
    /// letters (including ñ and accented letters), spaces, hyphens and apostrophes, and nothing
    /// else; it never starts with a space or holds a double space; and on leaving it is trimmed.
    /// <para/>
    /// It only ever REMOVES characters that cannot be part of a name or collapses spacing. It never
    /// changes a letter, so "Dela Peña", "D'Souza", "Dela Cruz-Santos" and "de la Cruz" stay exactly
    /// as typed. A full stop is also accepted (and kept) because "Ma. Cristina", a middle initial
    /// "A." and a suffix "Jr." are all ordinary Filipino name forms.
    /// </summary>
    internal static class NameField
    {
        /// <summary>Whether <paramref name="c"/> may appear in a name box.</summary>
        public static bool Allowed(char c, bool allowPeriod)
        {
            return char.IsLetter(c) || c == ' ' || c == '-' || c == '\'' || c == '’' || (allowPeriod && c == '.');
        }

        /// <summary>The text as it should look WHILE typing or pasting: bad characters dropped, runs
        /// of spaces collapsed to one, the typographic apostrophe made plain. Not trimmed, so a
        /// space typed between two words is not eaten before the next word arrives.</summary>
        public static string Typing(string s, bool allowPeriod)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool lastSpace = true;   // true at the start, so a name never begins with a space
            foreach (char raw in s)
            {
                char c = raw == '’' ? '\'' : raw;
                if (char.IsWhiteSpace(c)) c = ' ';
                if (!Allowed(c, allowPeriod)) continue;
                if (c == ' ') { if (lastSpace) continue; lastSpace = true; }
                else lastSpace = false;
                sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>The final value: <see cref="Typing"/> plus the trailing space trimmed.</summary>
        public static string Clean(string s, bool allowPeriod)
        {
            return Typing(s, allowPeriod).Trim();
        }

        /// <summary>True when the text holds at least one letter - a name made only of hyphens or
        /// apostrophes is not a name.</summary>
        public static bool HasLetter(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (char.IsLetter(c)) return true;
            return false;
        }

        /// <summary>Applies the rule to a box. <paramref name="cue"/> is the grey hint shown while it is empty.</summary>
        public static void Attach(TextBox box, bool allowPeriod = false, string cue = null)
        {
            if (box == null) return;
            box.MaxLength = 60;
            bool fixing = false;

            box.KeyPress += (s, e) =>
            {
                char c = e.KeyChar;
                if (char.IsControl(c)) return;                       // backspace, ctrl+V, enter...
                if (!Allowed(c, allowPeriod)) { e.Handled = true; return; }
                if (c == ' ')
                {
                    // No space at the start, and none straight after another space.
                    int at = box.SelectionStart;
                    bool afterSpace = at > 0 && box.Text.Length >= at && box.Text[at - 1] == ' ';
                    if (at == 0 || afterSpace) e.Handled = true;
                }
            };

            // A paste (or an input method) can bring in anything: clean it, and keep the caret
            // where it was relative to the text that survived.
            box.TextChanged += (s, e) =>
            {
                if (fixing) return;
                string cleaned = Typing(box.Text, allowPeriod);
                if (cleaned == box.Text) return;
                int caret = Typing(box.Text.Substring(0, Math.Min(box.SelectionStart, box.Text.Length)), allowPeriod).Length;
                fixing = true;
                try { box.Text = cleaned; box.SelectionStart = Math.Min(caret, cleaned.Length); }
                finally { fixing = false; }
            };

            box.Leave += (s, e) =>
            {
                string final = Clean(box.Text, allowPeriod);
                if (final != box.Text) box.Text = final;
            };

            if (!string.IsNullOrEmpty(cue)) SetCue(box, cue);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private static void SetCue(TextBox box, string cue)
        {
            EventHandler set = (s, e) => SendMessage(box.Handle, 0x1501 /* EM_SETCUEBANNER */, (IntPtr)1, cue);
            if (box.IsHandleCreated) set(box, EventArgs.Empty);
            else box.HandleCreated += set;
        }
    }
}

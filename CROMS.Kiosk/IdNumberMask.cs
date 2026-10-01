using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace CROMS.Kiosk
{
    /// <summary>
    /// Shapes an ID-number box to the format of whichever ID type is picked, the same way the
    /// mobile-number box is shaped: only characters that fit the next slot are accepted, groups
    /// are spaced automatically, letters are upper-cased, and the box stops at the format's
    /// length. Pattern letters: # = digit, A = letter, X = letter or digit.
    /// ID types with no fixed national format (OWWA, company/school, Other, anything typed in)
    /// stay free text — forcing a shape on them would turn real IDs away.
    /// </summary>
    internal static class IdNumberMask
    {
        private struct Fmt { public string Mask, Example; public Fmt(string m, string e) { Mask = m; Example = e; } }

        // Keyed by the exact ID type names in KioskCore.IdTypes.
        private static readonly Dictionary<string, Fmt> Formats = new Dictionary<string, Fmt>
        {
            { "Philippine National ID (PhilSys)",        new Fmt("#### #### #### ####", "1234 5678 9012 3456") },
            { "Philippine Passport (DFA)",               new Fmt("A########",            "P12345678") },
            { "Driver's License (LTO)",                  new Fmt("A## ## ######",        "N12 34 567890") },
            { "UMID (Unified Multi-Purpose ID)",         new Fmt("#### ####### #",       "1234 5678901 2") },
            { "SSS ID",                                  new Fmt("## ####### #",         "34 0123456 7") },
            { "GSIS eCard",                              new Fmt("##########",           "1234567890") },
            { "PRC ID (Professional License)",           new Fmt("#######",              "1234567") },
            { "Voter's ID / COMELEC Certification",      new Fmt("##########",           "1234567890") },
            { "Postal ID (PHLPost)",                     new Fmt("#### #### ####",       "1234 5678 9012") },
            { "PhilHealth ID",                           new Fmt("## ######### #",       "07 123456789 1") },
            { "TIN ID (BIR)",                            new Fmt("### ### ### ###",      "123 456 789 000") },
            { "Pag-IBIG Loyalty Card Plus",              new Fmt("#### #### ####",       "1234 5678 9012") },
            { "Senior Citizen ID (OSCA)",                new Fmt("##########",           "1234567890") },
            // The NCDA number is RR-PPMM-BBB-NNNNNNN = 2+4+3+7 digits.
            { "PWD ID",                                  new Fmt("## #### ### #######",  "04 5402 013 0000001") },
            { "Solo Parent ID",                          new Fmt("##########",           "1234567890") },
            { "Barangay ID / Certification (with photo)", new Fmt("##########",          "1234567890") },
            { "NBI Clearance",                           new Fmt("XXXXXXX XXXXXXXX" ,    "NBI2026 12345678") },
            { "Police Clearance",                        new Fmt("XXXXXXX XXXXXXXX" ,    "NPC2026 12345678") },
        };

        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public static void Attach(TextBox box, ComboBox type)
        {
            if (box == null || type == null) return;
            bool busy = false;

            Func<string> mask = () => Lookup(type, false);

            Action reformat = () =>
            {
                if (busy) return;
                busy = true;
                try { Format(box, Lookup(type, false)); SetCue(box, Lookup(type, true)); }
                finally { busy = false; }
            };

            box.KeyPress += (s, e) =>
            {
                string m = mask();
                if (m == null || char.IsControl(e.KeyChar)) return;
                if (!char.IsLetterOrDigit(e.KeyChar)) { e.Handled = true; return; }
                if (box.SelectionLength > 0) { e.KeyChar = char.ToUpperInvariant(e.KeyChar); return; }
                int have = 0;
                foreach (char c in box.Text) if (char.IsLetterOrDigit(c)) have++;
                int slot = -1, n = 0;
                foreach (char p in m) { if (p == ' ') continue; if (n++ == have) { slot = p; break; } }
                if (slot < 0 || !Fits((char)slot, e.KeyChar)) { e.Handled = true; return; }
                e.KeyChar = char.ToUpperInvariant(e.KeyChar);
            };
            box.TextChanged += (s, e) => reformat();
            box.HandleCreated += (s, e) => SetCue(box, Lookup(type, true));
            type.SelectedIndexChanged += (s, e) => reformat();
            type.TextChanged += (s, e) => reformat();
            reformat();
        }

        private static string Lookup(ComboBox type, bool example)
        {
            Fmt f;
            if (!Formats.TryGetValue((type.Text ?? "").Trim(), out f)) return null;
            return example ? f.Example : f.Mask;
        }

        private static bool Fits(char slot, char c)
        {
            switch (slot)
            {
                case '#': return c >= '0' && c <= '9';
                case 'A': return char.IsLetter(c);
                default:  return char.IsLetterOrDigit(c);
            }
        }

        private static void Format(TextBox box, string mask)
        {
            if (mask == null) return;
            var raw = new StringBuilder();
            foreach (char c in box.Text) if (char.IsLetterOrDigit(c)) raw.Append(char.ToUpperInvariant(c));

            var sb = new StringBuilder();
            int ri = 0;
            bool gap = false;
            foreach (char p in mask)
            {
                if (p == ' ') { gap = true; continue; }
                while (ri < raw.Length && !Fits(p, raw[ri])) ri++;   // a char that fits no slot is dropped
                if (ri >= raw.Length) break;
                if (gap && sb.Length > 0) sb.Append(' ');
                gap = false;
                sb.Append(raw[ri++]);
            }
            string next = sb.ToString();
            if (next == box.Text) return;
            box.Text = next;
            box.SelectionStart = box.Text.Length;
        }

        private static void SetCue(TextBox box, string example)
        {
            if (!box.IsHandleCreated) return;
            SendMessage(box.Handle, EM_SETCUEBANNER, (IntPtr)1, example == null ? "" : "e.g. " + example);
        }
    }
}

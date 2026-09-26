using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// Auto-capitalizes the first letter of each word in a name field as the operator types
    /// ("john" -&gt; "John", "dela cruz" -&gt; "Dela Cruz"). Only a lowercase letter sitting at the
    /// start of a word is changed — everything else typed (including an already-capitalized
    /// letter, or a letter mid-word) is left exactly as written, so this cannot mangle a name
    /// like "McDonald" or "O'Brien".
    /// </summary>
    public static class AutoCaps
    {
        private static readonly ConditionalWeakTable<TextBox, object> _applying =
            new ConditionalWeakTable<TextBox, object>();

        public static void Attach(params TextBox[] boxes)
        {
            foreach (TextBox box in boxes)
            {
                if (box == null) continue;
                box.TextChanged += (s, e) => Apply(box);
            }
        }

        private static void Apply(TextBox box)
        {
            object ignored;
            if (_applying.TryGetValue(box, out ignored)) return;

            string text = box.Text;
            if (string.IsNullOrEmpty(text)) return;

            char[] chars = text.ToCharArray();
            bool atWordStart = true;
            bool changed = false;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsLetter(c))
                {
                    if (atWordStart && char.IsLower(c))
                    {
                        chars[i] = char.ToUpper(c);
                        changed = true;
                    }
                    atWordStart = false;
                }
                else if (c == ' ' || c == '-' || c == '\'' || c == '.')
                {
                    atWordStart = true;
                }
                else
                {
                    atWordStart = false;
                }
            }
            if (!changed) return;

            int selStart = box.SelectionStart;
            int selLen = box.SelectionLength;
            _applying.Add(box, null);
            try { box.Text = new string(chars); }
            finally { _applying.Remove(box); }
            box.SelectionStart = Math.Min(selStart, box.Text.Length);
            box.SelectionLength = selLen;
        }
    }
}

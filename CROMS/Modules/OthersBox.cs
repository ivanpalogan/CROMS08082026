using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CROMS.Modules
{
    /// <summary>
    /// "Others, specify ______" — the pattern every PSA form uses, wired once.
    /// <para/>
    /// A dropdown that offers "Others" and nothing else is a dead end: the encoder picks it,
    /// the record says "Others", and what the paper actually says is lost. These forms print
    /// the word "specify" next to the box for that reason, so the screen has to as well.
    /// <para/>
    /// WHAT GETS STORED, and why it is not just the typed text. The value is written as
    /// <c>Others - Traditional Birth Attendant</c>: the CATEGORY survives, so a PSA count of
    /// "attended by others" is still possible, and the DETAIL survives, so the certificate
    /// can be reprinted with what the paper says. Storing only the typed text would lose the
    /// category; storing only "Others" would lose the answer.
    /// <para/>
    /// The specify box is hidden — not merely blank — whenever the choice is anything else,
    /// for the same reason the marriage fields grey out on an unmarried birth: an empty box
    /// that does not apply reads as one somebody forgot.
    /// </summary>
    public static class OthersBox
    {
        /// <summary>The separator between the category and the specified detail.</summary>
        private const string Join = " - ";

        /// <summary>
        /// Show <paramref name="box"/> (and its caption, if any) only while
        /// <paramref name="combo"/> is on an "Others" choice, and clear it when it is not.
        /// Safe to call on a combo that has no Others entry — it simply never shows.
        /// </summary>
        public static void Bind(ComboBox combo, TextBox box, Control caption = null)
        {
            if (combo == null || box == null) return;
            EventHandler apply = (s, e) => Refresh(combo, box, caption);
            combo.SelectedIndexChanged += apply;
            combo.TextChanged += apply;
            Refresh(combo, box, caption);
        }

        private static void Refresh(ComboBox combo, TextBox box, Control caption)
        {
            bool others = IsOthers(combo.Text);
            if (!others && box.Text.Length > 0) box.Clear();
            box.Visible = others;
            if (caption != null) caption.Visible = others;
        }

        /// <summary>
        /// True for the several spellings a form and a master file use between them —
        /// "Others", "Other", "Others (specify)", "Other, specify".
        /// </summary>
        public static bool IsOthers(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   Regex.IsMatch(value.Trim(), @"^others?\b", RegexOptions.IgnoreCase);
        }

        /// <summary>The value to store: the choice, plus the specified detail when there is one.</summary>
        public static string Compose(ComboBox combo, TextBox box)
        {
            string choice = (combo == null ? "" : combo.Text ?? "").Trim();
            string detail = (box == null ? "" : box.Text ?? "").Trim();
            if (!IsOthers(choice) || detail.Length == 0) return choice;
            return choice + Join + detail;
        }

        /// <summary>
        /// Put a stored value back on screen, splitting "Others - detail" into the choice
        /// and the specify box.
        /// </summary>
        public static void Apply(ComboBox combo, TextBox box, Control caption, string stored,
                                 Action<ComboBox, string> select)
        {
            stored = (stored ?? "").Trim();
            string choice = stored, detail = "";

            if (IsOthers(stored))
            {
                int cut = stored.IndexOf(Join, StringComparison.Ordinal);
                if (cut > 0)
                {
                    choice = stored.Substring(0, cut).Trim();
                    detail = stored.Substring(cut + Join.Length).Trim();
                }
            }

            if (select != null) select(combo, choice);
            else if (combo != null) combo.Text = choice;

            if (box != null) box.Text = detail;
            Refresh(combo, box, caption);
        }
    }
}

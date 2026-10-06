namespace CROMS.OwnOcr
{
    /// <summary>
    /// The characters the classifier can name. A civil-registry value is a name, a place, a
    /// date or a number, so this is letters (both cases), digits, the punctuation those use,
    /// and the enye - "Pe\u00F1ablanca" is the office's own municipality.
    /// <para/>
    /// Written with escapes rather than literal non-ASCII characters on purpose: a source file
    /// read in the wrong code page is how this project once stored the enye as two
    /// box-drawing characters (2026-09-06).
    /// </summary>
    public static class Charset
    {
        public const string Chars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            ".,-'/():;&#" +
            "\u00D1\u00F1";

        public static int Count { get { return Chars.Length; } }

        public static int IndexOf(char c) { return Chars.IndexOf(c); }

        /// <summary>
        /// The label of a picture that is NOT one character: two letters stuck together, or half a
        /// letter. A classifier that only knows letters will read such a picture as the letter it
        /// most resembles - a pair of narrow letters comes out as a confident 'm' - so it is given
        /// a class of its own, and the splitter can then see that a cut is wrong.
        /// </summary>
        public static int Reject { get { return Chars.Length; } }

        /// <summary>Number of labels including the reject class.</summary>
        public static int LabelCount { get { return Chars.Length + 1; } }

        public static char At(int index) { return index >= 0 && index < Chars.Length ? Chars[index] : '\0'; }
    }
}

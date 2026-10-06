namespace CROMS.OwnOcr
{
    /// <summary>
    /// The characters the classifier can name. A civil-registry value is a name, a place, a
    /// date or a number, so this is letters (both cases), digits, the punctuation those use,
    /// and the enye - "Peñablanca" is the office's own municipality.
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
            "Ññ";

        public static int Count { get { return Chars.Length; } }

        public static int IndexOf(char c) { return Chars.IndexOf(c); }

        public static char At(int index) { return Chars[index]; }
    }
}

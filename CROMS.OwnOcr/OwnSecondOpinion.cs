using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CROMS.OwnOcr
{
    /// <summary>
    /// The own engine used as a SECOND OPINION next to Tesseract, never as a replacement.
    /// <para/>
    /// Measured on the office's own scans (CLAUDE.md, 2026-10-07): Tesseract reads more fields
    /// right overall, so the own engine is only allowed to speak where it has something
    /// Tesseract cannot: a field whose answer comes from a small CLOSED list (province,
    /// municipality, citizenship, religion, civil status). There the own engine's reading is
    /// repaired against the list, and it may replace the current value only when
    /// <list type="bullet">
    ///   <item>its repaired reading IS an entry of that list, and</item>
    ///   <item>the current value is not an entry (a misread such as "Filip ino"), or is the same
    ///         entry wrapped in stray marks ("» PAMPANGA").</item>
    /// </list>
    /// A value Tesseract already got onto the list is never overridden, so this cannot turn a
    /// correct answer into a different list entry. Personal names have no public list and are
    /// never touched (a name list gets more dangerous as the registry grows: 2026-09-04).
    /// <para/>
    /// Thread-safe after Load: the classifier and the lexicons are read-only.
    /// </summary>
    public sealed class OwnSecondOpinion
    {
        /// <summary>What the own engine made of one crop.</summary>
        public sealed class Opinion
        {
            public string Raw = "";          // the classifier's reading
            public string Text = "";         // after lexicon repair
            public bool IsMember;            // the repaired text is exactly one entry of the field's list
        }

        private OwnOcrReader _reader;
        private LexiconSet _lex;

        /// <summary>Folder that holds refs.bin and the lexicon folder.</summary>
        public const string RefsFile = "refs.bin";

        /// <summary>
        /// Loads the reference glyphs and the public lexicons from <paramref name="dir"/>
        /// (refs.bin + lexicon\*.txt). Throws when a file is missing - callers decide whether that
        /// is fatal; the app treats it as "second opinion unavailable".
        /// </summary>
        public static OwnSecondOpinion Load(string dir)
        {
            string refs = Path.Combine(dir, RefsFile);
            string lexDir = Path.Combine(dir, "lexicon");
            if (!File.Exists(refs)) throw new FileNotFoundException("Own OCR reference set not found.", refs);
            if (!Directory.Exists(lexDir)) throw new DirectoryNotFoundException("Own OCR lexicon folder not found: " + lexDir);

            var samples = Dataset.Load(refs);
            // refs.bin is already the capped set, so the cap here is a no-op (same seed as the
            // benchmark that measured it).
            var reader = new OwnOcrReader(new KnnClassifier(samples, 600, 3));
            reader.LettersOnly = true; reader.CaseConsistency = true; reader.TrimEdgeMarks = true;
            return new OwnSecondOpinion { _reader = reader, _lex = LexiconSet.Load(lexDir) };
        }

        /// <summary>True when the field is answered from a closed list the own engine may speak on.</summary>
        public bool IsClosedListField(string key) { return _lex.IsClosedListField(key); }

        /// <summary>Read one field crop. Safe to call from several threads at once.</summary>
        public Opinion Read(Bitmap crop, string key)
        {
            ReadResult rr = _reader.Read(crop);
            RepairResult rp = _lex.Repair(key, rr);
            return new Opinion
            {
                Raw = rr.Text ?? "",
                Text = rp.Text ?? "",
                IsMember = _lex.IsMember(key, rp.Text)
            };
        }

        /// <summary>
        /// The one rule that decides whether the own engine's reading replaces the value the app
        /// already has. See the class comment. Blank values are never replaced (a blank was left
        /// blank for a reason: no consensus, or a seal over the box).
        /// </summary>
        public bool ShouldReplace(string key, string current, Opinion own)
        {
            if (own == null || !own.IsMember) return false;
            if (!_lex.IsClosedListField(key)) return false;
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (string.Equals(current.Trim(), own.Text.Trim(), StringComparison.OrdinalIgnoreCase)) return false;

            // The current value already IS a list entry (compared without case, accents or marks):
            // leave it alone, even when the own engine names a different entry. Tesseract named a
            // real entry, so the own engine may not overrule it. This also keeps the office's own
            // spelling "Penablanca" with its n-tilde: the list file holds the plain spelling and
            // would otherwise "correct" the accented one away.
            if (_lex.IsMember(key, current)) return false;
            return true;
        }
    }
}

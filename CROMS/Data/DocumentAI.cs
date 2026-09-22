using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>The civil-registry document classes CROMS can recognise.</summary>
    public enum DocKind { Unknown, Birth, Marriage, Death }

    /// <summary>
    /// One extracted field, with everything the operator and the audit trail need: what
    /// OCR actually read, what it became after correction, how sure the engine is, and
    /// what rule it broke if any. <see cref="OcrValue"/> is never overwritten - the audit
    /// log stores it beside the final value, so a correction can always be traced.
    /// </summary>
    public class DocField
    {
        public string Key;
        public string Label;
        public string Value;
        public bool Uncertain;   // anything not clean → highlight for the operator

        /// <summary>Exactly what OCR produced, before any context correction.</summary>
        public string OcrValue = "";
        /// <summary>0-100, from the confidence of the words that produced THIS value.</summary>
        public int Confidence;
        /// <summary>A context correction changed the read value (it never creates one).</summary>
        public bool Corrected;
        /// <summary>Why the field is flagged, in words the operator can act on.</summary>
        public string Issue = "";
        /// <summary>The operator typed this value in the review grid.</summary>
        public bool EditedByUser;
        /// <summary>The verdict shown in the review grid.</summary>
        public FieldStatus Status = FieldStatus.Ok;
        /// <summary>Where the value sits on the prepared page, for the scan highlight.</summary>
        public Rectangle Region = Rectangle.Empty;
        /// <summary>The record cannot be saved without this field.</summary>
        public bool Required;
        /// <summary>True when the value was read from its own region of a known form layout.</summary>
        public bool FromRegion;
        /// <summary>
        /// Confidence earned by the REGION read (agreement between renderings). Kept
        /// separately because the page-text scorer cannot see it: a value the whole-page
        /// pass never produced — which is most of them on a bordered table — would
        /// otherwise be scored as if it had been invented.
        /// </summary>
        public int RegionConfidence;
        /// <summary>The region it was read from, in 0-1 page coordinates.</summary>
        public RectangleF RegionNorm = RectangleF.Empty;

        public DocField(string key, string label, string value, bool uncertain)
        { Key = key; Label = label; Value = value; Uncertain = uncertain; }
    }

    /// <summary>Result of analysing one document.</summary>
    public class DocAiResult
    {
        public DocKind Kind = DocKind.Unknown;
        public int ClassifyConfidence;   // 0-100, how sure of the document type
        public int OcrConfidence;        // 0-100, Tesseract mean confidence ("Recognition")
        public string RawText = "";
        public List<DocField> Fields = new List<DocField>();
        public string Error;

        /// <summary>Mean per-field confidence over the fields that were actually read.</summary>
        public int OverallConfidence;
        /// <summary>True when this document must not be auto-filled without a human deciding.</summary>
        public bool NeedsManualReview;
        /// <summary>Why it needs manual review, in one line.</summary>
        public string ReviewReason = "";
        /// <summary>Degrees the page had to be turned to read it (0/90/180/270).</summary>
        public int RotationApplied;
        /// <summary>Size of the prepared page the field regions are measured in.</summary>
        public int PageWidth, PageHeight;
        /// <summary>The form revision the fields were read from, e.g. "MF-102 (2007)".</summary>
        public string LayoutCode;
        /// <summary>How that form's template was calibrated onto this photo.</summary>
        public string FitNote;
        /// <summary>
        /// Set when a form layout was recognised by the page's words but its coordinates
        /// did not land on this page, so it was refused and the printed labels were read
        /// instead. Names the layout and why — this is the operator's explanation for why
        /// a certificate they recognise was not read box by box.
        /// </summary>
        public string LayoutRejected;
        /// <summary>The closest known layout named by the page text, even when its boxes did not align.</summary>
        public string CandidateLayoutCode;

        /// <summary>0-100 estimate of whether the photographed page is clear enough to encode safely.</summary>
        public int ScanQualityScore;
        /// <summary>GOOD, USABLE — REVIEW, or RESCAN REQUIRED.</summary>
        public string ScanQualityGrade = "NOT CHECKED";
        /// <summary>Short operator-facing explanation of the scan-quality result.</summary>
        public string ScanQualityNote = "";
        /// <summary>True when image quality itself is too poor for safe automatic routing.</summary>
        public bool RescanRecommended;
        /// <summary>
        /// True when the ordinary pipeline found nothing usable and a perspective-corrected
        /// re-read (see <see cref="DocumentDewarp"/>) is what actually produced this result —
        /// so a photographed, not-square-to-the-camera page still yielded values instead of
        /// falling through to the refused-template blanks.
        /// </summary>
        public bool PerspectiveCorrected;
        /// <summary>True when the certificate kind is known but its printed layout is not in CROMS.</summary>
        public bool PossibleNewForm => Kind != DocKind.Unknown && LayoutCode == null
            && LayoutRejected != null && !RescanRecommended;

        /// <summary>Required fields still empty — these block saving or routing.</summary>
        public List<string> MissingRequired =>
            Fields.Where(f => f.Required && string.IsNullOrWhiteSpace(f.Value))
                  .Select(f => f.Label).ToList();

        public int ExtractedCount => Fields.Count(f => !string.IsNullOrWhiteSpace(f.Value));
        public int MissingCount => Fields.Count(f => string.IsNullOrWhiteSpace(f.Value));

        /// <summary>Extracted values keyed by canonical key, for auto-fill.</summary>
        public Dictionary<string, string> Map()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Fields)
                if (!string.IsNullOrWhiteSpace(f.Value)) d[f.Key] = f.Value.Trim();
            return d;
        }
    }

    /// <summary>
    /// Offline document-recognition engine: Tesseract OCR + rule/label-based
    /// classification and field extraction (no cloud service — civil-registry PII
    /// never leaves the machine). Classification identifies the document by the
    /// labels/headings it contains (not fixed coordinates), so it tolerates PSA form
    /// revisions and slight layout changes. Extraction reads a field's LABEL and pulls
    /// the value next to / below it. New document types are added by registering a
    /// <see cref="DocProfile"/> — the engine is deliberately modular so CENOMAR, court
    /// orders, etc. can be added later without touching the pipeline.
    /// </summary>
    public static class DocumentAI
    {
        public static bool IsAvailable() => OcrService.IsAvailable();

        // ---- classification profiles (extensible) --------------------------
        private class DocProfile
        {
            public DocKind Kind;
            public string[] StrongMarkers;   // near-definitive phrases
            public string[] WeakMarkers;     // supporting phrases
        }

        private static readonly List<DocProfile> Profiles = new List<DocProfile>
        {
            new DocProfile {
                Kind = DocKind.Birth,
                StrongMarkers = new[] { "certificate of live birth", "live birth", "municipal form no. 102", "form no. 102" },
                WeakMarkers   = new[] { "birth registration", "child", "date of birth", "place of birth", "maiden" }
            },
            new DocProfile {
                Kind = DocKind.Marriage,
                StrongMarkers = new[] { "certificate of marriage", "municipal form no. 97", "form no. 97" },
                WeakMarkers   = new[] { "marriage registration", "husband", "wife", "solemnizing", "date of marriage" }
            },
            new DocProfile {
                Kind = DocKind.Death,
                StrongMarkers = new[] { "certificate of death", "municipal form no. 103", "form no. 103" },
                WeakMarkers   = new[] { "death registration", "deceased", "cause of death", "date of death" }
            },
        };

        /// <summary>Load a supported image (JPG/JPEG/PNG). PDF is not yet supported.</summary>
        public static Bitmap LoadImage(string path)
        {
            string ext = (Path.GetExtension(path) ?? "").ToLowerInvariant();
            if (ext == ".pdf")
                throw new NotSupportedException(
                    "PDF upload is not supported yet. Please upload an image (JPG, JPEG or PNG) " +
                    "of the document — e.g. a photo or scan saved as an image.");
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp" && ext != ".tif" && ext != ".tiff")
                throw new NotSupportedException("Unsupported file type. Upload a JPG, JPEG or PNG image.");

            using (var tmp = new Bitmap(path))
                return CapSize(tmp);
        }

        /// <summary>
        /// Load an image already held in memory (a phone-uploaded scan pulled out of
        /// <c>ocr_batch.source_image</c>) — same size cap as <see cref="LoadImage"/>, so a
        /// mobile-submitted page is read exactly like a locally loaded file once it reaches
        /// this engine.
        /// </summary>
        public static Bitmap LoadImageBytes(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var tmp = new Bitmap(ms))
                return CapSize(tmp);
        }

        /// <summary>
        /// Cap very large scans so OCR preprocessing stays responsive. Kept high on
        /// purpose: PSA forms are dense and their print is small, so shrinking a 4000px
        /// scan to 2200 throws away strokes OCR cannot get back. The preprocessing pass
        /// is O(pixels) and no longer upscales, so a full-size page is affordable.
        /// </summary>
        private static Bitmap CapSize(Bitmap tmp)
        {
            const int max = 4200;
            if (tmp.Width <= max && tmp.Height <= max) return new Bitmap(tmp);
            double s = Math.Min((double)max / tmp.Width, (double)max / tmp.Height);
            var outBmp = new Bitmap((int)(tmp.Width * s), (int)(tmp.Height * s));
            using (var g = Graphics.FromImage(outBmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(tmp, 0, 0, outBmp.Width, outBmp.Height);
            }
            return outBmp;
        }

        /// <summary>
        /// The page size that reads best on most scans. Measured, not guessed: a real
        /// Certificate of Live Birth loses its date of birth above this, and a small
        /// 755px Certificate of Death is unreadable until it is scaled UP to it.
        /// </summary>
        private const int PreferredLongSide = 2400;

        /// <summary>
        /// OCR the image, classify it, and extract the fields for its type.
        /// <para/>
        /// Runs the page at TWO resolutions and keeps whichever produced more fields.
        /// There is no single best scale — on the office's own samples the birth
        /// certificate reads best at 2400px while the marriage certificate needs full
        /// size, and picking one loses real fields on the other. Two passes still
        /// finish faster than the single pass this replaced, because the old
        /// GetPixel/SetPixel preprocessing cost more than an extra recognition does.
        /// </summary>
        public static DocAiResult Analyze(Bitmap image)
        {
            if (!IsAvailable())
                return new DocAiResult { Error = "OCR engine (Tesseract 'eng' language data) not found." };

            // A sideways page is not a slightly worse read, it is no read at all -
            // Tesseract's layout analysis assumes horizontal lines. Probe the four right
            // angles once on a small copy, then work from the straightened page.
            int rotation = 0;
            Bitmap upright = image;
            try
            {
                rotation = OcrService.DetectRotation(image);
                if (rotation != 0) upright = OcrService.Rotate(image, rotation);
            }
            catch { rotation = 0; upright = image; }

            Bitmap straightened = null;
            try
            {
                DocAiResult best = AnalyzeAt(upright, PreferredLongSide);

                // A second pass at native size only helps when the page had to be read as
                // free text. Once a form LAYOUT is recognised the values already come from
                // full-resolution crops of the original bitmap, so re-reading the page
                // larger buys nothing and doubles the time.
                int longest = Math.Max(upright.Width, upright.Height);
                if (best.LayoutCode == null && longest > PreferredLongSide * 1.15)
                {
                    DocAiResult native = AnalyzeAt(upright, longest);
                    if (Score(native) > Score(best)) best = native;
                }

                Bitmap qualityImage = upright;

                // The ordinary pipeline refused the template outright — no scale+offset
                // placed the anchors, which is exactly what a photograph taken at an angle
                // does (the marriage-certificate sample that never got past this: 2026-09-06).
                // Try once more on a perspective-corrected copy of the page. This can only
                // help: it runs only when the normal attempt already produced nothing to
                // lose, and the two results are compared by the same Score() used for the
                // two-resolution pass above, so a bad correction just loses the comparison.
                if (best.LayoutRejected != null)
                {
                    try
                    {
                        straightened = DocumentDewarp.TryStraighten(upright);
                        if (straightened != null)
                        {
                            DocAiResult warped = AnalyzeAt(straightened, PreferredLongSide);
                            if (Score(warped) > Score(best))
                            {
                                warped.PerspectiveCorrected = true;
                                warped.FitNote = "(read from a perspective-corrected copy — the photo was not "
                                    + "square to the page) " + (warped.FitNote ?? "");
                                best = warped;
                                qualityImage = straightened;
                            }
                        }
                    }
                    catch { /* the correction is a bonus attempt; never let it break the real result */ }
                }

                best.RotationApplied = rotation;
                AssessScanQuality(qualityImage, best);
                return best;
            }
            finally
            {
                if (!ReferenceEquals(upright, image)) upright.Dispose();
                if (straightened != null) straightened.Dispose();
            }
        }

        /// <summary>How much a pass actually recovered — fields first, confidence to break ties.</summary>
        private static int Score(DocAiResult r)
        {
            if (r == null || !string.IsNullOrEmpty(r.Error)) return -1;
            // Fields first, then how sure we are of them: a pass that reads one more field
            // but reads every field badly is not the better pass, so validated per-field
            // confidence breaks the tie rather than raw page confidence.
            int broken = r.Fields.Count(f => f.Status == FieldStatus.Invalid);
            return (r.ExtractedCount - broken) * 1000 + r.OverallConfidence;
        }

        /// <summary>
        /// Judge the IMAGE separately from the form and extracted values.  A different
        /// revision can be a perfectly good scan, while a supported form can still be too
        /// dark or blurred to file safely.  The score combines resolution, tonal range,
        /// edge clarity and OCR legibility; it never guesses the document type.
        /// </summary>
        private static void AssessScanQuality(Bitmap image, DocAiResult result)
        {
            if (image == null || result == null) return;

            int longest = Math.Max(image.Width, image.Height);
            int step = Math.Max(2, longest / 550);
            double sum = 0, sum2 = 0, edge = 0;
            int count = 0;

            for (int y = step; y < image.Height - step; y += step)
            for (int x = step; x < image.Width - step; x += step)
            {
                Color p = image.GetPixel(x, y);
                Color px = image.GetPixel(x + 1, y);
                Color py = image.GetPixel(x, y + 1);
                double l = 0.299 * p.R + 0.587 * p.G + 0.114 * p.B;
                double lx = 0.299 * px.R + 0.587 * px.G + 0.114 * px.B;
                double ly = 0.299 * py.R + 0.587 * py.G + 0.114 * py.B;
                sum += l;
                sum2 += l * l;
                edge += Math.Abs(l - lx) + Math.Abs(l - ly);
                count++;
            }

            if (count == 0) return;
            double mean = sum / count;
            double contrast = Math.Sqrt(Math.Max(0, sum2 / count - mean * mean));
            double clarity = edge / (count * 2.0);

            int resolution = longest >= 2000 ? 100 : longest >= 1500 ? 82
                           : longest >= 1100 ? 62 : 35;
            int tonal = Clamp((int)Math.Round((contrast - 18.0) * 2.2));
            int sharp = Clamp((int)Math.Round((clarity - 2.0) * 8.5));
            int exposure = mean < 55 ? Clamp((int)(mean / 55.0 * 100))
                         : mean > 242 ? Clamp((int)((255.0 - mean) / 13.0 * 100)) : 100;
            int legibility = Clamp(result.OcrConfidence);

            int score = Clamp((int)Math.Round(
                resolution * 0.18 + tonal * 0.18 + sharp * 0.18
                + exposure * 0.16 + legibility * 0.30));
            result.ScanQualityScore = score;

            if (score >= 76)
            {
                result.ScanQualityGrade = "GOOD";
                result.ScanQualityNote = "Clear enough for OCR; still verify civil-registry values before saving.";
            }
            else if (score >= 55)
            {
                result.ScanQualityGrade = "USABLE — REVIEW";
                result.ScanQualityNote = "Readable, but blur, lighting or contrast may cause mistakes; verify every flagged field.";
            }
            else
            {
                result.ScanQualityGrade = "RESCAN REQUIRED";
                result.ScanQualityNote = "Too unclear for safe automatic filing. Flatten the page, fill the frame and use even light without shadows.";
                result.RescanRecommended = true;
                result.NeedsManualReview = true;
                string qualityReason = "scan quality " + score + "% is too low for safe filing";
                result.ReviewReason = string.IsNullOrWhiteSpace(result.ReviewReason)
                    ? qualityReason : qualityReason + "; " + result.ReviewReason;
            }
        }

        private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

        private static DocAiResult AnalyzeAt(Bitmap image, int longSide)
        {
            var result = new DocAiResult();
            using (var session = new OcrSession(image, longSide))
            {
                OcrResult ocr = session.Page();
                result.RawText = ocr.Text ?? "";
                result.OcrConfidence = ocr.Confidence;
                result.PageWidth = ocr.PageWidth;
                result.PageHeight = ocr.PageHeight;

                // The sparse pass finds printed text the layout analyser skips on a
                // bordered table. It is unusable as a reading ORDER, so it only ever adds
                // evidence for deciding WHICH form this is.
                string evidence = result.RawText + "\n" + (session.PageSparse().Text ?? "");

                int layoutConfidence;
                FormLayout layout = DocLayouts.Detect(evidence, out layoutConfidence);
                result.CandidateLayoutCode = layout == null ? null : layout.Code;

                // Detect names the KIND of certificate from the page's words; it cannot
                // tell one revision of that form from another, because every revision
                // prints the same words. Fit the template first and let the page say
                // whether the coordinates land on it. If they do not, this is a form the
                // library does not have — read it by its labels instead of forcing it
                // through the wrong boxes.
                PageFit fit = null;
                if (layout != null)
                {
                    fit = PageFit.From(layout, ocr);
                    fit.RefineToRulings(layout, session.Rulings());
                    if (!fit.Trustworthy)
                    {
                        result.LayoutRejected = string.Format(CultureInfo.InvariantCulture,
                            "{0} does not fit this page ({1} anchor(s), {2} agreeing, {3} ruling(s)) — "
                            + "read by printed labels instead",
                            layout.Code, fit.AnchorsMatched, fit.SquareInliers, fit.RulingsMatched);
                        layout = null;
                        fit = null;
                    }
                }

                if (layout != null)
                {
                    // A known form: read every value from its OWN region. Whole-page text
                    // places the template, it does not supply values — Tesseract drops
                    // entire typewriter rows inside these bordered tables.
                    result.Kind = layout.Kind;
                    result.LayoutCode = layout.Code;
                    result.ClassifyConfidence = layoutConfidence;

                    result.FitNote = string.Format(CultureInfo.InvariantCulture,
                        "{0} anchor(s) ({5} agreeing), {1} ruling(s), scale {2:0.000}x{3:0.000}, offset {4:+0.000;-0.000}/{6:+0.000;-0.000}",
                        fit.AnchorsMatched, fit.RulingsMatched, fit.ScaleX, fit.ScaleY,
                        fit.OffsetX, fit.SquareInliers, fit.OffsetY);

                    List<FieldRead> reads = RegionReader.Read(session, layout, fit, ocr);
                    ReconcileFamilyNames(layout, reads);
                    DeriveFields(layout, reads);
                    result.Fields = reads.Select(ToDocField).ToList();
                    MergePageText(result, ocr, layout);
                }
                else
                {
                    // No layout, or one that did not fit: read the page by its printed
                    // labels, so an unusual form — or a revision the library does not
                    // carry — still yields something rather than a page of wrong boxes.
                    Classify(result.RawText, ocr, result);
                    if (result.LayoutRejected != null)
                        result.FitNote = result.LayoutRejected;
                    if (result.Kind == DocKind.Birth)
                        result.Fields = ExtractBirth(result.RawText, ocr.Confidence);
                    else if (result.Kind == DocKind.Marriage)
                        result.Fields = ExtractMarriage(ocr);
                    else if (result.Kind == DocKind.Death)
                        result.Fields = ExtractDeath(result.RawText, ocr.Confidence);
                }

                // BR-20. Strip the scanner's noise BEFORE anything downstream sees it: the
                // ruled lines, tick marks and speckle that come back as "|", "~", "___",
                // "»" and stray box-drawing characters. Done here, at the one point both
                // the region path and the label path have converged, so neither can leak
                // them into a form field, a stored value or a printed certificate.
                foreach (DocField field in result.Fields)
                    if (field != null) field.Value = Sanitize(field.Value);

                // Score, correct and validate every field before anyone sees it.
                DocIntelligence.Enrich(result, ocr);
            }
            return result;
        }

        /// <summary>
        /// Fill fields the form's own boxes could not read, from the whole-page label pass.
        /// <para/>
        /// A layout is a set of coordinates calibrated on ONE revision of a form. The
        /// office does not only receive that revision: a certificate re-issued years apart
        /// moves its rows, renumbers its items, or drops a row entirely, and a PSA-issued
        /// transcription of the same record is laid out differently again. When the
        /// template lands off a row, that field comes back BLANK — the region is read, it
        /// simply holds nothing the field will accept. The printed LABELS, though, are
        /// still on the page wherever the rows moved to, which is exactly what the
        /// label-anchored extractors read.
        /// <para/>
        /// So the two are used for what each is good at: the template supplies values
        /// where it fits, and the labels answer for whatever it missed. The region read
        /// always wins where it produced something — it is read from the cell the value
        /// actually lives in, and this pass has no such certainty.
        /// <para/>
        /// A page value is taken only if it is the SHAPE the field allows
        /// (<see cref="RegionReader.Vet"/>). Filling a blank with a plausible-looking
        /// wrong value is worse than leaving it blank: a blank asks the operator to type
        /// it, a wrong value has to be spotted first. Everything filled here is marked as
        /// not-from-its-own-region, so the grid flags it and the operator is told where it
        /// came from.
        /// </summary>
        private static void MergePageText(DocAiResult result, OcrResult ocr, FormLayout layout)
        {
            List<DocField> page;
            if (layout.Kind == DocKind.Birth) page = ExtractBirth(result.RawText, ocr.Confidence);
            else if (layout.Kind == DocKind.Marriage) page = ExtractMarriage(ocr);
            else if (layout.Kind == DocKind.Death) page = ExtractDeath(result.RawText, ocr.Confidence);
            else return;

            foreach (DocField p in page)
            {
                if (p == null || string.IsNullOrWhiteSpace(p.Value)) continue;

                DocField have = result.Fields.FirstOrDefault(
                    f => string.Equals(f.Key, p.Key, StringComparison.OrdinalIgnoreCase));
                if (have != null && !string.IsNullOrWhiteSpace(have.Value)) continue;

                int score;
                string vetted = RegionReader.Vet(layout, p.Key, p.Value, out score);
                // A key this layout does not define has no shape to be checked against —
                // it is a field the template simply does not know about, which is the
                // case this fallback exists for. Take it as read rather than drop a value
                // the page plainly carries.
                if (RegionReader.Declares(layout, p.Key) && vetted == null) continue;
                string value = (vetted ?? p.Value).Trim();
                if (value.Length == 0) continue;

                if (have == null)
                {
                    have = new DocField(p.Key, p.Label, value, true);
                    result.Fields.Add(have);
                }
                else have.Value = value;

                have.OcrValue = p.Value;
                have.Uncertain = true;
                have.FromRegion = false;
                // Leave the score to Enrich: with no region confidence it grades the value
                // by the page words that produced it, which is the right measure here.
                have.RegionConfidence = 0;
                have.Issue = "Read from the form's printed labels, not from its own box — "
                           + "this scan's layout did not match the template there";
            }
        }

        /// <summary>
        /// Remove the characters a scanner invents and a certificate never contains.
        /// <para/>
        /// What survives is deliberately narrow: letters (including the enye and the
        /// accented vowels these names carry), digits, and the punctuation a civil-registry
        /// value legitimately uses - comma, full stop, hyphen, slash, apostrophe, brackets,
        /// colon for a time, hash and ampersand for an address. Everything else is a ruled
        /// line, a tick box, a fold or speckle read as a glyph.
        /// <para/>
        /// Runs are collapsed rather than kept, because the dotted fill-in line on these
        /// forms reads as "____" or "......" and a value is never written that way; a token
        /// with no letter and no digit is dropped entirely, since it cannot be part of an
        /// answer. If nothing is left the field comes back EMPTY - which asks the operator
        /// to type it, where a value of "|" or "-" looks like something was read.
        /// </summary>
        internal static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var kept = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || c == ' ') { kept.Append(c); continue; }
                kept.Append(SafePunctuation.IndexOf(c) >= 0 ? c : ' ');
            }

            // A dotted fill-in line reads as "____" or "......"; no value is written that
            // way, so a run of one punctuation mark collapses to a single character.
            string s = kept.ToString();
            foreach (char p in SafePunctuation)
                s = Regex.Replace(s, Regex.Escape(p.ToString()) + "{2,}", p.ToString());
            s = Regex.Replace(s, @"\s{2,}", " ").Trim();

            // A token with no letter and no digit cannot be part of an answer.
            var words = s.Split(' ').Where(w => w.Any(char.IsLetterOrDigit)).ToArray();
            s = string.Join(" ", words).Trim((" " + SafePunctuation).ToCharArray());
            return s.Any(char.IsLetterOrDigit) ? s : "";
        }

        /// <summary>The punctuation a civil-registry value legitimately uses. Everything
        /// else a scan produces is a ruled line, a tick box, a fold or speckle.</summary>
        private const string SafePunctuation = ",.-/':()#&";


        private static DocField ToDocField(FieldRead r)
        {
            bool empty = string.IsNullOrWhiteSpace(r.Value);
            return new DocField(r.Key, r.Label, r.Value, empty || r.Issue != null)
            {
                OcrValue = r.OcrValue ?? "",
                Corrected = r.Repaired,
                Confidence = r.Confidence,
                RegionConfidence = r.Confidence,
                RegionNorm = r.Region,
                FromRegion = r.FromRegion,
                Required = r.Required,
                Issue = r.Issue ?? ""
            };
        }

        /// <summary>
        /// Fields the registry needs that the form does not print in one box: the date of
        /// birth spread over three columns, the place of birth over three, the deceased's
        /// full name. Derived only from values already read — never invented, and never
        /// more confident than the parts they came from.
        /// </summary>
        private static void DeriveFields(FormLayout layout, List<FieldRead> reads)
        {
            Func<string, FieldRead> get = key =>
                reads.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

            Action<string, string, string, FieldRead[]> put = (key, label, value, sources) =>
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                var src = sources.Where(x => x != null).ToList();
                FieldRead existing = get(key);
                if (existing != null && !string.IsNullOrWhiteSpace(existing.Value)) return;
                FieldRead target = existing ?? new FieldRead { Key = key, Label = label };
                if (existing == null) reads.Add(target);
                target.Value = value;
                target.FromRegion = false;
                target.Confidence = src.Count == 0 ? 0 : src.Min(x => x.Confidence);
            };

            if (layout.Kind == DocKind.Birth)
            {
                FieldRead day = get("DobDay"), month = get("DobMonth"), year = get("DobYear");
                if (day != null && month != null && year != null)
                    put("DateOfBirth", "Date of Birth",
                        ComposeDate(day.Value, month.Value, year.Value), new[] { day, month, year });

                FieldRead hosp = get("PlaceHospital"), city = get("PlaceMunicipality"), prov = get("PlaceProvince");
                put("PlaceOfBirth", "Place of Birth",
                    JoinParts(", ", hosp, city, prov), new[] { hosp, city, prov });

                FieldRead mrel = get("MotherReligion"), frel = get("FatherReligion");
                put("Religion", "Religion (parents)", FirstValue(mrel, frel), new[] { mrel, frel });
                FieldRead mcit = get("MotherCitizenship"), fcit = get("FatherCitizenship");
                put("Nationality", "Citizenship (parents)", FirstValue(mcit, fcit), new[] { mcit, fcit });
            }
            else if (layout.Kind == DocKind.Death)
            {
                FieldRead a = get("DeceasedFirst"), b = get("DeceasedMiddle"), c = get("DeceasedLast");
                put("FullName", "Full Name", JoinParts(" ", a, b, c), new[] { a, b, c });
            }
            else if (layout.Kind == DocKind.Marriage)
            {
                FieldRead hc = get("HusbandCitizenship"), wc = get("WifeCitizenship");
                put("Nationality", "Citizenship", FirstValue(hc, wc), new[] { hc, wc });
            }
        }

        /// <summary>
        /// Use the certificate's own structure to repair a surname that was read twice.
        /// <para/>
        /// On Municipal Form 102 two pairs of cells are not merely likely to match, they are
        /// the SAME NAME by law: the child's last name is the father's last name, and the
        /// child's middle name is the mother's maiden surname. So when the two cells come
        /// back one or two characters apart, that is one name the scanner read twice with
        /// different luck — "Talosig" at 82% beside "Talosige" at 51% — and the better read
        /// can repair the worse one.
        /// <para/>
        /// This is NOT the office-wide name gazetteer that was rejected earlier for turning
        /// a father into his son. Nothing is imported from other records: both readings come
        /// off the page in front of us, the repair only runs when they already almost agree,
        /// and a genuine difference (a child registered under the mother's surname, which is
        /// a real and lawful case) is far more than two edits away and is left alone for the
        /// existing "surname matches neither parent" check to flag.
        /// </summary>
        private static void ReconcileFamilyNames(FormLayout layout, List<FieldRead> reads)
        {
            if (layout.Kind != DocKind.Birth) return;

            Reconcile(reads, "ChildLast", "FatherLast", "the father's surname");
            Reconcile(reads, "ChildMiddle", "MotherLast", "the mother's maiden surname");
        }

        private static void Reconcile(List<FieldRead> reads, string keyA, string keyB, string relation)
        {
            FieldRead a = reads.FirstOrDefault(r => string.Equals(r.Key, keyA, StringComparison.OrdinalIgnoreCase));
            FieldRead b = reads.FirstOrDefault(r => string.Equals(r.Key, keyB, StringComparison.OrdinalIgnoreCase));
            if (a == null || b == null) return;
            if (string.IsNullOrWhiteSpace(a.Value) || string.IsNullOrWhiteSpace(b.Value)) return;

            string na = a.Value.Trim(), nb = b.Value.Trim();
            if (string.Equals(na, nb, StringComparison.OrdinalIgnoreCase)) return;

            int distance = RegionReader.Distance(na.ToLowerInvariant(), nb.ToLowerInvariant());
            if (distance > 2) return;          // two different names, not one name read twice

            FieldRead better = a.Confidence >= b.Confidence ? a : b;
            FieldRead worse = ReferenceEquals(better, a) ? b : a;
            if (better.Confidence - worse.Confidence < 5) return;   // no clear winner, leave both

            string was = worse.Value;
            worse.Value = better.Value;
            worse.Repaired = true;
            // Agreeing with the other cell is real evidence, but it is still the weaker read
            // being overwritten, so it never inherits the stronger cell's confidence.
            worse.Confidence = Math.Max(worse.Confidence, Math.Min(better.Confidence, 80) - 5);
            string note = "Read as \"" + was + "\" — set to match " + relation +
                          " read elsewhere on this certificate; confirm against the scan";
            worse.Issue = string.IsNullOrEmpty(worse.Issue) ? note : worse.Issue + "; " + note;
        }

        private static string FirstValue(params FieldRead[] fields)
        {
            foreach (FieldRead f in fields)
                if (f != null && !string.IsNullOrWhiteSpace(f.Value)) return f.Value;
            return "";
        }

        private static string JoinParts(string separator, params FieldRead[] fields)
        {
            var kept = fields.Where(f => f != null && !string.IsNullOrWhiteSpace(f.Value))
                             .Select(f => f.Value.Trim()).ToList();
            return kept.Count == 0 ? "" : string.Join(separator, kept);
        }

        /// <summary>Build a date from three separately-read columns, or "" if they do not make one.</summary>
        private static string ComposeDate(string day, string month, string year)
        {
            int d, y;
            if (!int.TryParse((day ?? "").Trim(), out d) || !int.TryParse((year ?? "").Trim(), out y)) return "";
            if (d < 1 || d > 31 || y < 1900 || y > DateTime.Today.Year) return "";

            string[] months =
            {
                "January", "February", "March", "April", "May", "June",
                "July", "August", "September", "October", "November", "December"
            };
            string word = Regex.Replace(month ?? "", @"[^A-Za-z]", "");
            if (word.Length < 3) return "";

            string best = null; int bestDistance = int.MaxValue;
            foreach (string m in months)
            {
                int distance = Math.Min(
                    RegionReader.Distance(word.ToLowerInvariant(), m.ToLowerInvariant()),
                    RegionReader.Distance(word.ToLowerInvariant(), m.Substring(0, 3).ToLowerInvariant()));
                if (distance < bestDistance) { bestDistance = distance; best = m; }
            }
            if (best == null || bestDistance > (word.Length <= 4 ? 1 : 2)) return "";

            DateTime parsed;
            return DateTime.TryParseExact(d + " " + best + " " + y, "d MMMM yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                ? parsed.ToString("yyyy-MM-dd") : "";
        }

        /// <summary>
        /// Identify the document from what it SAYS and how it is LAID OUT - never from the
        /// file name, and never from a symbol. Three kinds of evidence are added up:
        /// strong markers (the form's own title or Municipal Form number), weak markers
        /// (field labels that belong to that certificate), and layout (structures only one
        /// form has, such as the side-by-side HUSBAND | WIFE columns of Municipal Form 97).
        /// <para/>
        /// Markers are also matched against the text with punctuation and spacing stripped,
        /// so "Municipal Form No. 102" still matches when OCR prints it as "Municipal Form
        /// No, 1O2". Confidence reflects the MARGIN over the runner-up as well as the raw
        /// score: a page that looks a little like all three is not a confident anything.
        /// </summary>
        private static void Classify(string rawText, OcrResult ocr, DocAiResult result)
        {
            string lower = (rawText ?? "").ToLowerInvariant();
            string squashed = Regex.Replace(lower, @"[^a-z0-9]", "");

            DocProfile best = null; int bestScore = 0, secondScore = 0;
            foreach (var p in Profiles)
            {
                int score = 0;
                foreach (var m in p.StrongMarkers)
                    if (lower.Contains(m) || squashed.Contains(Regex.Replace(m, @"[^a-z0-9]", "")))
                        score += 5;
                foreach (var m in p.WeakMarkers)
                    if (lower.Contains(m)) score += 1;
                score += LayoutEvidence(p.Kind, ocr, lower);

                if (score > bestScore) { secondScore = bestScore; bestScore = score; best = p; }
                else if (score > secondScore) secondScore = score;
            }

            if (best == null || bestScore < 3)
            {
                // Too little evidence to name it. Unknown is a real answer here: it sends
                // the scan to manual review instead of into the wrong registry.
                result.Kind = DocKind.Unknown;
                result.ClassifyConfidence = 0;
                return;
            }

            result.Kind = best.Kind;
            int margin = bestScore - secondScore;
            int confidence = 45 + bestScore * 4 + Math.Min(20, margin * 4);
            result.ClassifyConfidence = Math.Max(50, Math.Min(99, confidence));
        }

        /// <summary>
        /// Structural evidence a form cannot fake: MF-97 puts HUSBAND and WIFE side by side
        /// on the same printed row, MF-102 has the maiden-name and birth-weight bands,
        /// MF-103 has a cause-of-death block. Worth more than a single label, because a
        /// birth certificate mentions "husband" nowhere.
        /// </summary>
        private static int LayoutEvidence(DocKind kind, OcrResult ocr, string lower)
        {
            if (kind == DocKind.Marriage)
            {
                if (ocr == null || ocr.Words == null || ocr.Words.Count == 0) return 0;
                OcrWord h = ocr.Words.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^H[UO]SBAND$", RegexOptions.IgnoreCase));
                OcrWord w2 = ocr.Words.FirstOrDefault(w => Regex.IsMatch(w.Text, @"^W[I1L]FE$", RegexOptions.IgnoreCase));
                // Side by side on one band is the two-column table; one above the other is
                // just a form that happens to mention both words.
                if (h != null && w2 != null)
                    return Math.Abs(h.CenterY - w2.CenterY) < Math.Max(20, h.Height * 2) ? 4 : 2;
                return 0;
            }

            if (kind == DocKind.Birth)
            {
                int bands = 0;
                if (Regex.IsMatch(lower, @"\bmaiden\b")) bands++;
                if (Regex.IsMatch(lower, @"type\s+of\s+birth|weight\s+at\s+birth|\bgram")) bands++;
                if (Regex.IsMatch(lower, @"multiple\s+birth|birth\s+order")) bands++;
                return Math.Min(4, bands * 2);
            }

            if (kind == DocKind.Death)
            {
                int bands = 0;
                if (Regex.IsMatch(lower, @"cause\s+of\s+death|immediate\s+cause")) bands++;
                if (Regex.IsMatch(lower, @"burial|crematio|cemetery")) bands++;
                if (Regex.IsMatch(lower, @"attendant|autops")) bands++;
                return Math.Min(4, bands * 2);
            }

            return 0;
        }

        // ---- Birth Certificate extraction (PSA Municipal Form 102) ---------
        // The COLB is a fixed table: each numbered field's VALUE sits on the line
        // below its label, prefixed by the section letter (C/H/I/L/D, M/O/T/H/E/R,
        // F/A/T/H/E/R) that OCR reads off the vertical band. Child / Mother / Father
        // names come as three cells (First | Middle | Last). We anchor on the numbered
        // labels and section keywords, strip the stray prefixes, and split names into
        // cells — far more accurate on the real form than a flat label search.
        private static List<DocField> ExtractBirth(string text, int ocrConf)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string[] lines = text.Replace("\r", "")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            bool low = ocrConf < 90;

            // --- names via numbered section anchors ---
            // The field number is NOT a reliable anchor on a real scan: OCR read the
            // child's "1. NAME" as "4. NAME" and the father's "14. NAME" as "14, NAME",
            // and each miss silently emptied a whole name. So the numbers are tried
            // first, then the row's own "(First) (Middle) (Last)" heading is used —
            // that heading is three words wide and survives when one digit does not.
            // The printed word itself is often damaged: this scan set has "MAIDEN" read as
            // "ADEN" and "PLACE OF" as "PLAGE OF". An anchor that only matches the perfect
            // spelling takes the whole section down with it, so each one is written to
            // survive a lost or swapped letter.
            int motherIdx = FindIdx(lines, @"\bM?A[I1l]?[DO]EN\b");
            int childIdx = FindIdx(lines, @"\b1\s*[.,)]?\s*NAME");
            if (childIdx < 0 || (motherIdx >= 0 && childIdx > motherIdx))
                childIdx = NameHeadingRow(lines, 0, motherIdx >= 0 ? motherIdx : lines.Length);

            int fatherIdx = FindIdx(lines, @"\b14\s*[.,)]?\s*NAME");
            if (fatherIdx < 0 && motherIdx >= 0)
                fatherIdx = NameHeadingRow(lines, motherIdx + 1, lines.Length);

            var (cf, cm, cl) = SplitNameCells(NameAfter(lines, childIdx));
            var (mf, mm, ml) = SplitNameCells(NameAfter(lines, motherIdx));
            var (ff, fm, fl) = SplitNameCells(NameAfter(lines, fatherIdx));

            string sex = Regex.IsMatch(text, @"\bFemale\b", IC) ? "Female"
                       : Regex.IsMatch(text, @"\bMale\b", IC) ? "Male" : "";

            // Date of birth as "12 June 2018" (day month-name year).
            string dob = FindDmy(text);
            // Time of birth: "...born alive at 1:40 PM...". The anchored fallback
            // survives common OCR damage such as "alive st _3:40.Piypmpm".
            string tob = FindTimeOfBirth(text);

            // MF-102 prints this label across two OCR lines. Skip the continuation
            // "BIRTH (House No., St., Barangay)" and capture the following value row.
            string place = PlaceAfter(lines, FindIdx(lines, @"P\w{0,3}[CG]E\s+OF"),
                @"TYPE\s+OF\s+BIRTH|MULTIPLE\s+BIRTH|BIRTH\s+ORDER|WEIGHT\s+AT\s+BIRTH");
            // "2722 grams" comes back as "2722 grenis" on a real scan, so the unit is
            // matched as "a number followed by a g-word" rather than by its spelling. The
            // 300-8000 g range check downstream is what keeps a stray number out.
            if (LooksLikePrintedHint(place)) place = "";
            string weight = MatchRegex(text, @"(\d{3,5})\s*g[a-z]{2,6}\b");

            // Type of birth: read the VALUE row (the one carrying the weight, e.g.
            // "Single _ Third 2722 grams") — NOT the whole text, whose label line
            // "(Single, Twin, Triplet, etc.)" lists every option and would misfire.
            string birthType = BirthTypeIn(lines);

            string nationality = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            // Split the "Hospital, Municipality, Province" place text into its three cells,
            // so each drops into the matching Place-of-Birth combo on the form.
            string[] pp = place.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            string placeHosp = pp.Length > 0 ? pp[0] : "";
            string placeMuni = pp.Length > 1 ? pp[1] : "";
            string placeProv = pp.Length > 2 ? pp[2] : "";

            // The three cells are ruled, not punctuated, so OCR usually returns the whole row
            // as one run with no commas in it. Splitting on commas alone then left the entire
            // string sitting in the facility cell with Municipality and Province blank - and
            // Place of Birth, rebuilt from those parts, repeated the facility word for word.
            if (pp.Length == 1)
            {
                string f2, m2, p2;
                if (DocVocabulary.SplitPlace(placeHosp, out f2, out m2, out p2))
                {
                    placeHosp = f2;
                    placeMuni = m2;
                    placeProv = p2;
                }
            }
            placeHosp = RepairKnownBirthFacility(placeHosp, placeMuni, placeProv);
            place = string.Join(", ", new[] { placeHosp, placeMuni, placeProv }
                .Where(p => !string.IsNullOrWhiteSpace(p)));

            // Religion (applied to both parents) + each parent's occupation (block scan).
            string religion = ReligionOf(text);
            // Reuse the anchors resolved above — re-deriving them here with the strict
            // numbered pattern is how the father's occupation went missing when OCR
            // printed his section heading as "14, NAME".
            string motherOcc = OccupationIn(lines, motherIdx,
                fatherIdx >= 0 ? fatherIdx : lines.Length);
            string fatherOcc = OccupationIn(lines, fatherIdx, lines.Length);

            // Registry no is usually handwritten → OCR often can't read it (leave blank).
            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            string informant = SafeInformant(lines);

            var f = new List<DocField>
            {
                new DocField("RegistryNo",       "Registry Number",     regNo,          low || regNo == ""),
                new DocField("ChildFirst",       "Child First Name",    cf,             low || cf == ""),
                new DocField("ChildMiddle",       "Child Middle Name",  cm,             low || cm == ""),
                new DocField("ChildLast",        "Child Last Name",     cl,             low || cl == ""),
                new DocField("Sex",              "Sex",                 sex,            sex == ""),
                new DocField("DateOfBirth",      "Date of Birth",       dob,            low || dob == ""),
                new DocField("TimeOfBirth",      "Time of Birth",       tob,            low || tob == ""),
                new DocField("PlaceOfBirth",     "Place of Birth",      place,          low || place == ""),
                new DocField("PlaceHospital",    "Hospital / Facility", placeHosp,      low || placeHosp == ""),
                new DocField("PlaceMunicipality","Municipality",        placeMuni,      low || placeMuni == ""),
                new DocField("PlaceProvince",    "Province",            placeProv,      low || placeProv == ""),
                new DocField("TypeOfBirth",      "Type of Birth",       birthType,      birthType == ""),
                new DocField("Weight",           "Weight at Birth (g)", weight,         low || weight == ""),
                new DocField("MotherFirst",      "Mother First Name",   mf,             low || mf == ""),
                new DocField("MotherMiddle",     "Mother Middle Name",  mm,             low || mm == ""),
                new DocField("MotherLast",       "Mother Maiden Last",  ml,             low || ml == ""),
                new DocField("MotherOccupation", "Mother Occupation",   motherOcc,      low || motherOcc == ""),
                new DocField("FatherFirst",      "Father First Name",   ff,             low || ff == ""),
                new DocField("FatherMiddle",     "Father Middle Name",  fm,             low || fm == ""),
                new DocField("FatherLast",       "Father Last Name",    fl,             low || fl == ""),
                new DocField("FatherOccupation", "Father Occupation",   fatherOcc,      low || fatherOcc == ""),
                new DocField("Religion",         "Religion (parents)",  religion,       low || religion == ""),
                new DocField("Nationality",      "Nationality",         nationality,    low || nationality == ""),
                new DocField("Informant",        "Informant",           informant,      low || informant == ""),
            };
            return f;
        }

        // ---- Marriage Certificate extraction (PSA Municipal Form 97) -------
        // The COM is a TWO-COLUMN table: the husband's value and the wife's value for
        // the SAME numbered field sit side by side on ONE printed line, e.g.
        //     "1. Name of   (First) RYAN        (First) TOFIE FAE"
        //     "Contracting  (Middle) PANLILIO   (Middle) CADAVA"
        //     "Parties      (Last) MACANANG     (Last) QUILANG"
        // Flat OCR text therefore CANNOT say which spouse a value belongs to — reading
        // "the line after the HUSBAND heading" lands on the row that carries BOTH names
        // and gives the two spouses the same value. So this extractor works off word
        // POSITIONS instead: words are grouped into printed rows, and each row is cut
        // into label / husband / wife columns using the page's own HUSBAND and WIFE
        // headings as the ruler, so it survives a crop or a slightly different scan.
        private static List<DocField> ExtractMarriage(OcrResult ocr)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string text = ocr.Text ?? "";
            bool low = ocr.Confidence < 90;

            List<FormRow> rows = BuildRows(ocr);
            ColumnRuler ruler = MeasureColumns(rows, ocr.PageWidth);

            string hf = "", hm = "", hl = "", wf = "", wm = "", wl = "";
            NameRows(rows, ruler, ref hf, ref hm, ref hl, ref wf, ref wm, ref wl);

            // The date sits on its own row just ABOVE the "16. Date of Marriage" label,
            // so search around that label rather than taking the first date on the page
            // (which is usually the marriage LICENCE's issue date further down).
            string date = DateNearLabel(rows, @"D.{0,2}te\s+of\s+M|T[i1l]me\s+of\s+M");
            if (date == "") date = FindDmy(text);

            string place = PlaceOfMarriage(rows, ruler);
            string solemn = SolemnizingOfficer(rows, ruler);
            string nat = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            return new List<DocField>
            {
                new DocField("RegistryNo",      "Registry Number",      regNo,  low || regNo == ""),
                new DocField("HusbandFirst",    "Husband First Name",   hf,     low || hf == ""),
                new DocField("HusbandMiddle",   "Husband Middle Name",  hm,     low || hm == ""),
                new DocField("HusbandLast",     "Husband Last Name",    hl,     low || hl == ""),
                new DocField("WifeFirst",       "Wife First Name",      wf,     low || wf == ""),
                new DocField("WifeMiddle",      "Wife Middle Name",     wm,     low || wm == ""),
                new DocField("WifeLast",        "Wife Last Name",       wl,     low || wl == ""),
                new DocField("DateOfMarriage",  "Date of Marriage",     date,   low || date == ""),
                new DocField("PlaceOfMarriage", "Place of Marriage",    place,  low || place == ""),
                new DocField("Solemnizer",      "Solemnizing Officer",  solemn, low || solemn == ""),
                new DocField("Nationality",     "Citizenship",          nat,    low || nat == ""),
            };
        }

        // ---- Death Certificate extraction (PSA Municipal Form 103) ---------
        // Deceased name is three cells (First | Middle | Last) below the "NAME" heading.
        // Sex / civil status / age / date / place read off their labels and keywords.
        private static List<DocField> ExtractDeath(string text, int ocrConf)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            string[] lines = text.Replace("\r", "")
                .Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            bool low = ocrConf < 90;

            int ni = FindIdx(lines, @"NAME\s+OF\s+DECEASED");
            if (ni < 0) ni = FindIdx(lines, @"\b1\s*\.?\s*NAME");
            var (df, dm, dl) = SplitNameCells(NameAfter(lines, ni));
            string fullName = Clean(string.Join(" ", new[] { df, dm, dl }.Where(s => s.Length > 0)));

            string sex = Regex.IsMatch(text, @"\bFemale\b", IC) ? "Female"
                       : Regex.IsMatch(text, @"\bMale\b", IC) ? "Male" : "";

            string civil = Regex.IsMatch(text, @"\bWidow", IC) ? "Widowed"
                         : Regex.IsMatch(text, @"\bMarried\b", IC) ? "Married"
                         : Regex.IsMatch(text, @"\bSeparated\b", IC) ? "Separated"
                         : Regex.IsMatch(text, @"\bDivorced\b", IC) ? "Divorced"
                         : Regex.IsMatch(text, @"\bAnnulled\b", IC) ? "Annulled"
                         : Regex.IsMatch(text, @"\bSingle\b", IC) ? "Single" : "";

            // Anchored to the AGE box. Form 103 spells out the under-1-year brackets
            // ("b. IF UNDER 1 YEAR", "42 days to 1 year"), so an unanchored search for
            // "<number> year" happily returns one of those printed captions as the
            // deceased's age — a wrong age is worse than none.
            // \bAGE\b, not just "AGE": without the trailing boundary it also matched the
            // word "aged" in the printed maternal-condition caption
            // ("if the deceased is female aged 35 - 40 years") and reported 40.
            string age = MatchRegex(text, @"\bAGE\b[^\n]{0,80}?\b(\d{1,3})\s*(?:years|yrs?|year)\b");
            if (age == "") age = MatchRegex(text, @"\b(\d{1,3})\s*(?:years|yrs?)\s+old\b");
            string dod = FindDmy(text);
            string place = StripPrefix(LineAfter(lines, FindIdx(lines, @"P.?ACE\s+OF\s+DEATH")));
            string nat = Regex.IsMatch(text, @"Filipin[oa]", IC) ? "Filipino" : "";

            Match rm = Regex.Match(text, @"(20\d{2}|19\d{2})\s*[-–—]\s*(\d{3,5})");
            string regNo = rm.Success ? rm.Groups[1].Value + "-" + rm.Groups[2].Value : "";

            return new List<DocField>
            {
                new DocField("RegistryNo",     "Registry Number",      regNo,    low || regNo == ""),
                new DocField("DeceasedFirst",  "Deceased First Name",  df,       low || df == ""),
                new DocField("DeceasedMiddle", "Deceased Middle Name", dm,       low || dm == ""),
                new DocField("DeceasedLast",   "Deceased Last Name",   dl,       low || dl == ""),
                new DocField("FullName",       "Full Name",            fullName, low || fullName == ""),
                new DocField("Sex",            "Sex",                  sex,      sex == ""),
                new DocField("CivilStatus",    "Civil Status",         civil,    low || civil == ""),
                new DocField("Age",            "Age",                  age,      low || age == ""),
                new DocField("Citizenship",    "Citizenship",          nat,      low || nat == ""),
                new DocField("DateOfDeath",    "Date of Death",        dod,      low || dod == ""),
                new DocField("PlaceOfDeath",   "Place of Death",       place,    low || place == ""),
            };
        }

        // ---- two-column form layout (Municipal Form 97) ---------------------

        /// <summary>One printed row of a form, with its words already split by column.</summary>
        private class FormRow
        {
            public int CenterY;
            public List<OcrWord> Words = new List<OcrWord>();

            /// <summary>The whole row in reading order — for matching printed labels.</summary>
            public string Text
            {
                get { return string.Join(" ", Words.OrderBy(w => w.X).Select(w => w.Text)); }
            }

            public string Column(int fromX, int toX)
            {
                return string.Join(" ", Words
                    .Where(w => w.CenterX >= fromX && w.CenterX < toX)
                    .OrderBy(w => w.X)
                    .Select(w => w.Text));
            }
        }

        /// <summary>Where the label / husband / wife columns start and end, in page pixels.</summary>
        private class ColumnRuler
        {
            public int LabelEdge;   // labels left of this are the form's printed field names
            public int Divider;     // husband column ends here, wife column begins
            public int WifeEdge;    // right edge of the table (NOT the page edge)
            public int PageWidth;

            public string Husband(FormRow r) { return r.Column(LabelEdge, Divider); }

            // Bounded on the right on purpose: a scanned page carries speckle and
            // binding marks in the margin beyond the table, and those came through as
            // stray capitals glued onto the wife's surname.
            public string Wife(FormRow r) { return r.Column(Divider, WifeEdge); }
        }

        /// <summary>Group words into printed rows by their vertical middle.</summary>
        private static List<FormRow> BuildRows(OcrResult ocr)
        {
            var rows = new List<FormRow>();
            if (ocr == null || ocr.Words == null) return rows;

            foreach (OcrWord w in ocr.Words.OrderBy(x => x.CenterY))
            {
                FormRow last = rows.Count > 0 ? rows[rows.Count - 1] : null;
                // A word belongs to the row above it when their middles are closer than
                // about half a character height — tolerant of the slight baseline drift
                // a photographed page always has.
                int tolerance = Math.Max(8, (int)(w.Height * 0.6));
                if (last != null && Math.Abs(w.CenterY - last.CenterY) <= tolerance)
                {
                    last.Words.Add(w);
                    last.CenterY = (int)last.Words.Average(x => x.CenterY);
                }
                else
                {
                    rows.Add(new FormRow { CenterY = w.CenterY, Words = { w } });
                }
            }
            return rows;
        }

        /// <summary>
        /// Find the husband/wife divider from the form's own HUSBAND and WIFE headings.
        /// Deriving it from the page beats a fixed fraction: a crop, a margin or a
        /// phone photo taken at an angle all move the columns. Falls back to halves.
        /// </summary>
        private static ColumnRuler MeasureColumns(List<FormRow> rows, int pageWidth)
        {
            var ruler = new ColumnRuler
            {
                PageWidth = pageWidth,
                Divider = pageWidth / 2,
                LabelEdge = (int)(pageWidth * 0.12),
                WifeEdge = (int)(pageWidth * 0.95)
            };

            OcrWord husband = null, wife = null;
            foreach (FormRow r in rows)
                foreach (OcrWord w in r.Words)
                {
                    if (husband == null && Regex.IsMatch(w.Text, @"^H[UO]SBAND$", RegexOptions.IgnoreCase))
                        husband = w;
                    if (wife == null && Regex.IsMatch(w.Text, @"^W[I1L]FE$", RegexOptions.IgnoreCase))
                        wife = w;
                }

            if (husband != null && wife != null && wife.CenterX > husband.CenterX)
            {
                ruler.Divider = (husband.CenterX + wife.CenterX) / 2;
                // The husband column is as wide on its left as it is on its right, so
                // mirroring the heading gives where the printed label column ends.
                int labelEdge = 2 * husband.CenterX - ruler.Divider;
                if (labelEdge > 0 && labelEdge < ruler.Divider)
                {
                    ruler.LabelEdge = labelEdge;
                    // The wife's column is as wide as the husband's, so the table ends
                    // one column-width past the divider — anything further right is margin.
                    int wifeEdge = ruler.Divider + (ruler.Divider - labelEdge);
                    ruler.WifeEdge = Math.Min(pageWidth, wifeEdge);
                }
            }
            return ruler;
        }

        /// <summary>
        /// Read the three name rows (First / Middle / Last) that follow the
        /// HUSBAND | WIFE heading. Within a cell only ALL-CAPS words are kept: PSA
        /// forms print the names in capitals while the cell's own "(First)" / "(Middle)"
        /// / "(Last)" hint is mixed case, so this drops the hint without needing to
        /// recognise it — which matters, because OCR mangles those hints badly
        /// ("Frsty", "Jownse)", "(Las)").
        /// </summary>
        private static void NameRows(List<FormRow> rows, ColumnRuler ruler,
            ref string hf, ref string hm, ref string hl,
            ref string wf, ref string wm, ref string wl)
        {
            int header = -1;
            for (int i = 0; i < rows.Count && header < 0; i++)
            {
                string t = rows[i].Text;
                if (Regex.IsMatch(t, @"H[UO]SBAND", RegexOptions.IgnoreCase) &&
                    Regex.IsMatch(t, @"W[I1L]FE", RegexOptions.IgnoreCase))
                    header = i;
            }
            if (header < 0) return;

            var cells = new List<Tuple<string, string>>();
            for (int i = header + 1; i < rows.Count && cells.Count < 3; i++)
            {
                // Stop at the next numbered section — past it we are reading dates of
                // birth, not names.
                if (Regex.IsMatch(rows[i].Text, @"\b2\s*[ab]?\s*\.|Date\s+of\s+B|P.?ace\s+of\s+B",
                        RegexOptions.IgnoreCase))
                    break;

                string husband = CapsOnly(ruler.Husband(rows[i]));
                string wife = CapsOnly(ruler.Wife(rows[i]));
                if (husband == "" && wife == "") continue;
                cells.Add(Tuple.Create(husband, wife));
            }

            if (cells.Count > 0) { hf = cells[0].Item1; wf = cells[0].Item2; }
            if (cells.Count > 1) { hm = cells[1].Item1; wm = cells[1].Item2; }
            if (cells.Count > 2) { hl = cells[2].Item1; wl = cells[2].Item2; }
        }

        /// <summary>Keep only the ALL-CAPS words of a cell (the value, not the printed hint).</summary>
        private static string CapsOnly(string cell)
        {
            if (string.IsNullOrWhiteSpace(cell)) return "";
            var kept = new List<string>();
            foreach (string raw in cell.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = Regex.Replace(raw, @"[^A-Za-z\-']", "");
                if (t.Length < 2) continue;
                if (t.Any(char.IsLower)) continue;
                kept.Add(t);
            }
            return string.Join(" ", kept);
        }

        /// <summary>A date on, just above, or just below the row carrying a label.</summary>
        private static string DateNearLabel(List<FormRow> rows, string labelPattern)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (!Regex.IsMatch(rows[i].Text, labelPattern, RegexOptions.IgnoreCase)) continue;
                for (int j = Math.Max(0, i - 2); j <= Math.Min(rows.Count - 1, i + 2); j++)
                {
                    string d = FindDmy(rows[j].Text);
                    if (d != "") return d;
                }
            }
            return "";
        }

        /// <summary>
        /// "15. Place of Marriage" is written on the SAME row as its number, with the
        /// printed hint "(Office of the/House of/Barangay of/Church of)" on the row
        /// below — so reading the line after the label returns the hint, not the answer.
        /// Take what is written to the right of the label on the label's own row, drop
        /// the printed words of the label itself, and ignore the page margin.
        /// </summary>
        private static string PlaceOfMarriage(List<FormRow> rows, ColumnRuler ruler)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            const string printed =
                @"^(15|5|16)\.?$|P.?ace|Marr?[il]?[ae]ge|Mar[ft]ege|Off?ice|House|Bara[ng]|Chur|" +
                @"C[il]ty|Municipal|Province|Country|Day|Month|Year";

            for (int i = 0; i < rows.Count; i++)
            {
                if (!Regex.IsMatch(rows[i].Text, @"P.?ace\s+of\s+M|1\s*5\s*\.\s*P", IC)) continue;

                var kept = new List<string>();
                foreach (OcrWord w in rows[i].Words.OrderBy(w => w.X))
                {
                    if (w.CenterX < ruler.LabelEdge || w.CenterX > ruler.WifeEdge) continue;
                    string t = Clean(w.Text);
                    if (t.Length < 2 || !t.Any(char.IsLetterOrDigit)) continue;
                    if (Regex.IsMatch(t, printed, IC)) continue;
                    kept.Add(t);
                }

                // Require real words: a couple of stray marks is not a place name.
                string value = Clean(string.Join(" ", kept));
                if (kept.Count(t => t.Count(char.IsLetter) >= 3) >= 2) return value;
            }
            return "";
        }

        /// <summary>
        /// The solemnizing officer's name, written above the "(Signature Over Printed
        /// Name of Solemnizing Officer)" line and in that label's own column — the two
        /// columns to its right hold the position ("Judge") and the court, which are NOT
        /// the officer's name. Never returns the certification paragraph that begins
        /// "THIS IS TO CERTIFY: THAT BEFORE ME": a sentence sitting in a name field is
        /// worse than an empty one, because it looks filled in and gets saved as a name.
        /// </summary>
        private static string SolemnizingOfficer(List<FormRow> rows, ColumnRuler ruler)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            const string boilerplate =
                @"CERTIF|BEFORE\s+ME|consent|witness|solemniz|signature|position|designation|" +
                @"licen[sc]e|Executive\s+Order|Presidential|legal\s+age|Judge|COURT|BRANCH";

            for (int i = 0; i < rows.Count; i++)
            {
                if (!Regex.IsMatch(rows[i].Text, @"Pr[il]nted\s+Name\s+of\s+Sole", IC)) continue;

                // The name sits over the label, so use the label's own horizontal span
                // (widened a little) as the column to read.
                var labelWords = rows[i].Words
                    .Where(w => Regex.IsMatch(w.Text, @"Pr[il]nted|Name|Sole", IC)).ToList();
                if (labelWords.Count == 0) continue;
                int pad = Math.Max(40, ruler.PageWidth / 20);
                int from = labelWords.Min(w => w.X) - pad;
                int to = labelWords.Max(w => w.X + w.Width) + pad;

                for (int j = i - 1; j >= Math.Max(0, i - 3); j--)
                {
                    string band = rows[j].Column(from, to);
                    if (Regex.IsMatch(band, boilerplate, IC)) continue;
                    string caps = CapsOnly(band);
                    if (caps.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length >= 2)
                        return caps;
                }
            }
            return "";
        }

        /// <summary>Index of the first line matching the pattern (-1 if none).</summary>
        private static int FindIdx(string[] lines, string pattern)
        {
            for (int i = 0; i < lines.Length; i++)
                if (Regex.IsMatch(lines[i], pattern, RegexOptions.IgnoreCase)) return i;
            return -1;
        }

        /// <summary>
        /// The informant's name, from the "Name in Print" line. The certification block
        /// has several of those labels stacked next to each other, and when OCR loses
        /// the handwritten row the next line is another printed label — so a value that
        /// reads like a label, or that isn't at least two words, is rejected. On a real
        /// scan this field otherwise came back as "Tite oF ADMIN STRATIVE ADEM!", which
        /// would have been saved to the registry as somebody's name.
        /// </summary>
        private static string SafeInformant(string[] lines)
        {
            string candidate = StripPrefix(LineAfter(lines, FindIdx(lines, @"Name in Print")));
            if (Regex.IsMatch(candidate,
                    @"Relationship|Title|Tite|Position|Address|Date|Signature|Name\s+in\s+Print|" +
                    @"Administrat|Officer|Registrar|Prepared|Received",
                    RegexOptions.IgnoreCase))
                return "";

            int words = candidate.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(token => token.Count(char.IsLetter) >= 2);
            return words >= 2 ? candidate : "";
        }

        /// <summary>
        /// A "NAME (First) (Middle) (Last)" heading row within a line range. Matched on
        /// the bracketed cell hints rather than their spelling, because OCR mangles them
        /// ("{Fiesty (Miadte) (Last)") while the bracket pattern itself survives.
        /// </summary>
        private static int NameHeadingRow(string[] lines, int from, int to)
        {
            for (int i = Math.Max(0, from); i < Math.Min(lines.Length, to); i++)
            {
                if (!Regex.IsMatch(lines[i], @"\bNAME\b", RegexOptions.IgnoreCase)) continue;
                if (Regex.Matches(lines[i], @"[\(\{\[]").Count >= 2) return i;
            }
            return -1;
        }

        private static string LineAfter(string[] lines, int idx) =>
            idx >= 0 && idx + 1 < lines.Length ? lines[idx + 1] : "";

        /// <summary>
        /// Return the first value row after a place label. PSA forms often split the
        /// printed label over multiple OCR lines, so label-only continuations are
        /// ignored and scanning stops at the next numbered section.
        /// </summary>
        private static string PlaceAfter(string[] lines, int idx, string stopPattern)
        {
            if (idx < 0) return "";
            for (int i = idx + 1; i < lines.Length && i <= idx + 5; i++)
            {
                string line = lines[i];
                if (Regex.IsMatch(line, stopPattern, RegexOptions.IgnoreCase)) break;

                bool labelFragment = Regex.IsMatch(line,
                    @"\b(?:Name\s+of\s+Hospital|Hospital/Clinic|House\s+No\.?|St\.?\s*,?\s*Barangay|City/Municipality|Province)\b",
                    RegexOptions.IgnoreCase);
                if (labelFragment) continue;

                string value = StripPrefix(line);
                value = Regex.Replace(value,
                    @"^(?:BIRTH|B[I1L|]R\s*[™®])\s+", "", RegexOptions.IgnoreCase).Trim();
                if (value.Count(char.IsLetterOrDigit) >= 4) return value;
            }
            return "";
        }

        /// <summary>Read a time close to the "born alive" sentence despite minor OCR noise.</summary>
        private static string FindTimeOfBirth(string text)
        {
            Match direct = Regex.Match(text,
                @"at\s*(\d{1,2})[:.](\d{2})\s*([AaPp])\.?\s*[Mm]", RegexOptions.IgnoreCase);
            Match match = direct.Success ? direct : Regex.Match(text,
                @"born\s+alive.{0,40}?(\d{1,2})[:.](\d{2})\W*([AaPp])",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success) return "";
            return match.Groups[1].Value + ":" + match.Groups[2].Value + " " +
                match.Groups[3].Value.ToUpperInvariant() + "M";
        }

        /// <summary>
        /// The name value below a name-label line: scans the next few lines for the
        /// first that looks like a name (≥2 alphabetic tokens after prefix stripping).
        /// </summary>
        private static string NameAfter(string[] lines, int idx)
        {
            if (idx < 0) return "";
            for (int j = idx + 1; j <= idx + 3 && j < lines.Length; j++)
            {
                string s = StripPrefix(lines[j]);
                int words = s.Split(' ').Count(t => t.Length >= 2 && t.All(char.IsLetter));
                if (words >= 2) return s;
            }
            return "";
        }

        /// <summary>
        /// Correct a heavily distorted but otherwise unambiguous facility name.  This is
        /// deliberately guarded by the municipality, province, facility suffix, and edit
        /// distance so a merely similar hospital elsewhere is never silently substituted.
        /// </summary>
        private static string RepairKnownBirthFacility(string hospital, string municipality, string province)
        {
            const string canonical = "Cagayan Valley Medical Center";
            string h = Regex.Replace(hospital ?? "", @"[^A-Za-z]", "").ToLowerInvariant();
            string c = Regex.Replace(canonical, @"[^A-Za-z]", "").ToLowerInvariant();
            bool rightLocation = Regex.IsMatch(municipality ?? "", @"Tuguegarao", RegexOptions.IgnoreCase)
                && Regex.IsMatch(province ?? "", @"Cagayan", RegexOptions.IgnoreCase);
            bool facilityShape = Regex.IsMatch(hospital ?? "", @"Medical\s+Center", RegexOptions.IgnoreCase);

            if (rightLocation && facilityShape && RegionReader.Distance(h, c) <= 7)
                return canonical;
            return hospital;
        }

        /// <summary>Drop leading section letters / stray marks (e.g. "M ", "*_ ", "L ").</summary>
        private static string StripPrefix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string[] tok = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int start = 0;
            while (start < tok.Length)
            {
                string t = tok[start];
                bool junk = t.Length <= 2 || !t.Any(char.IsLetter);
                if (!junk) break;
                start++;
            }
            return Clean(string.Join(" ", tok.Skip(start)));
        }

        /// <summary>Split a form-cell name ("First[ …] Middle Last") into its three cells.</summary>
        private static (string, string, string) SplitNameCells(string raw)
        {
            raw = Clean(raw);
            if (raw.Length == 0) return ("", "", "");
            string[] t = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Drop lone stray characters picked up from the page margin: a scan of
            // "SHEILA ARTICULO BALOSO a" was otherwise read as surname "a" with the
            // real surname shunted into the middle-name cell. Only done when enough
            // real words remain, so a genuine middle initial is left alone.
            // Clean each cell on its own: Clean() only trims the ends of the whole
            // string, so a speck read as a quote in the middle stuck to a name
            // ("SHEILA“ ARTICULO BALOSO").
            t = t.Select(x => x.Trim('_', '.', ':', '-', '|', '"', '\'', '`', '“', '”', ',', ';'))
                 .Where(x => x.Length > 0)
                 .ToArray();
            if (t.Length == 0) return ("", "", "");

            if (t.Length > 3)
            {
                string[] words = t.Where(x => x.Length > 1).ToArray();
                if (words.Length >= 3) t = words;
            }
            if (t.Length == 1) return (t[0], "", "");
            if (t.Length == 2) return (t[0], "", t[1]);
            // 3+ tokens: last = Last, second-to-last = Middle, the rest = First.
            string last = t[t.Length - 1];
            string mid = t[t.Length - 2];
            string first = string.Join(" ", t.Take(t.Length - 2));
            return (first, mid, last);
        }

        /// <summary>
        /// Parse a "12 June 2018" style date to yyyy-MM-dd (blank if none). The ordinal
        /// suffix is optional because Form 97 prints the day as "18th" — without it the
        /// match slid onto a different date elsewhere on the page.
        /// </summary>
        /// <summary>
        /// Type of birth, read from the rows under the "TYPE OF BIRTH" heading.
        /// <para/>
        /// It used to be read only off the row carrying the birth WEIGHT, which tied one
        /// field to another for no reason: when OCR returned "2722 grenis" instead of
        /// "2722 grams" the weight row could not be found, and Type of Birth came back blank
        /// on a form that plainly says "Single". Two fields failing because one word was
        /// misread is a coupling worth removing rather than a threshold worth tuning.
        /// <para/>
        /// The heading's own line is skipped: the form prints "(Single, Twin, Triplet, etc.)"
        /// as a hint under it, which lists every option and would otherwise match the first.
        /// </summary>
        private static string BirthTypeIn(string[] lines)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;

            int start = FindIdx(lines, @"TYPE\s+OF\s+B[I1l]RTH");
            int from = start >= 0 ? start : 0;
            int to = start >= 0 ? Math.Min(lines.Length, start + 6) : lines.Length;

            for (int i = from; i < to; i++)
            {
                string l = lines[i] ?? "";

                // The printed option list names them all, so it can never be the answer.
                if (Regex.IsMatch(l, @"\betc\b|\bete\b|\be14\b", IC)) continue;
                int listed = new[] { "Single", "Twin", "Triplet", "Quadruplet" }
                    .Count(o => Regex.IsMatch(l, @"\b" + o + @"\b", IC));
                if (listed > 1) continue;

                if (Regex.IsMatch(l, @"\bTwin\b", IC)) return "Twin";
                if (Regex.IsMatch(l, @"\bTriplet\b", IC)) return "Triplet";
                if (Regex.IsMatch(l, @"\bQuadruplet\b", IC)) return "Quadruplet";
                if (Regex.IsMatch(l, @"\bSin\w?le\b", IC)) return "Single";
            }
            return "";
        }

        /// <summary>
        /// True when a candidate is the form's own printed HINT rather than a value.
        /// <para/>
        /// These hints are parenthesised on the paper - "(House No., St., Barangay,
        /// City/Municipality, Province)" - and a written value is not, so an unbalanced
        /// bracket is the giveaway even after OCR has mauled the words themselves. On the
        /// 1993 sample the place field was returning "Moves Mo., Gireet, Barangay)", which
        /// is that hint. A blank asks the operator to type it; a plausible-looking wrong
        /// value has to be noticed first.
        /// </summary>
        private static bool LooksLikePrintedHint(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            int open = value.Count(c => c == '('), close = value.Count(c => c == ')');
            return open != close;
        }

        private static string FindDmy(string text)
        {
            Match m = Regex.Match(text,
                @"\b(\d{1,2})(?:st|nd|rd|th)?\s+([A-Za-z]{3,9})\s+((?:19|20)\d{2})\b",
                RegexOptions.IgnoreCase);
            if (!m.Success) return "";
            string cand = m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value;
            string[] fmts = { "d MMMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "dd MMM yyyy" };
            if (DateTime.TryParseExact(cand, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                return d.ToString("yyyy-MM-dd");
            return "";
        }

        // ---- extraction helpers --------------------------------------------

        /// <summary>Best-effort religion by keyword (normalised to a canonical spelling).</summary>
        private static string ReligionOf(string text)
        {
            const RegexOptions IC = RegexOptions.IgnoreCase;
            if (Regex.IsMatch(text, @"Roman\s+Catholic|R\.?\s*Catholic|\bCatholic\b", IC)) return "Roman Catholic";
            if (Regex.IsMatch(text, @"Iglesia\s+ni\s+Cristo|\bINC\b", IC)) return "Iglesia ni Cristo";
            if (Regex.IsMatch(text, @"Islam|Muslim", IC)) return "Islam";
            if (Regex.IsMatch(text, @"Born\s*Again", IC)) return "Born Again";
            if (Regex.IsMatch(text, @"Baptist", IC)) return "Baptist";
            if (Regex.IsMatch(text, @"Aglipay", IC)) return "Aglipayan";
            if (Regex.IsMatch(text, @"Methodist", IC)) return "Methodist";
            if (Regex.IsMatch(text, @"Adventist", IC)) return "Seventh-day Adventist";
            if (Regex.IsMatch(text, @"Protestant", IC)) return "Protestant";
            return "";
        }

        // Common occupations recognised on PSA forms (extend freely).
        private static readonly string[] Occupations =
        {
            "Housewife", "Housekeeper", "Farmer", "Fisherman", "Driver", "Teacher", "Nurse",
            "Physician", "Doctor", "Engineer", "Vendor", "Laborer", "Businessman", "Businesswoman",
            "Government Employee", "Employee", "Self-employed", "OFW", "Student", "Carpenter",
            "Merchant", "Clerk", "Accountant", "Police Officer", "Soldier", "Electrician",
            "Mechanic", "Cook", "Tailor", "Seamstress", "Security Guard", "Sales Lady",
            "Cashier", "Welder", "Plumber", "Painter", "Barber", "Midwife", "Pharmacist",
            "Lawyer", "Manager"
        };

        /// <summary>First recognised occupation within a line range (a parent's block).</summary>
        private static string OccupationIn(string[] lines, int from, int to)
        {
            if (from < 0) return "";
            from = Math.Max(0, from); to = Math.Min(lines.Length, to);
            for (int i = from; i < to; i++)
                foreach (string occ in Occupations)
                    if (Regex.IsMatch(lines[i], @"\b" + Regex.Escape(occ) + @"\b", RegexOptions.IgnoreCase))
                        return occ;
            return "";
        }

        /// <summary>
        /// Label-based lookup: find the first line containing any of the labels and
        /// return the text after the label on that line; if none, the next non-empty
        /// line. Reads meaning from the label, not a fixed position.
        /// </summary>
        private static string ValueAfter(string[] lines, params string[] labels)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string low = lines[i].ToLowerInvariant();
                foreach (string label in labels)
                {
                    int idx = low.IndexOf(label, StringComparison.Ordinal);
                    if (idx < 0) continue;
                    string after = lines[i].Substring(idx + label.Length)
                        .TrimStart(':', '.', ')', '-', ' ', '\t');
                    if (after.Length >= 2 && after.Any(char.IsLetterOrDigit)) return Clean(after);
                    if (i + 1 < lines.Length && lines[i + 1].Any(char.IsLetterOrDigit)) return Clean(lines[i + 1]);
                }
            }
            return "";
        }

        private static string MatchRegex(string text, string pattern)
        {
            Match m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        /// <summary>Split a full name into (first, middle, last); tolerant of "LAST, FIRST MIDDLE".</summary>
        private static (string, string, string) SplitName(string full)
        {
            full = Clean(full);
            if (full.Length == 0) return ("", "", "");

            if (full.Contains(","))
            {
                string[] parts = full.Split(',');
                string lastN = parts[0].Trim();
                string[] rest = parts[1].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string firstN = rest.Length > 0 ? rest[0] : "";
                string midN = rest.Length > 1 ? string.Join(" ", rest.Skip(1)) : "";
                return (firstN, midN, lastN);
            }

            string[] tok = full.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length == 1) return (tok[0], "", "");
            if (tok.Length == 2) return (tok[0], "", tok[1]);
            return (tok[0], string.Join(" ", tok.Skip(1).Take(tok.Length - 2)), tok[tok.Length - 1]);
        }

        /// <summary>
        /// Strip stray OCR punctuation/underscores from a captured value. Quote marks
        /// are included because the engine reads the form's ruled lines and specks as
        /// quotes, which then stick to a name ("SHEILA“").
        /// </summary>
        private static string Clean(string s)
        {
            s = (s ?? "").Trim(' ', '_', '.', ':', '-', '|', '"', '\'', '`', '“', '”', '‘', '’');
            return Regex.Replace(s, @"\s{2,}", " ").Trim();
        }

        /// <summary>Best-effort date normalise to yyyy-MM-dd (blank if unparseable).</summary>
        private static string NormDate(string s)
        {
            s = Clean(s);
            if (s.Length == 0) return "";
            string[] fmts = { "MMMM d, yyyy", "MMMM dd, yyyy", "MMM d yyyy", "M/d/yyyy", "d/M/yyyy",
                              "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" };
            if (DateTime.TryParseExact(s, fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
                || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.ToString("yyyy-MM-dd");
            return s;   // keep raw text for the operator to correct
        }

        public static string KindName(DocKind k)
        {
            switch (k)
            {
                case DocKind.Birth: return "Birth Certificate";
                case DocKind.Marriage: return "Marriage Certificate";
                case DocKind.Death: return "Death Certificate";
                default: return "Unknown";
            }
        }
    }
}

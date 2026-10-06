# An OCR engine written from scratch for civil-registry forms

Capstone component of CROMS (Civil Registry Operations Management System, LGU Penablanca LCRO).
Built to a seven-day plan (preprocessing, segmentation, training data, classifier, splitting, repair and benchmark, write-up). Project folders: `CROMS.OwnOcr` (the engine, a library) and `CROMS.OwnOcr.Bench` (the test harness).

## 1. Requirement and constraints

The requirement was to write an OCR engine from scratch, not to wrap an existing one. We agreed these limits at the start:

- C# only, .NET Framework base library only. The project references `System`, `System.Core` and `System.Drawing` and nothing else (no Tesseract, no OpenCV, no NuGet package, no ML library).
- Printed text only. Handwriting is out of scope.
- Field crops first. The engine reads one cropped field at a time (a name, a place, a citizenship). Whole-page layout analysis is not attempted; CROMS already knows where each field sits on a known form.
- One week.
- A typewriter font may be used as training data (Courier Prime, SIL Open Font Licence, included beside the code).

The engine is about 2,600 lines of C# in 17 files.

### What "from scratch" means here, exactly

| Written by us | Borrowed |
|---|---|
| grayscale conversion, bicubic upscaling, adaptive thresholding, connected components, table-rule removal, line choice, character splitting, glyph normalisation, feature extraction, k-NN classifier, synthetic data generator, lexicon repair | `System.Drawing` to load an image file and to draw text with a font when making training data. It is used as a pixel buffer; every pixel operation is our own code on arrays. |

Tesseract is not used by the engine. It appears only in the benchmark, as the thing we compare against.

## 2. The pipeline

```
field crop
  1 grayscale                         GrayImage      (LockBits copy-out, no GetPixel)
  2 bicubic upscale to ~40 px glyphs  Resampler      (own Catmull-Rom)
  3 adaptive threshold                Binarizer      (Bradley/Roth, integral image)
  4 remove table rules                LineRemover
  5 connected components              ConnectedComponents (8-connected, two-pass union-find)
  6 choose the value line             Segmenter
  7 drop rule remnants, keep one run  Segmenter
  8 cut pieces that are two letters   Segmenter
  9 20x20 baseline-normalised glyphs  GlyphCell
 10 features (233 numbers per glyph)  Features
 11 k-NN classification               KnnClassifier  (+ field knowledge)
 12 words from gap sizes              OwnOcrReader
 13 lexicon repair                    Lexicon
```

`DebugRender` draws every stage into one picture, which is how most bugs were found (figure in section 9).

Notes on the stages that needed real design:

- **Thresholding (3).** A single global threshold fails on tinted, unevenly lit security paper. Each pixel is compared with the mean of its own neighbourhood (integral image, so the cost is constant per pixel), plus a minimum-contrast guard so blank paper grain is not read as ink.
- **Upscaling (2).** Glyphs in these crops are only 9-17 px tall. Thresholding them directly gives blocky shapes, so the crop is enlarged first.
- **Rule removal (4).** A table rule is not just a long run of ink: a letter stem is a run too, and a photographed page is tilted. Candidate runs are bridged across small gaps, grouped into connected pieces, and kept only if the whole piece is long and thin. Vertical rules must span 75% of the crop height because the stem of an I or l is about half of it.
- **Features (10).** 10x10 zoning (ink density per block), a gradient-direction histogram (4x4 blocks, 8 directions), and 5 geometry values (width, top and bottom against the line, aspect). Each block is L2-normalised so none outweighs the others. 233 numbers per glyph.
- **Classifier (11).** k-nearest-neighbour, k = 5, distance-weighted vote, at most 600 stored examples per class. Chosen because it has no training step to get wrong and every answer can be explained by pointing at the examples that voted for it.
- **Field knowledge (11).** A name or place has no digits and none of `# & : ; / ( )`, and a word keeps one letter case (decided by vote). Applied as an allowed-label mask inside the k-NN vote, not as post-editing.

## 3. Training data: synthetic text

There is no labelled set of real typewriter glyphs, so we generated one. `Synth` renders random lines from Courier Prime and seven installed fonts, then degrades them: size 12-30 px, tilt, shear, ink bloom, touching letters, blur, uneven light, noise, and a table rule under half the lines.

The key design choice: synthetic lines go through the same engine as real crops (`OwnOcrEngine.Analyze`), so a training glyph is normalised exactly like a real one. A line is labelled only when the engine finds as many characters as the line has; otherwise it is dropped (49% were, mostly touching letters). This teaches clean single characters; the splitter handles touching ones.

Result: 20,000 lines produced 103,722 labelled glyphs (at least 177 per class), plus 30,824 "reject" examples (merged pairs, half letters) that teach the classifier to say "this is not one character".

The real crops are test data only, never training data, so the scores below are honest in that respect.

## 4. Test data

`CROMS.DocTest --dump-crops` cuts every field out of the five office sample scans with CROMS's own region reader: 101 crops, of which 72 hold text printed as stored (names, places, citizenship, religion, occupation, civil status, cause of death). Dates, sex, weight, age, registry numbers and tick-box rows are excluded because their stored value is not the characters on the paper. Each crop has a hand-checked truth string.

The five documents are different printing: a clean 2007 laser-printed birth certificate, a faded 1993 photocopy, a 2016 death certificate, and typewriter print on textured security paper (marriage).

The real crops are citizens' certificates. They are kept out of version control (`data/` is git-ignored) and must not be published.

## 5. Results

### 5.1 Segmentation: does the engine find the right number of characters?

Measured on the 72 crops (the count of characters found equals the count on the paper):

| Stage added | exact count | within +/-1 |
|---|---|---|
| first cut, largest-ink line, no filters | 11% | 22% |
| drop rule remnants (flat slivers, specks) | 34% | 50% |
| choose the line by height and nearness to centre, drop pieces under the baseline, keep one run, split only very wide pieces | 55% | 72% |
| join broken rule fragments before the length test | 55% | 75% |

By document (exact): birth 2007 9/20, 1993 photocopy 12/15, death 12/15, marriage typewriter 7/22.

### 5.2 Single-character accuracy

| | accuracy |
|---|---|
| synthetic hold-out (rendered glyphs) | 92.5% |
| real glyphs cut from the crops | 73.2% (78% ignoring upper/lower case) |

The 19-point gap between rendered and real glyphs is the main finding of the classifier work. The commonest real mistakes are look-alikes: i read as l, c as e, o as e, r as R, l as 1, I as 1. The 325 real test glyphs carry a little label noise (a character count can match by coincidence), so 73% is slightly understated.

### 5.3 Whole fields against Tesseract

Per field, 72 crops. "CROMS (Tesseract)" is not raw Tesseract: it is the production CROMS result after region reading, several renderings, voting and context repair, so it is a hard baseline.

| | exact | exact, any case | mean character similarity |
|---|---|---|---|
| own engine, first end-to-end run | 9.7% | n/a | 55.8% |
| + field knowledge (no digits, one case per word) | 16.7% | 19.4% | 58.0% |
| + edge-mark trimming | 18.1% | 20.8% | 58.6% |
| + lexicon repair | **30.6%** | 33.3% | **62.0%** |
| CROMS (Tesseract) | 43.1% | 47.2% | 71.4% |

Character similarity is 1 minus edit distance divided by the longer length.

By document, similarity ours vs Tesseract: birth 2007 72% vs 92%, faded 1993 photocopy **69% vs 65%**, death 72% vs 82%, marriage typewriter 30% vs 50%.

Speed: about 38 ms per crop for the front half, about 20 ms per glyph for k-NN over the reference set.

### 5.4 Lexicon repair

A reading that is a letter or two off is snapped to a public vocabulary: provinces (87), municipalities (1,647), barangays (27,040 distinct), nationalities, religions, civil statuses, taken from the CROMS master tables.

- It is not a list of persons' names, and not of occupations. A name list gains near-neighbours as the registry grows (it would turn a father "Alberto" into his son "Gilbert"), and a wrong name that looks right is worse than a garbled one.
- The distance is classifier-informed, not plain edit distance. Each character keeps its ranked candidates; calling a character a letter costs 1 minus (its vote share over the top vote share), so a second-choice letter is cheap and a letter the classifier never considered costs 1.
- A repair is accepted only if the cost is at most 0.28 times the entry length and the next-best distinct entry is at least 0.5 worse. Otherwise the reading is left exactly as read.
- Effect: 12 fields repaired, 12 better, none worse. Examples: `FAMFAWOA` to `PAMPANGA`, `Roman(atholio` to `Roman Catholic`, `Siwrle` to `Single`, `Filisiau` to `Filipino`, `T'uguegxrao Ciw,` to `Tuguegarao City`.
- It cannot help the large majority of fields: about 48 of the 72 are people's names, with hospital names and occupations on top.

## 6. What did not work (kept, because it is the honest result)

### 6.1 Classifier-guided splitting by dynamic programming

Typewriter letters touch, and cutting at the thinnest column cuts inside an H. The idea was to try many candidate cuts and let the classifier choose the cheapest path through the line. Four variants and a reject class, same 72 fields (exact / similarity), against the plain thinnest-column splitter at 12.5% / 56.0%:

| variant | exact | similarity |
|---|---|---|
| per-clump search, distance cost | 12.5 | 52.9 |
| global search (can also merge broken letters) | 6.9 | 33.5 |
| global, vote-share cost | 8.3 | 40.0 |
| global + reject class | 15.3 | 42.3 |
| + dense candidate cuts | 15.3 | 42.0 (19 minutes per run) |
| per-clump + reject class | 13.9 | 54.4 |

None beat the plain splitter. Why, from measurements and pictures:

1. The distance to the nearest stored example barely separates a glyph read right from one read wrong (median 0.57 vs 0.69 on real glyphs). The neighbours' vote share does (0.86 vs 0.57), so the cost must lead with the vote.
2. With many candidate boxes, some box always looks like a confident letter (selection bias): two narrow letters read as a confident `m`. A classifier that has only seen letters needs a reject class, and even then it merged real clumps that its synthetic rejects did not resemble.
3. Where the plain splitter was already right, the search could only make it worse.

The variants stay behind switches so the result is reproducible; the plain splitter is the default.

### 6.2 Training with real glyphs

The obvious fix for the 73% real-glyph accuracy is to train on real glyphs. Leave-one-document-out (a document is read by a classifier that has seen real glyphs from the other four only), 225-261 extra glyphs, never thinned, vote weighted:

| real-glyph vote weight | exact | similarity |
|---|---|---|
| none | 16.7% | 58.0% |
| 1 | 15.3% | 57.8% |
| 3 | 13.9% | 57.0% |

It got slightly worse. Probable reason: the five documents are printed differently, so one document's glyphs are weak evidence for another's, and 250 glyphs over 60 classes is about 4 per class.

## 7. Lessons about the problem

1. "Most ink" is the wrong test for which text line is the value. A row cut off at the crop edge has many small letters and outweighed a 7-letter name. The value is taller than its cropped neighbours and near the middle of the crop.
2. Removing a rule leaves slivers just under the baseline that count as characters (28 found for 13 real ones). Filter flat pieces, specks, and anything wholly below the baseline.
3. A thin faint rule thresholds into fragments that each fail a length test, so half of it stayed behind as an underscore. Join fragments on one line before testing length.
4. Touching letters must be cut at a chosen point, not the thinnest column, and the typical character width must come from the upper quartile because merged pairs pollute the average.
5. A classifier trained on rendered text meets real typewriter ink (heavier, filled counters, uneven) as a different domain. The data matters more than the algorithm.
6. Knowing the field helps more than a better classifier did in the time available: the lexicon repair gained 12 points of exact match where real-glyph training gained none.

## 8. Limits, stated plainly

- Tesseract is better on clean print: it reads 18 of 20 fields on the 2007 birth certificate where the own engine reads 4. The own engine is ahead only on the faded 1993 photocopy.
- The own engine reads fields one crop at a time. It does not find fields on a page; CROMS's existing layout code does that.
- Test set: 72 fields from 5 documents. The design was tuned on those same crops, so 30.6% is indicative, not a held-out score. No hold-out set existed to keep apart.
- Field-level exact match is harsh: one wrong character fails the field. Similarity (62%) says the readings are usually close.
- Names, the largest group of fields, get no vocabulary help by design.
- Handwriting is not read.

## 9. Decision for CROMS

Tesseract stays the production engine and the fallback. Nothing in the CROMS application calls the own engine. It is a separate, self-contained, explainable engine that meets the requirement, and the benchmark above says where it would have to improve first: per-character accuracy on real typewriter ink and merged-letter splitting.

## 10. Reproducing and demonstrating

Build (PowerShell, from the repository root):

```
$ms="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
& $ms CROMS.OwnOcr.Bench\CROMS.OwnOcr.Bench.csproj -p:Configuration=Debug -nologo -v:minimal
```

Demo of every stage on one crop (writes a picture):

```
CROMS.OwnOcr.Bench\bin\Debug\CROMS.OwnOcr.Bench.exe debug <crop.png> <outFolder>
```

Figure for the report: `CROMS.OwnOcr\data\demo\nice__MotherFirst_debug.png` shows the stages for the field "SHEILA": (1) the crop, (2) adaptive threshold, (3) the table rule found and removed in red, (4) the chosen line with each character box (green whole, magenta cut), (5) the 20x20 normalised characters. It also shows a real failure: the E is cut into two pieces, so the engine reads `SHIIILA`. That is the splitting problem of section 6.1 in one picture. (The picture is a real certificate: for a printed report, fine; do not publish it.)

Benchmarks:

```
Bench segment <crops>                                   segmentation (5.1)
Bench gen 20000 data\synth.bin 7                        synthetic training set (section 3)
Bench eval data\synth.bin data\real.bin                 single-character accuracy (5.2)
Bench read <crops> data\synth.bin --nodp --letters --case --trim --lexicon data\lexicon
                                                        whole fields vs Tesseract (5.3, 5.4)
Bench read ... --realtrain 1                            real-glyph training (6.2)
Bench read ... (without --nodp)                         dynamic-programming splitter (6.1)
```

The data under `CROMS.OwnOcr\data` is generated or exported (crops from `CROMS.DocTest --dump-crops`, lexicon from the CROMS master tables) and is not in the repository.

## 11. File map

| File | Job |
|---|---|
| GrayImage, BinaryImage | pixel arrays |
| Resampler | bicubic upscaling |
| Binarizer | adaptive threshold |
| ConnectedComponents | pieces of ink |
| LineRemover | table rules |
| Segmenter | line choice, remnants, run, cutting, 20x20 glyphs |
| SplitSearch | classifier-guided splitting (experimental, off) |
| Charset, Features, Dataset | label set, 233-number feature vector, training file |
| Synth | synthetic training data |
| Classifier | k-NN |
| OwnOcrEngine, OwnOcrReader | front half; image to text with field knowledge |
| Lexicon | public-vocabulary repair |
| DebugRender | stage-by-stage picture |

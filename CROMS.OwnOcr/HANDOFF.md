# Own OCR engine - hand-off (read this first in a new window)

Last updated 2026-10-06, end of Day 6 of the 7-day plan. Commits are made AUTOMATICALLY by a hook (messages 'auto: claude update') and are already on origin/main; I did not make them.

## What this is
The professor's requirement: **write an OCR engine from scratch.** Constraints agreed with the user:
all C#, .NET Framework base library only (System.Drawing for pixels), 1 week, printed text only, field
crops (not whole-page layout) first, typewriter .ttf allowed as training data (Courier Prime, downloaded
into this folder with its OFL licence).
It is a **separate project**. CROMS (the app) still uses Tesseract and nothing in the app calls this engine.

## Safety net
Backup taken before any work: `C:\Users\ivan palogan\CROMS_Backups\2026-10-06_pre-own-ocr\`
(`CROMS_repo.bundle` + `croms_db.sql`) and git tag `backup-pre-own-ocr-2026-10-06`.
Restore code: `git clone CROMS_repo.bundle restored`. Restore DB: `mysql croms < croms_db.sql`.

## Git state
An automatic hook ('auto: claude update') commits and pushes the working tree, so the engine code, the
Bench, CropDump and the Courier Prime font (SIL OFL) are ALREADY on origin/main. Checked: nothing under
CROMS.OwnOcr/data/ is tracked (gitignored: real citizens' certificate crops = PII). Do not add data/ to git.

## Pipeline (CROMS.OwnOcr, library)
image -> grayscale -> bicubic upscale (glyphs to ~40 px) -> adaptive threshold -> table-rule removal ->
connected components -> choose the value LINE -> drop rule remnants -> keep one text run ->
split wide pieces -> 20x20 baseline-normalised glyph pictures -> features -> k-NN -> text (+ spaces).

| File | Job |
|---|---|
| GrayImage, BinaryImage | pixel arrays (LockBits, no GetPixel) |
| Resampler | own bicubic (Catmull-Rom) |
| Binarizer | Bradley/Roth adaptive threshold over an integral image |
| ConnectedComponents | 8-connected two-pass union-find |
| LineRemover | rule detection (fragments on one line are joined BEFORE the length test) |
| Segmenter | line choice, remnant filter, run selection, cut of wide pieces, GlyphCell 20x20 |
| OwnOcrEngine | `Analyze(Bitmap or GrayImage)` -> `Analysis` (every stage kept) |
| DebugRender | stacked picture of every stage - THE way to see why something failed |
| Charset | A-Z a-z 0-9 . , - ' / ( ) : ; & # N-tilde (escapes, never literal non-ASCII) |
| Features | zoning 10x10 + gradient histogram 4x4x8 + 5 geometry values (233 dims) |
| Synth | renders lines from fonts + degrades them, runs them through the engine, labels cells |
| Dataset | binary save/load of labelled glyphs |
| Classifier | `KnnClassifier` (balanced per class, distance-weighted vote) |
| SplitSearch | classifier-scored DP splitter (experimental, off by default - see results) |
| OwnOcrReader | image -> text; options LettersOnly, CaseConsistency, optional scorer for the DP splitter |

Bench (`CROMS.OwnOcr.Bench`, console, not in the .sln) modes:
`debug`, `segment`, `gen`, `realset`, `sheet`, `eval [--tune]`, `read` (last one written, not run).

## Data (all under CROMS.OwnOcr/data/, gitignored, regenerate if missing)
- `crops/` + `manifest.tsv` : 101 real field crops with truth + Tesseract's reading.
  Regenerate: `CROMS.DocTest.exe --dump-crops <dir>` (needs the 5 sample scans in Downloads; ~3 min).
- `synth.bin` : 103,722 synthetic glyphs. `Bench gen 20000 <out> 7` (2.5 min).
- `real.bin`  : 325 glyphs cut from real crops where the character count matched. `Bench realset <crops> <out>`.
  NOTE it has a little label noise (a count can match by coincidence) so real accuracy is slightly UNDER-stated.

## Results so far (end of Day 5)
- Segmentation (character COUNT equals the paper): 55% exact, 75% within +/-1 (plain splitter).
- Classifier, per character: synthetic hold-out 92.5%, REAL glyphs 73.2%. Weight sweep: defaults best.
- WHOLE ENGINE per FIELD (72 printed-text crops, `Bench read <crops> <synth.bin> --nodp --letters --case`):
  **own 16.7% exact / 58.0% character similarity vs CROMS/Tesseract 43.1% / 71.4%.**
  By sample (similarity ours vs Tesseract): birth 2007 71 vs 92; faded 1993 photocopy 68 vs 65 (ours level/ahead);
  death 71 vs 82; marriage typewriter 31 vs 50.
- Classifier-guided DP splitting (4 variants + a reject class) did NOT beat the plain splitter; full table and
  the reasons are in CLAUDE.md (Day 5 entry). The plain splitter is the default; DP stays behind switches.
- Field knowledge helps a little: LettersOnly + CaseConsistency (12.5/56.3 -> 16.7/58.0).
- Speed: front half ~38 ms/crop; k-NN ~20 ms/glyph with 62k refs; the read benchmark takes ~70 s.

## Day 6 result (see CLAUDE.md)
raw 18.1% exact / 58.6% sim; + lexicon repair 30.6% / 62.0%; Tesseract 43.1% / 71.4%. Real-glyph training (leave-one-document-out) made it WORSE (15.3 / 13.9%). Lexicon files: data\lexicon\*.txt (export from croms master tables, gitignored). Run: ead <crops> <synth.bin> --nodp --letters --case --trim --lexicon data\lexicon.

## NEXT STEPS (in order, 1-2 are DONE or rejected: 6a rejected, 6b done)
1. Day 6a - the classifier is the bottleneck (73% per glyph). Biggest likely gain: close the synthetic->real gap.
   Train WITH real glyphs (leave-one-document-out so the score stays honest) and/or make the synthetic ink heavier.
2. Day 6b - dictionary / lookalike repair: lexicon from the PSGC tables + nationalities/religions/occupations
   (public names only; NOT persons' names, NOT the test truth), snap a word to a UNIQUE near match (edit distance
   scaled by length), never invent. Re-run `read` and report.
3. Speed: prototypes (k-means per class) instead of 60k raw references.
4. Day 7 - write-up for the professor: pipeline, the four segmentation lessons, the negative DP result and why,
   the honest table vs Tesseract, limits. Keep Tesseract as the production engine and fallback.
5. Log each step in CLAUDE.md; commit only when the user says.
## Build / run (PowerShell; the leading-slash `/p:` form is mangled by Git Bash, use `-p:`)
```
$ms="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
& $ms CROMS.OwnOcr.Bench\CROMS.OwnOcr.Bench.csproj -p:Configuration=Debug -nologo -v:minimal
CROMS.OwnOcr.Bench\bin\Debug\CROMS.OwnOcr.Bench.exe segment CROMS.OwnOcr\data\crops
```
DocTest: CROMS.exe may be running and VS holds `CROMS\bin\Debug`, so build DocTest with
`-p:BuildProjectReferences=false` (uses the existing CROMS.exe dated 2026-10-05; rebuild in VS before
trusting any DocTest accuracy number from current source).

## Traps already hit (do not repeat)
- Never pipe a long-running exe into `Select-Object -First N` (it kills the process, exit 255).
- PowerShell `[IO.File]` uses the process folder, not `Set-Location`: use absolute paths or the Edit tool.
  A failed patch script once left two empty stray files at the repo root (removed).
- The Bench exe cannot be rebuilt while a Bench process (e.g. `eval --tune`) is still running.
- "Most ink" is the wrong test for which text line is the value; "touching letters cut at the thinnest
  column" cuts inside an H; the typical glyph width is polluted by merged pairs (use the upper quartile).
- Render lock: GDI+ rendering in Synth is under a lock; analysis runs in parallel.
- The Write/Edit file tools turn typed \u00D1-style escapes into literal characters: check new sources for non-ASCII bytes.
- A leftover Bench process (killed background run) locks CROMS.OwnOcr.dll: Stop-Process CROMS.OwnOcr.Bench before rebuilding.
- synth.bin now contains 30,824 reject samples; synth_noreject.bin is the older set.

## Paste this into the new window
> Continue the own-OCR capstone work. Read `CROMS.OwnOcr/HANDOFF.md` and the last entries of `CLAUDE.md`
> first. Build the Bench, run `read` against the crops and synth.bin, then continue with Day 5 (DP character
> splitting). Do not commit or push unless I say so.

# Own OCR engine - hand-off (read this first in a new window)

Last updated 2026-10-06, end of Day 4 of the 7-day plan. Nothing here is committed or pushed.

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

## Uncommitted work (git status)
Modified: `.gitignore`, `CLAUDE.md`, `CROMS.DocTest/CROMS.DocTest.csproj`, `CROMS.DocTest/Program.cs`.
New: `CROMS.DocTest/CropDump.cs`, `CROMS.OwnOcr/`, `CROMS.OwnOcr.Bench/`.
Per CLAUDE.md git rules: commit only these files when the task is done or the user says so; never force push.
`CROMS.OwnOcr/data/` is gitignored on purpose (real citizens' certificate crops = PII).

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
| OwnOcrReader | image -> text. **Written, NOT YET BUILT OR RUN** |

Bench (`CROMS.OwnOcr.Bench`, console, not in the .sln) modes:
`debug`, `segment`, `gen`, `realset`, `sheet`, `eval [--tune]`, `read` (last one written, not run).

## Data (all under CROMS.OwnOcr/data/, gitignored, regenerate if missing)
- `crops/` + `manifest.tsv` : 101 real field crops with truth + Tesseract's reading.
  Regenerate: `CROMS.DocTest.exe --dump-crops <dir>` (needs the 5 sample scans in Downloads; ~3 min).
- `synth.bin` : 103,722 synthetic glyphs. `Bench gen 20000 <out> 7` (2.5 min).
- `real.bin`  : 325 glyphs cut from real crops where the character count matched. `Bench realset <crops> <out>`.
  NOTE it has a little label noise (a count can match by coincidence) so real accuracy is slightly UNDER-stated.

## Results so far
- Segmentation (character COUNT equals the paper): **55% exact, 75% within +/-1** (72 printed-text crops).
  Birth 1993 12/15, death 12/15, birth 2007 9/20, marriage typewriter only 7/22.
- Classifier, per character: synthetic hold-out 92.5%, **REAL glyphs 73.2%** (untuned). 8-setting weight
  sweep showed the default weights (1,1,1) are best within noise (325 test glyphs is small).
  Common mistakes: i->l, c->e, o->e, r->R, l->1, I->1 (look-alikes; context/dictionary repair is Day 6).
- Speed: ~38 ms per crop front half; k-NN with 62k references is slow (~20 ms per glyph).

## NEXT STEPS (in order)
1. Build the Bench (MSBuild, see below) and run `read <cropsDir> <synth.bin>` - the first honest
   "own engine vs CROMS/Tesseract" per-field number. Look at the printed examples.
2. Day 5: replace the thinnest-column cut with dynamic programming. Candidate cut columns = valleys of the
   column ink profile; score each candidate segment with the classifier; pick the best path. For typewriter
   print also estimate the constant character PITCH. This is where marriage (7/22) should improve.
3. Day 6: dictionary / lookalike correction (places from the PSGC tables, months, nationalities, case repair
   o/O s/S c/C), then re-run `read` and write the comparison table. Keep Tesseract as fallback in CROMS.
4. Speed: shrink the reference set (prototypes per class) so DP scoring is affordable.
5. Day 7: write-up for the professor (pipeline, the four segmentation lessons, numbers, limits).
6. Log each step in `CLAUDE.md` (the project convention) and commit when the user says.

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

## Paste this into the new window
> Continue the own-OCR capstone work. Read `CROMS.OwnOcr/HANDOFF.md` and the last entries of `CLAUDE.md`
> first. Build the Bench, run `read` against the crops and synth.bin, then continue with Day 5 (DP character
> splitting). Do not commit or push unless I say so.

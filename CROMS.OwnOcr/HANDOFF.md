# Own OCR engine - hand-off (read this first in a new window)

Last updated 2026-10-06. The 7-day plan is COMPLETE (Days 1-7). The write-up is WRITEUP.md (WRITEUP.docx is generated from it).
Commits are made AUTOMATICALLY by a hook (messages 'auto: claude update') and are already on origin/main; I did not make them.
Re-verified on 2026-10-06: Bench builds clean and `read` reproduces the Day 6 table below.

## What this is
The professor's requirement: **write an OCR engine from scratch.** Constraints agreed with the user:
all C#, .NET Framework base library only (System.Drawing for pixels), 1 week, printed text only, field
crops (not whole-page layout) first, typewriter .ttf allowed as training data (Courier Prime, downloaded
into this folder with its OFL licence).
It is a **separate project**. CROMS (the app) still uses Tesseract and nothing in the app calls this engine.
Decision: Tesseract stays the production engine and fallback.

## Safety net
Backup taken before any work: `C:\Users\ivan palogan\CROMS_Backups\2026-10-06_pre-own-ocr\`
(`CROMS_repo.bundle` + `croms_db.sql`) and git tag `backup-pre-own-ocr-2026-10-06`.
Restore code: `git clone CROMS_repo.bundle restored`. Restore DB: `mysql croms < croms_db.sql`.

## Git state
An automatic hook ('auto: claude update') commits and pushes the working tree, so the engine code, the
Bench, CropDump and the Courier Prime font (SIL OFL) are ALREADY on origin/main. Checked: nothing under
CROMS.OwnOcr/data/ is tracked (gitignored: real citizens' certificate crops = PII). Do not add data/ to git.
WRITEUP.docx embeds a real certificate crop: do not publish it.

## Pipeline (CROMS.OwnOcr, library)
image -> grayscale -> bicubic upscale (glyphs to ~40 px) -> adaptive threshold -> table-rule removal ->
connected components -> choose the value LINE -> drop rule remnants -> keep one text run ->
split wide pieces -> 20x20 baseline-normalised glyph pictures -> features -> k-NN -> text (+ spaces) ->
optional field knowledge (letters only, one case per word, trim edge marks) -> optional lexicon repair.

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
| Charset | A-Z a-z 0-9 . , - ' / ( ) : ; & # N-tilde + a Reject class (escapes, never literal non-ASCII) |
| Features | zoning 10x10 + gradient histogram 4x4x8 + 5 geometry values (233 dims) |
| Synth | renders lines from fonts + degrades them, runs them through the engine, labels cells |
| Dataset | binary save/load of labelled glyphs |
| Classifier | `KnnClassifier` (balanced per class, distance-weighted vote, optional `extra` real glyphs) |
| SplitSearch | classifier-scored DP splitter (experimental, OFF by default - negative result, see below) |
| OwnOcrReader | image -> text; options LettersOnly, CaseConsistency, TrimEdgeMarks, optional DP scorer; keeps ranked candidates per glyph |
| Lexicon | public closed vocabularies; classifier-informed distance repair (Lexicon, LexiconSet, RepairResult) |

Bench (`CROMS.OwnOcr.Bench`, console, not in the .sln) modes:
`debug`, `segment`, `gen`, `realset`, `sheet`, `eval [--tune]`, `dist`, `read`.
`read` flags: `--nodp --letters --case --trim --lexicon <dir> --penalty --global --debug --realtrain <w>`.

## Data (all under CROMS.OwnOcr/data/, gitignored, regenerate if missing)
- `crops/` + `manifest.tsv` : 101 real field crops with truth + Tesseract's reading.
  Regenerate: `CROMS.DocTest.exe --dump-crops <dir>` (needs the 5 sample scans in Downloads; ~3 min).
- `synth.bin` : 103,722 labelled glyphs + 30,824 reject samples. `Bench gen 20000 <out> 7` (2.5 min).
  `synth_noreject.bin` is the older set without rejects.
- `real.bin`  : 325 glyphs cut from real crops where the character count matched. `Bench realset <crops> <out>`.
  NOTE it has a little label noise (a count can match by coincidence) so real accuracy is slightly UNDER-stated.
- `lexicon/*.txt` : provinces 87, municipalities 1,647, barangays 27,040, nationalities, religions, civil statuses.
  Exported from the croms master tables. Public names only - NOT person names, NOT occupations.
- `demo/` : stage pictures for the presentation. `read_*.txt`, `seg_bad`, `read_bad`: saved benchmark output.

## Final results (72 printed-text field crops, per FIELD)
Run: `Bench read CROMS.OwnOcr\data\crops CROMS.OwnOcr\data\synth.bin --nodp --letters --case --trim --lexicon CROMS.OwnOcr\data\lexicon`

| | exact | exact (any case) | char similarity |
|---|---|---|---|
| Own engine (+ field knowledge + trim) | 18.1% | 20.8% | 58.6% |
| Own + lexicon repair | 30.6% | 33.3% | 62.0% |
| CROMS (production Tesseract) | 43.1% | 47.2% | 71.4% |

Similarity by document, own vs Tesseract: birth 2007 72 vs 92; faded 1993 photocopy 69 vs 65 (ours ahead);
death 72 vs 82; marriage typewriter 30 vs 50.
Repair: 10 fields repaired, 10 better, 0 worse on the 2026-10-06 re-run (the Day 6 log entry says 12; the
exact/similarity figures are identical).
Other numbers: segmentation (character COUNT equals the paper) 11% -> 55% exact, 75% within +/-1;
classifier per character 92.5% on synthetic hold-out vs 73.2% on REAL glyphs.
Speed: front half ~38 ms/crop; k-NN ~20 ms/glyph with 62k refs; the read benchmark takes ~70 s.

## Negative results (do not retry without new evidence)
- **Classifier-guided DP splitting (Day 5):** four variants + a reject class, none beat the plain splitter
  (12.5% exact / 56.0% sim; best DP 15.3% exact but 42.3% sim). Reasons: nearest-example distance barely
  separates right from wrong reads (vote share does); selection bias with many candidate boxes; where the
  plain splitter was right the search could only make it worse. Variants stay behind switches
  (`SplitSearch.GlobalMerge / DenseCandidates`, Bench `--nodp` / `--global`). Plain splitter is the default.
- **Training with real glyphs (Day 6a):** leave-one-document-out, weight 1 -> 15.3%, weight 3 -> 13.9% exact
  vs 16.7% none. The five documents are different printing, ~4 real glyphs per class. Code kept
  (`extra` ctor, `--realtrain <w>`).
- **Feature-block weight sweep:** defaults (1,1,1) best within noise.

## What helped
Field knowledge (LettersOnly + CaseConsistency: 12.5/56.3 -> 16.7/58.0, edge-mark trim -> 18.1/58.6) and
lexicon repair (-> 30.6/62.0). Full table and reasons: CLAUDE.md (Day 5 and Day 6 entries).

## Still the bottleneck (honest)
Per-glyph accuracy (73% on real glyphs) and merged-letter splitting, mostly on clean PSA print where
Tesseract reads 18 of 20 fields and ours 4. Personal names and hospital/occupation fields have no public
vocabulary, so repair cannot help them.

## OPEN IDEAS (none started; only do if the user asks)
1. Speed: prototypes (k-means per class) instead of ~60k raw references.
2. Close the synthetic -> real gap by making synthetic ink heavier (real typewriter E/e fill their counters).
3. Dictionary repair for occupations only if a CLEAN list exists (the occupations table holds learned junk).

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
- The Bench exe cannot be rebuilt while a Bench process (e.g. `eval --tune`) is still running; a leftover
  killed run also locks CROMS.OwnOcr.dll: Stop-Process CROMS.OwnOcr.Bench before rebuilding.
- "Most ink" is the wrong test for which text line is the value; "touching letters cut at the thinnest
  column" cuts inside an H; the typical glyph width is polluted by merged pairs (use the upper quartile).
- Render lock: GDI+ rendering in Synth is under a lock; analysis runs in parallel.
- The Write/Edit file tools turn typed \u00D1-style escapes into literal characters: check new sources for non-ASCII bytes.
- Run output with `*> file` from PowerShell shows the first stderr line as an error record; harmless.

## Paste this into a new window
> Continue the own-OCR capstone work. Read `CROMS.OwnOcr/HANDOFF.md` and the last entries of `CLAUDE.md`
> first. The 7-day plan is finished; do only what I ask next. Do not commit or push unless I say so.

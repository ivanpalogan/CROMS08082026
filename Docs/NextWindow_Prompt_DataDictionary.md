You are working in the CROMS repo (C:\Users\ivan palogan\OneDrive\Documents\Capstone\CROMS). Read CLAUDE.md first (Git workflow, Progress Log, and how the demo environment and the render/test harnesses are run). Do the whole task yourself, end to end - do not hand back a plan. Reply short (caveman style if the plugin is active).

GOAL
Make a DATA DICTIONARY of the whole CROMS database as a Word document, laid out exactly like the sample picture I attach (one table per database table: a header row, then one row per column).

LAYOUT OF EACH TABLE (copy the picture)
- 5 columns, in this order: Field | Type | Size | Attribute | Example.
- Header row bold. The Field column is bold too. Every other cell is normal weight.
- Text colour: the same pink / magenta as the picture (about #B5006E) in the header, the Field column and the body. Font: Times New Roman, about 11-12 pt. Thin black borders on every cell (single line, 1 pt), no shading.
- Column widths (US Letter, 1 inch margins, 6.5 in total): Field 1.35 in, Type 1.0 in, Size 0.85 in, Attribute 1.25 in, Example 2.05 in. Long examples wrap inside the cell like in the picture.
- Above each table: a bold line "Table N. <table_name>" and one short plain sentence saying what the table is for (use the screen / purpose list in Docs\DiagramTools\gen_table_bubbles.py, TABLES, which already has a sentence for every table).
- Do not split a row across two pages. A table may continue on the next page; that is fine.

WHAT GOES IN EACH COLUMN (all read from the database, never typed by hand)
- Field: the exact column name, in the database order (ordinal_position). EVERY column of EVERY table, none merged, none skipped (this is different from the bubble diagrams, which merged names and addresses).
- Type: the data type in lower case: int, bigint, tinyint, varchar, char, text, longtext, date, time, datetime, timestamp, decimal, enum, longblob, and so on.
- Size: the number in brackets: varchar(20) -> 20, int(11) style display width -> 11, decimal(10,2) -> 10,2. Leave blank for date, time, datetime, timestamp, text, blobs. For enum put nothing in Size and list the allowed values in the Example cell as "one of: A, B, C" shortened if long.
- Attribute: the letters used in the picture, comma separated, in this order:
  PK = primary key, NN = NOT NULL, AI = AUTO_INCREMENT, FK = foreign key, AN = allows NULL.
  A column is either NN or AN, never both. Use information_schema.columns (IS_NULLABLE, COLUMN_KEY, EXTRA) and information_schema.key_column_usage (REFERENCED_TABLE_NAME) for the real foreign keys. If a column name ends in _id and clearly points at another table but has NO constraint (a "soft" foreign key), mark it FK as well and list those columns in the final report so I can see them.
- Example: one realistic value for that column taken from a REAL ROW OF THE DEMO DATABASE (see the safety rules). Pick the first row where the column is not NULL. Format like the picture: text in "double quotes", numbers plain, dates 2026-10-10, times 11:54:05, timestamps 2026-10-13 08:14:26. A column that is NULL in every demo row gets a made-up but obviously fictional value of the right type, and you list those columns in the final report. Binary / image columns (scans, photos, blobs): write (image) or (binary), never the bytes. A password hash column: write (hash, not shown).

DATA SOURCE AND SAFETY RULES (hard)
- Read structure and examples from the DEMO schema croms_demo only. Never read or write the live schema croms for this. The demo has fictional sample data (names are generated). Use Scripts\Environment\_Common.ps1 (Invoke-MySql, Assert-DisposableSchema) - the login is read from CROMS\App.config at run time; never write a password, connection string or key into any file, script, prompt or document.
- Read only: SELECT / information_schema statements only. Do not change the database, do not run migrations.
- Never put a real person's data in the document. If a row looks real (for example it came from a scan of the office's sample certificates), take the example from another row or invent a fictional one.
- Skip the views (names starting with v_) and the demo-only ledger table _env_migrations. That leaves the 60 real tables of the database (59 + deleted_records). Say in the final report if the count differs.

HOW TO BUILD IT
1. Write a small Python script Docs\DiagramTools\build_data_dictionary.py (keep it in the repo, beside gen_table_bubbles.py). Step 1 exports with PowerShell + Invoke-MySql into a TSV in the session scratchpad: columns (table, ordinal, name, column_type, data_type, is_nullable, column_key, extra), foreign keys, and one sample value per column (one query per table: SELECT the first non-null value of each column; do it table by table so a 100-column table does not take 100 round trips).
2. Build the .docx with the docx skill (docx-js or raw WordprocessingML; US Letter, Times New Roman, tables with dual widths as in the skill notes). One Heading above the first table: "Data Dictionary - CROMS database", a short intro paragraph, and a small legend table for the five attribute codes (PK, NN, AI, FK, AN).
3. Table order: first the tables a person types into (the order in TABLES of gen_table_bubbles.py: births, deaths, marriage_licenses, marriages, petitions, ...), then the system-written tables, as in that list. Number them Table 1 .. Table 60.
4. Verify: the number of rows in each Word table equals the number of columns in information_schema (script asserts it, and prints a per-table count). Total columns should be 988 for 60 tables on the current schema. Export through Word to PDF (Word COM from PowerShell; soffice is not installed), rasterise a few pages with PyMuPDF (pip install pymupdf) and LOOK at them: header row, pink text, borders, widths, no clipped text, long enum lists wrapped. Fix whatever looks wrong and look again. Check births, marriages (126 columns), a tiny lookup table such as provinces, and the last page.
5. Save the result as C:\Users\ivan palogan\Downloads\Final Data Dictionary.docx (if a file with that name is open in Word and locked, tell me instead of killing Word). Do not overwrite Final Data Entry.docx.

FINISH
- Add one Progress Log entry to CLAUDE.md: what was built, counts (tables, columns, soft FKs found, columns with no demo value), how it was verified, and what was NOT verified.
- Commit only your own files (the script, CLAUDE.md) with a clear message and push to origin/main. Never force-push. The Word file stays in Downloads, not in git.
- Final reply: the path of the document, number of tables and columns, the list of soft foreign keys, the list of columns that had no demo value (so I can check the invented examples), and anything skipped with the reason.

# CROMS — Ethics and Data Privacy Review

Date: 2026-10-08. Scope: the CROMS desktop app, kiosk, Display, phone capture (ORCMobile), claimapp, the `croms` database and this repository.
Method: read the code and config, queried the live `croms` database (grants, users, row counts), checked git history. Nothing was changed by this review.

CROMS has **no built-in "Ethics Review" module**. This document is the review itself. It is not legal advice: confirm with the LGU Peñablanca Data Protection Officer (DPO) and your adviser.

## 1. What data CROMS handles

Civil registry data is sensitive personal information under RA 10173 (Data Privacy Act): civil status, age, birth, death, marriage, parentage and, in this system, government ID types and numbers.

| Where | Data | Who it is about |
|---|---|---|
| `births`, `marriages`, `deaths` | full names, dates, places, parents, cause of death, scanned certificate images | citizens, incl. minors and deceased |
| `marriage_licenses` | both applicants, parents, consent/advice persons, residence | citizens |
| `queue_tickets` | full name, contact number, valid ID type/number, **face photo**, spouse photo, marriage-licence photo | clients at the kiosk (39 tickets, all with names) |
| `claim_requests`, `releases` | uploaded ID photo, claimant webcam photo, representative ID, authorization letter | claimants and representatives |
| `breqs_requests`, `ctc_requests` | requested record owner, parents, purpose, relationship | clients and record owners |
| `ocr_batch` | raw OCR text, source image, per-field audit | citizens |
| `users`, `audit_log` | staff accounts, password hashes, action trail (758 rows) | LCRO staff |

Biometric-adjacent: face photos at the kiosk and release window. Photos are stored, but CROMS never matches a face to an ID. That is a good design choice.

## 2. Where CROMS already does the right thing

- **Hashed passwords**: PBKDF2-HMAC-SHA256, 100,000 iterations, per-password salt (`Data/PasswordHasher.cs`).
- **Two roles only** (Admin, Staff). Settings, users, fee schedule, templates are Admin-only, enforced at the menu and in services.
- **Bypasses are accountable**: a Staff bypass of a document requirement re-asks the password, needs a written reason, and writes history plus `audit_log` with user and role.
- **Audit trail** of create, update, delete, login, logout, with the window the actor was signed in to.
- **Data minimisation** at the kiosk: the Certified True Copy request asks only enough to find the record; no parents' names on the kiosk.
- **No names on the public queue board**: tickets only. The office has been told this is a DPA question.
- **OCR is fully local** (Tesseract, own engine). No scan is sent to a cloud AI or third-party OCR.
- **Phone capture** uses a publicly trusted HTTPS name; the token, not personal data, is in the QR/URL. DuckDNS token is stored with Windows DPAPI.
- **No fabricated data**: OCR never fills an unread field, flags weak values, holds weak scans for manual review, and requires operator confirmation. That is an integrity safeguard for a legal register.
- **Parameterised SQL**; identifiers whitelisted.

## 3. Findings

### High

**H1. Database passwords are committed to git and pushed.**
`CROMS/App.config`, `CROMS.Kiosk/App.config`, `CROMS.Display/App.config` (and DocTest, MarriageTest) are tracked. They contain the MySQL **root** password, the `croms_user` password (`LanDbPassword`) and the share password (`ReleaseSharePassword`). History shows the file has carried them since at least commit `e9f7a55`. The remote is `github.com/ivanpalogan/CROMS08082026`; I could not check if it is public (`gh` is not installed). Treat all three as exposed.
Fix: rotate the root, `croms_user` and `cromsshare` passwords; stop tracking real config (`git rm --cached`, ship `App.config.example`); remove from history (`git filter-repo`) or make the repo private; load secrets from a per-machine file (the DPAPI store you built for DuckDNS is the model).

**H2. MySQL is open to the LAN with powerful accounts, unencrypted.**
Live `mysql.user`: `root@192.168.1.%`, `croms_user@%` plus several subnet rows, `mysql_ivan@%`. `croms_user` has `ALL PRIVILEGES` on `croms`. Connection strings use `SslMode=Disabled`, so names, IDs and scans cross the Wi-Fi in clear. Anyone on that network with the password can read or change the whole registry.
Fix: remove LAN `root`; give `croms_user` only SELECT/INSERT/UPDATE/DELETE (no DDL, no `DROP`) and a fixed host list; remove `mysql_ivan@%`; enable TLS (`SslMode=Required`) on the server and clients.

**H3. The audit log is not tamper-evident, though the docs say it is.**
`audit_log` has no hash chain, no signature and no database block on UPDATE/DELETE; `croms_user` can edit or delete any row. The test harnesses delete audit rows by design, and on 2026-09-13 a test cleanup deleted 8 real rows (recovered from the binlog). `CLAUDE.md` and `01_schema.sql` call it "tamper-evident". For a government register that claim must be true or removed.
Fix: either (a) a trigger that rejects UPDATE/DELETE on `audit_log` plus a separate restricted account for the test harness, or (b) a per-row hash chain (`prev_hash`, `row_hash`) checked in the Audit screen. Until then, reword the claim.

**H4. No privacy notice or consent at the kiosk.**
The kiosk collects name, mobile number, valid ID, a **webcam photo of the client**, and for marriage tickets a spouse photo and a licence photo. There is no notice of purpose, retention, who sees it, or the client's rights, and no consent step. RA 10173 transparency and consent/lawful-basis principles are not met on screen. (An LGU may rely on a statutory mandate for registry data, but the photo and contact data at the kiosk are extra.)
Fix: a one-screen notice before the first data step with an "I understand" button, a short printed notice at the window, and a line on the ticket. Draft wording is in section 6.

### Medium

**M1. No retention or disposal rule.** Nothing purges or archives kiosk tickets, photos, ID images, OCR source images or tokens. Ticket photos and ID uploads stay forever.
Fix: office decides periods (e.g. kiosk photos 30–90 days after the visit closes; claim ID images after release); add an admin purge job; record it in the privacy manual.

**M2. Reading personal data is not audited.** `Audit` actions are Create/Update/Delete/Login/Logout only; no "View". Any Staff user can open Records Archive and any record with no trace.
Fix: add a View action for record detail, scan/photo viewers, certificate preview/print; at least log certificate prints.

**M3. All Staff see all records.** One shared operational menu by design (office decision 2026-09-14). Acceptable for a small LCRO, but it means accountability rests on M2.

**M4. Real citizens' data is in the repository.** `CROMS.DocTest/Truth.cs` (names transcribed from the sample certificates), `CROMS.DocTest/accuracy_report.txt`, `CROMS.OwnOcr/WRITEUP.md/.docx` (the .docx embeds a real certificate crop; its own log entry says do not publish) are tracked and pushed. The scan images themselves are not tracked (checked by name; OwnOcr `data/` is git-ignored).
Fix: confirm with the office that the samples are de-identified or that the holders consented; if not, replace names with synthetic ones, remove the crop, purge from history.

**M5. The repo lives in OneDrive.** Everything in it, including `App.config` secrets, syncs to a Microsoft cloud account. The DB backup folder is outside OneDrive (`C:\Users\ivan palogan\CROMS_Backups`) but unencrypted.
Fix: keep secrets and backups out of synced folders; encrypt backups.

**M6. claimapp (public citizens' ID upload) runs over plain HTTP** on the LAN by deliberate choice (self-signed warning vs trust). The ID photo and name travel in clear on Wi-Fi.
Fix: move it to the same trusted-certificate name as Mobile Capture, or restrict that Wi-Fi to staff-controlled access.

**M7. Default share password** `Croms#2026` is in App.config and in the project log, and never expires by design.

### Low

- Login lockout (5 tries, 60 s) is in memory per running app: a restart resets it. Adequate for a LAN office; note it.
- First-run admin password: seed row is a placeholder until set; confirm the live admin password is not a default.
- Test windows and `ZZ*` rows are cleaned by tag; keep the real database separate from test runs.

## 4. Ethics of the research itself (capstone)

Check these before submitting:

1. **Written consent / agreement** with LGU Peñablanca for the interview, the use of the LCRO's forms and the demonstration (a letter or MOA, signed). The 2026-09-11 interview is recorded in `CROMS - Agency Requirements Backlog.md`; the participants should be named by role only, and consent should be on file.
2. **Sample certificates**: state whether they were de-identified, consented, or office-owned, and how they are stored and destroyed after the study (see M4).
3. **Testing with real staff or clients** needs a briefing and the right to decline; the kiosk must carry the notice from H4 if clients use it.
4. **AI/OCR limits disclosed**: accuracy on the office's own samples is about 61 % exact field values (113/184); handwriting is not read. State this plainly. Mitigation shipped: flagging and mandatory human confirmation.
5. **No harm from wrong data**: a wrong value in a civil register can affect identity, inheritance, legitimacy. Document the safeguards in section 2.
6. **Third-party components and licences**: Tesseract (Apache 2.0), Courier Prime (OFL), PSGC place data (public), Let's Encrypt, DuckDNS. Crystal Reports runtime is licensed by SAP; confirm the licence for deployment.

## 5. Suggested priority order

1. Rotate the three passwords; make the repo private or purge history (H1).
2. Lock down MySQL accounts; turn on TLS (H2).
3. Kiosk privacy notice (H4).
4. Block audit tampering or reword the claim (H3).
5. Add View auditing and a retention rule (M1, M2).
6. De-identify repo samples (M4).

Items 1–2 are your action (passwords, server grants). Items 3–6 I can build.

## 6. Draft kiosk privacy notice (for the office to approve)

> **Your information**
> The Local Civil Registry Office of Peñablanca collects your name, contact number, ID details and a photo to serve your request, call your queue number, and verify who receives a document. It is used only for this transaction and kept as the office's records policy allows. Only authorized LCRO staff can see it. You may ask to see, correct or ask about your information at the window. Under the Data Privacy Act (RA 10173) you have these rights.
> [ I understand, continue ]

DPO name and contact line to be added by the LGU.

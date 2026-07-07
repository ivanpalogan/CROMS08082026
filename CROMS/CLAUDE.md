CROMS — Civil Registry Operations Management System

Full name: Civil Registry Operations Management System Client: LGU Peñablanca, Cagayan — Local Civil Registry Office (LCRO) Type: Desktop application (Windows Forms / C# .NET Framework 4.8) Purpose: A day-to-day operations system for the LCRO — not just a digitization tool, but the everyday workbench that staff use to register events, serve clients, issue certifications, track petitions, and submit reports to PSA.



What it does (in one paragraph)

CROMS replaces the paper-and-spreadsheet workflow of the LCRO with a single integrated desktop app. Clients get a queue number at the front desk, the receiving officer logs the transaction, the registrar processes the record (birth, marriage, death, or certification request), the cashier assesses fees against the Peñablanca Revenue Code, and the releasing officer captures the claimant's signature — all tied to one transaction ID with a full audit trail. In parallel, the office digitizes decades of old registry books through the OCR module, and generates PSA monthly submission reports automatically from the same database.



The 15 modules

🟦 Client Services (front-desk workflow)

Module	What it does

Dashboard	Live KPIs — queue length, today's registrations, collections, pending releases.

Queue Management	Issues numbered tickets (BIRTH-###, CERT-###, etc.), calls next client, tracks wait time.

Transactions	Master ledger of every client interaction — one TXN ID follows the client from queue to release.

Certificate Request	Intake for CTC (Certified True Copy) of Birth/Marriage/Death and Negative Certifications.

Release \& Claim	Captures claimant signature (or authorized representative + ID) on release; closes the transaction.

🟩 Record Management (the registry itself)

Module	What it does

Birth Registration	Municipal Form 102 — full data entry for live-birth registration.

Marriage Registration	Municipal Form 97 — marriage certificate registration.

Death Registration	Municipal Form 103 — death certificate + burial permit.

Registry Books	Digital equivalent of the physical registry books, organized by year/volume/page.

Petitions (RA 9048 / 10172)	Tracks correction-of-entry and change-of-first-name petitions through their legal stages (Filed → Posted → Decision → PSA endorsement).

Record Search	Cross-record search with fuzzy / SOUNDEX matching (so "Dela Cruz" finds "dela cruz" and "de la Cruz").

🟨 Document Workflow

Module	What it does

Incoming / Outgoing	Document routing log — what came in, where it went, who signed for it.

OCR Digitization	Scans old registry pages, runs OCR, shows confidence levels, lets staff verify and commit to the database. This is how the backlog gets digitized without replacing the daily workflow.

🟧 Operations

Module	What it does

Fees \& Payments	Auto-assesses fees per the Peñablanca Revenue Code, reconciles Official Receipt numbers with the Treasurer, applies Senior/PWD discounts (RA 11261).

Reports \& PSA	Generates the PSA monthly submission report (BReN, MReN, DReN counts) and internal management reports.

🟥 Administration

Module	What it does

Users \& Audit Trail	User accounts, role-based access (Registrar / Staff / Cashier / Releasing), and a tamper-evident audit log of every create/update/delete.

Why it's designed this way

Realistic, not ambitious. It solves what LCRO staff actually do every day — queue, register, assess, collect, release, report — instead of chasing AI features they don't need.

Deployable. WinForms + .NET 4.8 + MySQL runs on any office PC the LGU already owns. No cloud dependency, no subscription.

Survives past digitization. Even after every old book is scanned, the app is still the daily operations system — that's the whole point.

PSA-compliant. Uses the official Municipal Form numbers (102/97/103) and produces the monthly report PSA expects.

Auditable. Every transaction has one ID, one owner, one signature trail — important for a government office.


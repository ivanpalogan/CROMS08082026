# Handoff — CROMS.Display (separate "Now Serving" board app)

A standalone, view-only program that shows the public **Now Serving** board for the
waiting area. It shares the **same MySQL database** as the main CROMS app — that shared
database is the only connection between the two programs (staff app writes tickets, the
display app reads them). Do the project/solution setup in **Claude Code / Visual Studio**
(I can't create `.csproj`/`.sln` files).

## Files already written (in `CROMS.Display/`)
- `Program.cs` — entry point, runs the board full screen.
- `DisplayForm.cs` — the board UI (3 windows, big ticket numbers, auto-refresh every 2s, Esc/double-click to close, clock, "waiting for connection" fallback).
- `Db.cs` — minimal read-only data access using the shared `Croms` connection string.
- `App.config` — the connection string (same DB as the main app).

## Set it up in Visual Studio (one time)

1. **Add the project.** Solution Explorer → right-click the **solution** → *Add → New Project* → **Windows Forms App (.NET Framework)** → name it **CROMS.Display**, framework **.NET Framework 4.7.2**, location = the repo root so it lands in the existing `CROMS.Display` folder.
2. **Delete** the auto-generated `Form1.cs`, `Form1.Designer.cs`, `Form1.resx`, and the generated `Program.cs` (you'll use the ones provided).
3. **Include the provided files.** In Solution Explorer, *Show All Files*, then right-click `Program.cs`, `DisplayForm.cs`, `Db.cs`, `App.config` → *Include In Project* (they're already on disk in `CROMS.Display/`).
4. **Add MySQL.** Right-click the CROMS.Display project → *Manage NuGet Packages* → install **MySql.Data** (use the same version as the main app — 9.7.0 — to keep behavior identical). NuGet pulls its dependencies automatically.
5. **Build.** This produces `CROMS.Display.exe` in `CROMS.Display\bin\Debug\`.

## Running it as a connected system
- On the **staff PC**: run the main `CROMS.exe` as usual.
- On the **waiting-area PC/TV**: run `CROMS.Display.exe`. It boots straight into the full-screen board.
- Both must reach the **same MySQL database**. If the display PC is *not* the database server, edit `CROMS.Display/App.config` → change `server=localhost` to the DB server's IP (e.g. `server=192.168.1.10`) and make sure MySQL accepts remote connections and the firewall allows port 3306.

## Notes
- The board is purely read-only — no admin surface is exposed to the public screen.
- The in-app **Client Display** button still exists inside the main app (opens the same board in a window). You can keep it as a convenience or remove it now that a dedicated app exists — your call.
- Security: the connection string contains the DB password (same as the main app). For a real deployment, use a dedicated **read-only** MySQL user for the display app instead of `root`.

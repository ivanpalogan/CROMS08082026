# CROMS — How to Run (which app goes where)

CROMS is **three separate programs** that share one MySQL database (`croms`).
They are meant for **three different machines/screens** — never all on one monitor.
If you see the kiosk, the login page, and the "Now Serving" board piled on top of
each other, it's because Visual Studio is launching all three at once (see
[Development](#development-visual-studio) below) — not a bug.

---

## The three apps

| Program | Build output | Runs on | For whom |
|---|---|---|---|
| **CROMS** | `CROMS\bin\Debug\CROMS.exe` | Staff / admin workstation (your laptop) | Admin, Registrar, Cashier, Releasing, Staff |
| **CROMS.Kiosk** | `CROMS.Kiosk\bin\Debug\CROMS.Kiosk.exe` | Client self-service terminal by the entrance | Walk-in clients (get a queue number) |
| **CROMS.Display** | `CROMS.Display\bin\Debug\CROMS.Display.exe` | Waiting-area TV / second monitor | The public (view-only "Now Serving" board) |

**An admin only needs `CROMS.exe`.** The kiosk and display are public-facing and
should not open on the admin's screen. Each machine runs only the one app it needs.

---

## Development (Visual Studio)

By default a fresh clone may run **all three** projects on F5, which stacks every
window on the primary screen. Fix it once:

1. Right-click the **Solution** (`CROMS.sln`) in Solution Explorer.
2. **Set Startup Projects…**
3. Choose **Single startup project → `CROMS`**.
4. OK. Now F5 opens only the main app.

To test the kiosk or the display, right-click that project → **Debug → Start New
Instance** (or run its `.exe` from `bin\Debug`).

> This setting is stored per-machine in the `.suo` file (not in source control),
> so each person who opens the solution sets it once.

---

## Deployment (real office setup)

Copy each program's `bin\Debug` (or `bin\Release`) folder to the machine that
needs it and run its `.exe`:

- **Admin / staff PC** → `CROMS.exe`
- **Client kiosk PC** → `CROMS.Kiosk.exe` (put a shortcut in Startup so it opens on boot)
- **Waiting-area PC/TV** → `CROMS.Display.exe`

All three read the database connection from their own `App.config`
(`connectionStrings` → `Croms`). Point every copy at the same MySQL server so a
ticket made at the kiosk shows instantly on the staff app and the display board.

### Second monitor (Display / Kiosk)

Only relevant when a single machine has two screens (e.g. the display TV is a
second monitor on the kiosk PC). Currently the Display board opens **maximized on
the primary screen**. To move it to the second monitor, either:

- **Manual:** drag the window to the second monitor, then it stays maximized there
  the next time (Windows remembers per-app), **or**
- Ask to add **auto-placement + an App.config monitor override** (not built yet —
  the code snippet is ready if you want it).

---

## First sign-in

- **Username:** `admin`
- **Password:** `admin123`

On first login the app **forces you to set a new password** (minimum 8
characters) before it opens — this retires the default credential. After that,
create the real staff accounts in **Administration → Users & Audit Trail**
(admin-only; there is intentionally **no public self-registration**). An admin
password reset also forces that user to choose their own password on next login.

---

## Build from the command line (optional)

```powershell
# Main app (needs the WinForms resx architecture flag under the dotnet/MSBuild SDK)
msbuild CROMS\CROMS.csproj /p:Configuration=Debug /p:GenerateResourceMSBuildArchitecture=CurrentArchitecture

# Kiosk
msbuild CROMS.Kiosk\CROMS.Kiosk.csproj /p:Configuration=Debug

# Display
msbuild CROMS.Display\CROMS.Display.csproj /p:Configuration=Debug
```

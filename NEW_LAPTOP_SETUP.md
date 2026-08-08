# CROMS — Set Up on a New Laptop

How to run CROMS on a **new client laptop**. No MySQL install needed on the client —
it connects over Wi-Fi to the **server PC** (the one that has the database).

> **Server PC** = the main computer that holds MySQL + the `croms` database (your dev machine).
> **Client laptop** = any other office laptop that just runs CROMS and shares the same database.

---

## Before you start (server PC — do once each day you serve clients)

1. Turn ON the server PC and make sure **MySQL is running**.
2. Put the server + all client laptops on the **SAME Wi-Fi**.
   - Best/most reliable: turn ON the server's **Mobile Hotspot**
     (Settings → Network & Internet → Mobile hotspot → On), and connect the
     client laptops to THAT. Hotspot always gives the server the fixed IP
     `192.168.137.1`, so it works even with no internet.
   - Home/office Wi-Fi also works, but some routers block PC-to-PC — if the
     client can't see the server, switch to the Mobile Hotspot.

---

## A. Brand-new laptop — first install

1. **Copy the setup file** onto the new laptop.
   - File: **`CROMS_Setup_SmartConnect.zip`** (in this project folder).
   - Copy it with a USB stick, or download it from the shared folder.

2. **Extract it.** Right-click the zip → **Extract All** → pick a simple place like
   `C:\CROMS`. You should end up with a folder that has `Main`, `Kiosk`, `Display`
   inside (or a `CROMS.exe` — see note below).

3. **Run it.** Open the `Main` folder → double-click **`CROMS.exe`**.
   - First launch drops a **CROMS** shortcut on the Desktop — use that next time.

4. **Connect to the server.** The first time, CROMS looks for the server:
   - If it finds it automatically → it just opens. Done.
   - If it asks, click **🔍 Find Server Automatically** and wait — it scans the
     Wi-Fi for the server. When it shows the IP, click **Save & Continue**.
   - If auto-find fails, type the server IP by hand (ask the server PC — run
     `ipconfig`, look for the Wi-Fi `IPv4 Address`, e.g. `192.168.1.81`, or
     `192.168.137.1` if using the hotspot), then **Save & Continue**.

5. **Log in.** First account: username **`admin`**, password **`admin123`**.
   - It forces you to set a new password on first login. **Change it and keep it safe.**

That's it — the new laptop now shares the same live database as the server.

> **Note on layout:** the client usually only needs the **Main** app (staff work).
> The **Kiosk** app is for the client-facing queue PC; **Display** is for the
> waiting-area TV. On a plain staff laptop, just run `Main\CROMS.exe`.

---

## B. Already installed — get the latest version (update)

When you make a new build, each client laptop can pull it with ONE click.

**On the server PC (publish the new build):**
- Open CROMS → **Settings → App Updates** → **Publish New Release to Clients**
  (approve the admin prompt). This shares the latest build over the Wi-Fi.

**On each client laptop (install the update):**
- Open CROMS → click the **⟳ Update** button (top bar), OR
  **Settings → App Updates → Check for Updates & Install**.
- It downloads the new build and restarts itself. You stay logged in.

If Update says **"not found"**: the client can't see the server's share.
Check it's on the SAME Wi-Fi as the server, then in File Explorer type
`\\<server-ip>\CROMSRelease` (e.g. `\\192.168.1.81\CROMSRelease`) — it should open.
If it doesn't, switch to the server's **Mobile Hotspot** (see top of this file).

---

## Quick reference

| Thing | Value |
|---|---|
| Setup file for a new laptop | `CROMS_Setup_SmartConnect.zip` |
| Server Wi-Fi IP (example) | `192.168.1.81` (check with `ipconfig`) |
| Server Hotspot IP (fixed) | `192.168.137.1` |
| First login | `admin` / `admin123` (forces password change) |
| Update share | `\\<server-ip>\CROMSRelease` |
| Server publishes update | Settings → App Updates → Publish New Release |
| Client installs update | ⟳ Update button (or Settings → App Updates) |

**Requirement:** client and server must be on the **same Wi-Fi / hotspot**.
The database link is local, so it works **with or without internet**.

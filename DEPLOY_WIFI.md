# CROMS — Wireless LAN Deployment (share one database over Wi-Fi)

Run CROMS on 2+ PCs sharing the one `croms` database, over Wi-Fi, working
**with or without internet** (the database link is local — internet optional).

- **Server PC** = this dev machine. Hosts MySQL + the `croms` database. Also runs CROMS.
- **Client PC(s)** = any other office PC. Runs only CROMS.exe, connects to the server over Wi-Fi.

Server address used below: **`192.168.137.1`** = the server PC's **Mobile Hotspot** IP (fixed).
(Using a Wi-Fi router instead? Use the server's Wi-Fi IP `192.168.1.100` and reserve it in the router.)

---

## A. Server PC — one-time setup ✅ (already done)

These are already configured on this machine:

- [x] MySQL 9.3 running, `croms` database present.
- [x] LAN user created: **`croms_user`** / password **`Croms#2026`** (subnets `192.168.137.%` and `192.168.1.%`).
- [x] Firewall port 3306 open.
- [x] MySQL listening on all interfaces (`0.0.0.0:3306`).

**Each time you want to serve clients, just turn the hotspot ON:**

1. Windows **Settings → Network & Internet → Mobile hotspot → On**.
   (Note the network name + password shown — clients need it.)
2. Leave MySQL + the server PC running.

> Security: change the default `croms_user` password before real office use. In MySQL:
> ```sql
> ALTER USER 'croms_user'@'192.168.137.%' IDENTIFIED BY 'YourNewPass';
> ALTER USER 'croms_user'@'192.168.1.%'   IDENTIFIED BY 'YourNewPass';
> FLUSH PRIVILEGES;
> ```
> Then update the password in each client's `CROMS.exe.config`.

---

## B. Client PC — install CROMS

1. **Copy the app folder.** From the server PC, copy the entire folder:
   `CROMS\bin\Debug`  → onto the client PC (USB stick, or shared folder).
   Put it somewhere like `C:\CROMS`.
   *(No MySQL install needed on the client — the `MySql.Data.dll` in the folder is all it needs.)*

2. **Drop in the client config.** Copy `CROMS.client.config` (in the repo root) into
   that folder, **rename it to `CROMS.exe.config`**, overwriting the existing one.
   - It already points at `server=192.168.137.1` with `croms_user` / `Croms#2026`.
   - It sets `IonicAutoStart=false` so the client won't try to launch the phone-scanner services.

3. **Join the Wi-Fi.** On the client PC, connect to the server's Mobile Hotspot
   (the network name/password from step A).

4. **Run it.** Double-click `CROMS.exe`. Log in (`admin` / `admin123` on first run → it forces a password change).

That's it. The client now shares the same live database as the server.

---

## C. Test the connection (before running the app)

On the **client PC**, open Command Prompt and ping the server:

```
ping 192.168.137.1
```

If replies come back, the network is good. If the app still can't connect:

- Confirm the client is on the **server's** hotspot (not another Wi-Fi).
- Confirm the server PC's hotspot + MySQL are ON.
- Re-check the IP in `CROMS.exe.config` matches the server's hotspot IP
  (on the server run `ipconfig` and look for the `192.168.137.1` adapter).

---

## D. How online / offline works

- **Offline (no internet):** the hotspot still creates a local network. PCs talk to
  each other, the database works. ✅
- **Online (internet present):** same setup, database still works. Internet changes nothing
  for CROMS — the database is always reached locally.

---

## E. Reliability notes (for a real office)

- **Wi-Fi can drop mid-save.** For a government record office, prefer wiring the **server PC**
  to a router with an Ethernet cable, and let only the client PCs be wireless. Mixed is fine
  and is the most stable arrangement.
- Put the **server PC on a UPS** (battery backup) — a power cut mid-transaction is the main
  corruption risk.
- If the **server PC is off**, the shared database is unavailable to everyone (same as any
  client-server system). Keep it on during office hours.
- Take **daily backups** of the `croms` database (see the main architecture notes).

---

## Quick reference

| Thing | Value |
|---|---|
| Server hotspot IP | `192.168.137.1` |
| Server Wi-Fi IP (router option) | `192.168.1.100` |
| DB name | `croms` |
| DB user | `croms_user` |
| DB password | `Croms#2026` (change before real use) |
| Port | `3306` |
| App login | `admin` / `admin123` (first run forces change) |

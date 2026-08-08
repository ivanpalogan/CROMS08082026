# CROMS — install on another laptop (smart-connect build)

This build **asks for the server IP on first run and remembers it** — no config
editing, ever. Works on a hotspot or any Wi-Fi.

## 1. On the SERVER PC (the one with the database)
- Turn on the network the other laptop will use: **Mobile hotspot**, or just be on
  the **same Wi-Fi router**.
- Keep this PC on and MySQL running whenever others use CROMS.

## 2. On the OTHER laptop
1. Extract this zip to a folder, e.g. `C:\CROMS`.
2. Join the **same Wi-Fi / hotspot** as the server PC.
3. Run **`CROMS.exe`** (a **CROMS icon is also placed on your desktop** on first run).
4. First time only: a **"Connect to CROMS Server"** window appears.
   - Type the server PC's IP address.
     - Hotspot: usually `192.168.137.1`
     - Same Wi-Fi router: the server PC's Wi-Fi IP (run `ipconfig` on it to see)
   - Click **Test Connection** → should say "Connected successfully".
   - Click **Save & Continue**.
5. Log in: `admin` / `admin123` (first login forces a new password).

That's it. Next time you just double-click the **CROMS** desktop icon — it
remembers the server. If the server's IP ever changes, CROMS notices it can't
connect and simply asks for the new IP again.

## Finding the server IP
On the SERVER PC: `Win+R` → `cmd` → `ipconfig` → look for the **IPv4 Address**
of the Wi-Fi (or the `192.168.137.1` hotspot adapter).

## If "Connect to Server" can't connect
- Is the server PC on, with MySQL running and hotspot/Wi-Fi up?
- Is this laptop on the **same** network as the server?
- Did you type the correct IP? Re-check with `ipconfig` on the server.

## Notes
- No MySQL install needed on this laptop — the driver is bundled.
- Needs .NET Framework 4.8 (already on Windows 10/11).
- Default DB login user is `croms_user` — change its password before real office
  use (ask your admin).

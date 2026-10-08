-- CROMS: lock down MySQL accounts (Data Privacy Act, RA 10173 Sec. 28).
-- Run ONCE on the server laptop, as root, AFTER the new CROMS / Kiosk / Display builds are
-- installed on every PC (old builds connect with SslMode=Disabled and will be refused).
--
--   mysql -uroot -p < Scripts\lock_down_mysql_accounts.sql
--
-- BEFORE running: save the current accounts so this can be undone (keep the file OUT of git,
-- it contains password hashes):
--   mysql -uroot -p -N -B -e "SELECT CONCAT('SHOW CREATE USER ',QUOTE(user),'@',QUOTE(host),';') FROM mysql.user WHERE user IN ('croms_user','mysql_ivan') OR (user='root' AND host<>'localhost')" > show_users.sql
--   mysql -uroot -p -N -B < show_users.sql > users_before.txt
--   (repeat with SHOW GRANTS FOR ... for the same accounts)
--
-- What it does
--   1. Removes root from the LAN (root stays on localhost only).
--   2. Removes mysql_ivan@% (a global-superuser account open to any host).
--   3. Removes croms_user hosts that are not the office networks (%, 10.%, 172.%, 192.168.%,
--      192.168.1.205, the placeholder row). Kept: 192.168.1.% and 192.168.137.% (hotspot).
--      Add another subnet only if the office really uses one.
--   4. croms_user gets SELECT, INSERT, UPDATE, DELETE on croms.* and nothing else: no
--      DDL/DROP, no GRANT OPTION. The apps never run DDL (checked); migrations are run by
--      root on the server.
--   5. croms_user must connect over TLS (REQUIRE SSL).

DROP USER IF EXISTS 'root'@'192.168.1.%';
DROP USER IF EXISTS 'mysql_ivan'@'%';
DROP USER IF EXISTS 'croms_user'@'%';
DROP USER IF EXISTS 'croms_user'@'10.%';
DROP USER IF EXISTS 'croms_user'@'172.%';
DROP USER IF EXISTS 'croms_user'@'192.168.%';
DROP USER IF EXISTS 'croms_user'@'192.168.1.205';
DROP USER IF EXISTS 'croms_user'@'<real_ip_or_subnet>';

REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'croms_user'@'192.168.1.%';
REVOKE ALL PRIVILEGES, GRANT OPTION FROM 'croms_user'@'192.168.137.%';
GRANT SELECT, INSERT, UPDATE, DELETE ON croms.* TO 'croms_user'@'192.168.1.%';
GRANT SELECT, INSERT, UPDATE, DELETE ON croms.* TO 'croms_user'@'192.168.137.%';
ALTER USER 'croms_user'@'192.168.1.%'   REQUIRE SSL;
ALTER USER 'croms_user'@'192.168.137.%' REQUIRE SSL;
FLUSH PRIVILEGES;

-- Check
SELECT user, host, ssl_type FROM mysql.user WHERE user NOT LIKE 'mysql.%' ORDER BY user, host;
SHOW GRANTS FOR 'croms_user'@'192.168.1.%';

-- OPTIONAL, later, once every PC runs the new build and CROMS has been restarted:
--   SET PERSIST require_secure_transport = ON;
-- It also forces TLS for localhost. Not done here because the already-running CROMS and the
-- phone API would lose their next new connection until restarted. Undo: SET PERSIST
-- require_secure_transport = OFF;

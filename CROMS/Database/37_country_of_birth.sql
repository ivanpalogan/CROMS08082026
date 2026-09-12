-- 37_country_of_birth.sql
-- Country on every place of birth CROMS records.
--
-- Asked for by the LCRO at the 2026-09-11 pre-checkup: the office serves applicants born
-- outside the Philippines and foreign nationals, and a Philippines-only address shape
-- cannot record where they were born. Place of birth had to stop being a bare textbox and
-- start carrying a country.
--
-- COUNTRY IS ITS OWN COLUMN, NOT PART OF THE JOINED PLACE STRING. `births.place_of_birth`
-- stores "Hospital, Province, Municipality" as one comma-joined value and is split back on
-- the comma when a record loads (BirthRegistrationForm.SetPlace3). Appending a country to
-- that string would re-split every row already written into the wrong three cells. The same
-- reasoning kept place-of-death's stored order fixed when its boxes were reordered on
-- 2026-09-10.
--
-- Existing rows stay NULL rather than being backfilled to 'Philippines'. Almost all of them
-- ARE Philippine births, but "almost all" is not a fact about any particular record, and a
-- country written into a registry entry that never stated one is a fabricated entry. The
-- screen shows Philippines as the default for a NEW record; a loaded record shows what it
-- actually holds.
--
-- ASCII SPELLINGS ARE DELIBERATE. These migrations are applied by piping the file into the
-- mysql client, which decodes it with the console code page -- that is how the enye in
-- "Penablanca" was stored as two box-drawing characters on 2026-09-06. Every name here is
-- ASCII ("Cote d'Ivoire", "Turkiye", "Sao Tome and Principe") so no client charset can
-- reinterpret it. The office can correct any spelling in Master Files; unlike their own
-- municipality, a country name here is a picklist value, not a value printed on their forms.
--
-- Idempotent: guarded ADD COLUMN through a throwaway procedure (MySQL has no ADD COLUMN IF
-- NOT EXISTS), and the seed only inserts a name that is not already present.

CREATE TABLE IF NOT EXISTS `countries` (
  `id`   INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(80) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ux_countries_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Seed. Each name is inserted only when absent, so re-running adds nothing and a name the
-- office has edited or removed is not silently restored.
INSERT IGNORE INTO `countries` (`name`) VALUES
  ('Philippines'),
  ('Afghanistan'),
  ('Albania'),
  ('Algeria'),
  ('Andorra'),
  ('Angola'),
  ('Antigua and Barbuda'),
  ('Argentina'),
  ('Armenia'),
  ('Australia'),
  ('Austria'),
  ('Azerbaijan'),
  ('Bahamas'),
  ('Bahrain'),
  ('Bangladesh'),
  ('Barbados'),
  ('Belarus'),
  ('Belgium'),
  ('Belize'),
  ('Benin'),
  ('Bhutan'),
  ('Bolivia'),
  ('Bosnia and Herzegovina'),
  ('Botswana'),
  ('Brazil'),
  ('Brunei'),
  ('Bulgaria'),
  ('Burkina Faso'),
  ('Burundi'),
  ('Cabo Verde'),
  ('Cambodia'),
  ('Cameroon'),
  ('Canada'),
  ('Central African Republic'),
  ('Chad'),
  ('Chile'),
  ('China'),
  ('Colombia'),
  ('Comoros'),
  ('Congo (Republic)'),
  ('Congo (Democratic Republic)'),
  ('Costa Rica'),
  ('Cote d''Ivoire'),
  ('Croatia'),
  ('Cuba'),
  ('Cyprus'),
  ('Czechia'),
  ('Denmark'),
  ('Djibouti'),
  ('Dominica'),
  ('Dominican Republic'),
  ('Ecuador'),
  ('Egypt'),
  ('El Salvador'),
  ('Equatorial Guinea'),
  ('Eritrea'),
  ('Estonia'),
  ('Eswatini'),
  ('Ethiopia'),
  ('Fiji'),
  ('Finland'),
  ('France'),
  ('Gabon'),
  ('Gambia'),
  ('Georgia'),
  ('Germany'),
  ('Ghana'),
  ('Greece'),
  ('Grenada'),
  ('Guatemala'),
  ('Guinea'),
  ('Guinea-Bissau'),
  ('Guyana'),
  ('Haiti'),
  ('Honduras'),
  ('Hungary'),
  ('Iceland'),
  ('India'),
  ('Indonesia'),
  ('Iran'),
  ('Iraq'),
  ('Ireland'),
  ('Israel'),
  ('Italy'),
  ('Jamaica'),
  ('Japan'),
  ('Jordan'),
  ('Kazakhstan'),
  ('Kenya'),
  ('Kiribati'),
  ('Korea (North)'),
  ('Korea (South)'),
  ('Kuwait'),
  ('Kyrgyzstan'),
  ('Laos'),
  ('Latvia'),
  ('Lebanon'),
  ('Lesotho'),
  ('Liberia'),
  ('Libya'),
  ('Liechtenstein'),
  ('Lithuania'),
  ('Luxembourg'),
  ('Madagascar'),
  ('Malawi'),
  ('Malaysia'),
  ('Maldives'),
  ('Mali'),
  ('Malta'),
  ('Marshall Islands'),
  ('Mauritania'),
  ('Mauritius'),
  ('Mexico'),
  ('Micronesia'),
  ('Moldova'),
  ('Monaco'),
  ('Mongolia'),
  ('Montenegro'),
  ('Morocco'),
  ('Mozambique'),
  ('Myanmar'),
  ('Namibia'),
  ('Nauru'),
  ('Nepal'),
  ('Netherlands'),
  ('New Zealand'),
  ('Nicaragua'),
  ('Niger'),
  ('Nigeria'),
  ('North Macedonia'),
  ('Norway'),
  ('Oman'),
  ('Pakistan'),
  ('Palau'),
  ('Palestine'),
  ('Panama'),
  ('Papua New Guinea'),
  ('Paraguay'),
  ('Peru'),
  ('Poland'),
  ('Portugal'),
  ('Qatar'),
  ('Romania'),
  ('Russia'),
  ('Rwanda'),
  ('Saint Kitts and Nevis'),
  ('Saint Lucia'),
  ('Saint Vincent and the Grenadines'),
  ('Samoa'),
  ('San Marino'),
  ('Sao Tome and Principe'),
  ('Saudi Arabia'),
  ('Senegal'),
  ('Serbia'),
  ('Seychelles'),
  ('Sierra Leone'),
  ('Singapore'),
  ('Slovakia'),
  ('Slovenia'),
  ('Solomon Islands'),
  ('Somalia'),
  ('South Africa'),
  ('South Sudan'),
  ('Spain'),
  ('Sri Lanka'),
  ('Sudan'),
  ('Suriname'),
  ('Sweden'),
  ('Switzerland'),
  ('Syria'),
  ('Taiwan'),
  ('Tajikistan'),
  ('Tanzania'),
  ('Thailand'),
  ('Timor-Leste'),
  ('Togo'),
  ('Tonga'),
  ('Trinidad and Tobago'),
  ('Tunisia'),
  ('Turkiye'),
  ('Turkmenistan'),
  ('Tuvalu'),
  ('Uganda'),
  ('Ukraine'),
  ('United Arab Emirates'),
  ('United Kingdom'),
  ('United States'),
  ('Uruguay'),
  ('Uzbekistan'),
  ('Vanuatu'),
  ('Vatican City'),
  ('Venezuela'),
  ('Vietnam'),
  ('Yemen'),
  ('Zambia'),
  ('Zimbabwe'),
  ('Hong Kong'),
  ('Macau'),
  ('Puerto Rico'),
  ('Guam'),
  ('Northern Mariana Islands'),
  ('Bermuda'),
  ('Greenland'),
  ('Kosovo');

DROP PROCEDURE IF EXISTS _croms_country_of_birth;
DELIMITER //
CREATE PROCEDURE _croms_country_of_birth()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'births' AND COLUMN_NAME = 'birth_country') THEN
        ALTER TABLE `births`
            ADD COLUMN `birth_country` VARCHAR(80) NULL AFTER `place_of_birth`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = 'husband_birth_country') THEN
        ALTER TABLE `marriage_licenses`
            ADD COLUMN `husband_birth_country` VARCHAR(80) NULL AFTER `husband_place_of_birth`;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE()
                   AND TABLE_NAME = 'marriage_licenses' AND COLUMN_NAME = 'wife_birth_country') THEN
        ALTER TABLE `marriage_licenses`
            ADD COLUMN `wife_birth_country` VARCHAR(80) NULL AFTER `wife_place_of_birth`;
    END IF;
END //
DELIMITER ;

CALL _croms_country_of_birth();
DROP PROCEDURE IF EXISTS _croms_country_of_birth;

-- NOT DONE HERE, on purpose: `v_birth_certificate` is not restated to carry
-- `birth_country`. That view is the datasource a printed certificate binds to, and
-- extending it means restating all ~71 of its columns -- a large, easy-to-get-wrong edit
-- that belongs with the print-map work, not with capturing the field. Until then the
-- country is stored and shown on the registration screen but does not print. Restate the
-- view (the definition lives in 26_form_identity.sql, extended by 28_certification_block.sql)
-- when the certificate layout is next touched.

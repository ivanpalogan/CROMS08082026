-- 16_learning_library.sql
-- Smart Learning Library: a single shared reference store of frequently used values
-- (places, hospitals, municipalities, churches, cemeteries, names, nationalities,
-- occupations, ...). Document AI auto-learns new values from every scan (using the
-- operator-corrected value), and every module reads it for autocomplete suggestions.
--
-- `normalized` is a case/diacritic-folded key used only for duplicate detection, so
-- "Peñablanca", "PENABLANCA" and "penablanca" collapse to one row. The human-facing
-- `value` keeps its proper spelling. `usage_count` lets the UI rank common values first.

CREATE TABLE IF NOT EXISTS `reference_library` (
  `id`          INT AUTO_INCREMENT PRIMARY KEY,
  `category`    VARCHAR(40)  NOT NULL,
  `value`       VARCHAR(255) NOT NULL,
  `normalized`  VARCHAR(255) NOT NULL,
  `usage_count` INT          NOT NULL DEFAULT 1,
  `created_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE KEY `uq_cat_norm` (`category`, `normalized`),
  KEY `ix_cat_val` (`category`, `value`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 50_certificate_templates.sql
-- Runtime-editable certificate/report templates, so a non-technical LCRO staff member
-- can move a logo, resize a field, or add a line on a certificate WITHOUT editing C#
-- source or a Crystal .rpt.
--
-- Two tables:
--   certificate_templates       one "current" (is_active=1) row and one "default"
--                                (is_default=1) row per form_code. The default is the
--                                office's original layout (seeded from the existing
--                                hardcoded C# cell lists the first time a form is opened
--                                in the designer) and is never overwritten by editing —
--                                it exists purely so "Restore Default" has something to
--                                restore. The active row is what actually prints/previews.
--   certificate_template_images uploaded pictures (logos, seals, stamps, banners) that a
--                                template's Image elements reference. Scoped to the
--                                template row that owns them, so removing a template
--                                cannot orphan someone else's upload.
--
-- Idempotent: CREATE TABLE IF NOT EXISTS, safe to re-run.

CREATE TABLE IF NOT EXISTS certificate_templates (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    form_code     VARCHAR(60)  NOT NULL,
    name          VARCHAR(120) NOT NULL,
    page_width    FLOAT        NOT NULL DEFAULT 612,
    page_height   FLOAT        NOT NULL DEFAULT 792,
    orientation   VARCHAR(10)  NOT NULL DEFAULT 'Portrait',
    elements_json LONGTEXT     NOT NULL,
    is_default    TINYINT(1)   NOT NULL DEFAULT 0,
    is_active     TINYINT(1)   NOT NULL DEFAULT 0,
    updated_by    INT          NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    KEY idx_form_code (form_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS certificate_template_images (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    template_id   INT          NOT NULL,
    label         VARCHAR(80)  NULL,
    content_type  VARCHAR(50)  NOT NULL DEFAULT 'image/png',
    image_data    LONGBLOB     NOT NULL,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_tpl_img_template FOREIGN KEY (template_id)
        REFERENCES certificate_templates(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

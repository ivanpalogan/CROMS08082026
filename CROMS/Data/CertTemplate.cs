using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.Data
{
    /// <summary>
    /// One placeable thing on a certificate template. Positions are in POINTS
    /// (1/72 in) from the page's top-left, matching the coordinate space every
    /// existing built-in printer in this app already uses
    /// (<see cref="CertificateReport"/>, <see cref="Form3ACert"/>) — so a template
    /// designed here prints through the exact same rendering math as the code-built
    /// layouts it replaces.
    /// </summary>
    public class TemplateElement
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Kind = "Text";     // Text | Field | Image | Line | Rectangle
        public string Band = "Body";     // Header | Body | Footer — organizational only
        public int ZIndex;

        public float X, Y, Width = 120f, Height = 16f;

        // Text (literal) / Field (dynamic — Column names a key in the form's data row)
        public string Text = "";
        public string Column;
        public string FontFamily = "Arial";
        public float FontSize = 9f;
        public bool Bold, Italic, Underline;
        public string Align = "Left";    // Left | Center | Right

        // Image — exactly one of the three is set. OfficeAsset points at a named,
        // office-wide slot (the logo/seal/stamp managed in Settings); ImageId points
        // at a picture uploaded straight into THIS template; BundledImage names a fixed
        // artwork file shipped with the app under Assets\ (a reference graphic that is
        // part of the form's own content, not office branding the office would replace).
        public string OfficeAsset;
        public int? ImageId;
        public string BundledImage;
        public bool LockAspect = true;

        // Line / Rectangle
        public float StrokeWidth = 1f;

        public bool Locked;

        public TemplateElement Clone()
        {
            return (TemplateElement)MemberwiseClone();
        }
    }

    /// <summary>
    /// One certificate's whole printable layout: page geometry + every element on it.
    /// This is what the designer edits and what the renderer draws — the same object,
    /// so what the operator sees while designing is what prints.
    /// </summary>
    public class CertTemplate
    {
        public string FormCode;
        public string Name = "";
        public float PageWidth = 612f;   // Letter, points
        public float PageHeight = 792f;
        public string Orientation = "Portrait";
        public List<TemplateElement> Elements = new List<TemplateElement>();

        public CertTemplate Clone()
        {
            var t = new CertTemplate
            {
                FormCode = FormCode,
                Name = Name,
                PageWidth = PageWidth,
                PageHeight = PageHeight,
                Orientation = Orientation,
                Elements = Elements.Select(e => e.Clone()).ToList()
            };
            return t;
        }

        /// <summary>Every distinct Field key this template actually places on the page,
        /// in Z order — used to build the "what's already used" hint in the field
        /// picker, never to restrict what CAN be added.</summary>
        public IEnumerable<string> UsedFieldKeys =>
            Elements.Where(e => e.Kind == "Field" && !string.IsNullOrEmpty(e.Column))
                    .Select(e => e.Column).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>One field a template CAN place: the key the renderer reads off the data
    /// row, plus a friendly label for the "Add Field" picker. Never database identifiers
    /// shown to the operator — always the label.</summary>
    public class TemplateFieldOption
    {
        public string Key;
        public string Label;
        public string Group;
        public TemplateFieldOption(string key, string label, string group)
        { Key = key; Label = label; Group = group; }
    }
}

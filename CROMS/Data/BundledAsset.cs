using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace CROMS.Data
{
    /// <summary>
    /// Loads a fixed artwork file shipped under <c>Assets\</c> — content that is part of a
    /// form itself (a reference chart, a diagram) rather than office branding the office
    /// would ever replace, which is what <see cref="OfficeAssets"/> is for. Cached per
    /// filename for the life of the process, same reasoning as <see cref="OfficeAssets"/>'s
    /// own cache: a certificate print asks for the same picture on every page.
    /// </summary>
    public static class BundledAsset
    {
        private static readonly Dictionary<string, Image> _cache =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        public static Image Load(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            if (_cache.TryGetValue(fileName, out Image cached)) return cached;

            Image img = null;
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    using (var ms = new MemoryStream(bytes))
                    using (var loaded = Image.FromStream(ms))
                        img = new Bitmap(loaded);
                }
            }
            catch { img = null; }

            _cache[fileName] = img;
            return img;
        }
    }
}

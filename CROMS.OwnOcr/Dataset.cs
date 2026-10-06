using System;
using System.Collections.Generic;
using System.IO;

namespace CROMS.OwnOcr
{
    /// <summary>One labelled character picture, stored compactly (a byte per pixel).</summary>
    public sealed class Sample
    {
        public ushort Label;                 // index into Charset
        public byte[] Image;                 // GlyphCell.Size ^ 2, 0 = paper, 255 = ink
        public float RelW, RelTop, RelBottom, Aspect;

        public static Sample FromCell(GlyphCell c, int label)
        {
            var img = new byte[c.Image.Length];
            for (int i = 0; i < img.Length; i++)
            {
                float v = c.Image[i];
                img[i] = (byte)(v <= 0f ? 0 : v >= 1f ? 255 : (int)Math.Round(v * 255));
            }
            return new Sample
            {
                Label = (ushort)label, Image = img,
                RelW = c.RelW, RelTop = c.RelTop, RelBottom = c.RelBottom, Aspect = c.Aspect
            };
        }

        public float[] ImageFloat()
        {
            var f = new float[Image.Length];
            for (int i = 0; i < f.Length; i++) f[i] = Image[i] / 255f;
            return f;
        }

        public float[] Features()
        {
            return CROMS.OwnOcr.Features.Extract(ImageFloat(), RelW, RelTop, RelBottom, Aspect);
        }
    }

    /// <summary>Reads and writes a list of samples as one small binary file.</summary>
    public static class Dataset
    {
        private const int Magic = 0x4F574E4F;   // "ONWO"
        private const int Version = 1;

        public static void Save(string path, List<Sample> samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using (var fs = File.Create(path))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(Magic); w.Write(Version);
                w.Write(samples.Count);
                w.Write(GlyphCell.Size * GlyphCell.Size);
                foreach (Sample s in samples)
                {
                    w.Write(s.Label);
                    w.Write(s.RelW); w.Write(s.RelTop); w.Write(s.RelBottom); w.Write(s.Aspect);
                    w.Write(s.Image);
                }
            }
        }

        public static List<Sample> Load(string path)
        {
            var list = new List<Sample>();
            using (var fs = File.OpenRead(path))
            using (var r = new BinaryReader(fs))
            {
                if (r.ReadInt32() != Magic) throw new InvalidDataException("Not an OwnOcr dataset: " + path);
                if (r.ReadInt32() != Version) throw new InvalidDataException("Unsupported dataset version.");
                int n = r.ReadInt32();
                int px = r.ReadInt32();
                if (px != GlyphCell.Size * GlyphCell.Size) throw new InvalidDataException("Glyph size mismatch.");
                for (int i = 0; i < n; i++)
                {
                    var s = new Sample();
                    s.Label = r.ReadUInt16();
                    s.RelW = r.ReadSingle(); s.RelTop = r.ReadSingle();
                    s.RelBottom = r.ReadSingle(); s.Aspect = r.ReadSingle();
                    s.Image = r.ReadBytes(px);
                    list.Add(s);
                }
            }
            return list;
        }
    }
}

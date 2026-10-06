using System;
using System.Collections.Generic;

namespace CROMS.OwnOcr
{
    /// <summary>One connected piece of ink: its bounding box and how many pixels it holds.</summary>
    public sealed class Blob
    {
        public int Id;
        public int Left, Top, Right, Bottom;   // inclusive
        public int Area;
        public int Width { get { return Right - Left + 1; } }
        public int Height { get { return Bottom - Top + 1; } }
        public float CenterX { get { return (Left + Right) / 2f; } }
        public float CenterY { get { return (Top + Bottom) / 2f; } }
    }

    /// <summary>The result of labelling: a per-pixel id (0 = background) plus each blob's box.</summary>
    public sealed class Labeling
    {
        public int Width, Height;
        public int[] Labels;
        public List<Blob> Blobs = new List<Blob>();
    }

    /// <summary>
    /// Connected-component labelling (8-connected, two passes with union-find).
    /// <para/>
    /// 8-connected on purpose: typewriter strokes touch diagonally where a curve meets a
    /// stem, and 4-connectivity would split one letter into several pieces.
    /// </summary>
    public static class ConnectedComponents
    {
        public static Labeling Label(byte[] mask, int w, int h)
        {
            var labels = new int[w * h];
            var parent = new List<int> { 0 };

            int next = 1;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (mask[i] == 0) continue;

                    // Neighbours already visited: W, NW, N, NE.
                    int a = x > 0 ? labels[i - 1] : 0;
                    int b = (x > 0 && y > 0) ? labels[i - w - 1] : 0;
                    int c = y > 0 ? labels[i - w] : 0;
                    int d = (x < w - 1 && y > 0) ? labels[i - w + 1] : 0;

                    int min = 0;
                    if (a != 0) min = a;
                    if (b != 0 && (min == 0 || b < min)) min = b;
                    if (c != 0 && (min == 0 || c < min)) min = c;
                    if (d != 0 && (min == 0 || d < min)) min = d;

                    if (min == 0)
                    {
                        labels[i] = next;
                        parent.Add(next);
                        next++;
                    }
                    else
                    {
                        labels[i] = min;
                        if (a != 0) Union(parent, min, a);
                        if (b != 0) Union(parent, min, b);
                        if (c != 0) Union(parent, min, c);
                        if (d != 0) Union(parent, min, d);
                    }
                }
            }

            // Second pass: replace every provisional label by its root, renumbered 1..n.
            var remap = new int[parent.Count];
            var result = new Labeling { Width = w, Height = h, Labels = labels };
            var byRoot = new Dictionary<int, Blob>();
            for (int i = 0; i < labels.Length; i++)
            {
                int l = labels[i];
                if (l == 0) continue;
                int root = Find(parent, l);
                Blob blob;
                if (!byRoot.TryGetValue(root, out blob))
                {
                    blob = new Blob { Id = byRoot.Count + 1, Left = int.MaxValue, Top = int.MaxValue,
                                      Right = -1, Bottom = -1 };
                    byRoot[root] = blob;
                    result.Blobs.Add(blob);
                }
                labels[i] = blob.Id;
                int x = i % w, y = i / w;
                if (x < blob.Left) blob.Left = x;
                if (x > blob.Right) blob.Right = x;
                if (y < blob.Top) blob.Top = y;
                if (y > blob.Bottom) blob.Bottom = y;
                blob.Area++;
            }
            return result;
        }

        private static int Find(List<int> parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];   // path halving
                x = parent[x];
            }
            return x;
        }

        private static void Union(List<int> parent, int a, int b)
        {
            int ra = Find(parent, a), rb = Find(parent, b);
            if (ra == rb) return;
            if (ra < rb) parent[rb] = ra; else parent[ra] = rb;
        }
    }
}

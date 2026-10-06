using System;
using System.Collections.Generic;
using System.Linq;

namespace CROMS.OwnOcr
{
    /// <summary>One candidate reading of a character, with how strongly the neighbours voted for it.</summary>
    public sealed class Prediction
    {
        public int Label;        // index into Charset
        public float Score;      // share of the weighted vote, 0..1
        public char Char { get { return Charset.At(Label); } }
    }

    /// <summary>
    /// k-nearest-neighbour classifier over the feature vectors.
    /// <para/>
    /// Chosen for the capstone because it needs no training step to get wrong, every answer
    /// can be explained by pointing at the stored examples that voted for it, and it can be
    /// written in a page. Its weakness is also instructive: it only knows letters that look
    /// like something it has seen, so the data it is given matters more than the maths.
    /// <para/>
    /// The training set is capped per class. Some letters are far more common in the
    /// synthetic text than others (A outnumbers X thirty to one), and with a plain vote a
    /// crowded class wins ties it has no right to.
    /// </summary>
    public sealed class KnnClassifier
    {
        private readonly int _dim;
        private readonly int _n;
        private readonly float[] _data;
        private readonly ushort[] _labels;
        private readonly float[] _weight;        // vote weight of each stored example (1 = synthetic)

        public int K = 5;

        public int Count { get { return _n; } }

        public KnnClassifier(List<Sample> train, int maxPerClass, int seed)
            : this(train, null, 1f, maxPerClass, seed) { }

        /// <summary>
        /// <paramref name="extra"/> are real glyphs added on top of the capped synthetic set. They are
        /// few, so they are never thinned, and their vote counts <paramref name="extraWeight"/> times
        /// as much: a real typewriter "e" is better evidence about a real "e" than a rendered one.
        /// </summary>
        public KnnClassifier(List<Sample> train, List<Sample> extra, float extraWeight, int maxPerClass, int seed)
        {
            // Balanced subset.
            var rng = new Random(seed);
            var chosen = new List<Sample>();
            foreach (var group in train.GroupBy(s => s.Label))
            {
                var items = group.ToList();
                // The reject class is not one shape but every wrong way to cut a line, so it gets
                // several times the room of a letter.
                int cap = group.Key == Charset.Reject ? maxPerClass * 4 : maxPerClass;
                if (items.Count > cap)
                    items = items.OrderBy(_ => rng.Next()).Take(cap).ToList();
                chosen.AddRange(items);
            }

            int synthCount = chosen.Count;
            if (extra != null) chosen.AddRange(extra);
            _dim = Features.Dim;
            _n = chosen.Count;
            _weight = new float[_n];
            for (int i = 0; i < _n; i++) _weight[i] = i < synthCount ? 1f : extraWeight;
            _data = new float[_n * _dim];
            _labels = new ushort[_n];
            for (int i = 0; i < _n; i++)
            {
                float[] f = chosen[i].Features();
                Array.Copy(f, 0, _data, i * _dim, _dim);
                _labels[i] = chosen[i].Label;
            }
        }

        /// <summary>The most likely characters for one feature vector, best first.</summary>
        public Prediction[] Rank(float[] q, int top = 5)
        {
            float nearest;
            return Rank(q, top, out nearest);
        }

        /// <summary>
        /// As above, and also how far the single closest stored example was. That distance is
        /// the measure of "does this look like ANY character": half a letter, or two letters
        /// squeezed into one box, are far from everything the classifier has seen, and that is
        /// what the splitter uses to judge a candidate cut.
        /// </summary>
        public Prediction[] Rank(float[] q, int top, out float nearest)
        {
            return Rank(q, top, out nearest, null);
        }

        /// <summary>
        /// As above, but only examples whose label is allowed take part. This is how what is KNOWN
        /// about a field is used: a name has no digits, so a name field is read with digits (and
        /// the reject class) switched off and an '1' can no longer be chosen over an 'I'.
        /// </summary>
        public Prediction[] Rank(float[] q, int top, out float nearest, bool[] allowed)
        {
            int k = Math.Min(K, _n);
            var bestD = new float[k];
            var bestI = new int[k];
            for (int i = 0; i < k; i++) { bestD[i] = float.MaxValue; bestI[i] = -1; }

            for (int i = 0; i < _n; i++)
            {
                if (allowed != null && !allowed[_labels[i]]) continue;
                int o = i * _dim;
                float d = 0;
                for (int j = 0; j < _dim; j++)
                {
                    float diff = q[j] - _data[o + j];
                    d += diff * diff;
                    if (d >= bestD[k - 1]) break;       // cannot make the top k any more
                }
                if (d >= bestD[k - 1]) continue;

                int pos = k - 1;
                while (pos > 0 && bestD[pos - 1] > d) { bestD[pos] = bestD[pos - 1]; bestI[pos] = bestI[pos - 1]; pos--; }
                bestD[pos] = d; bestI[pos] = i;
            }

            nearest = bestI[0] < 0 ? float.MaxValue : (float)Math.Sqrt(bestD[0]);

            // Weighted vote: a near neighbour counts for more than a far one.
            var votes = new Dictionary<int, double>();
            double total = 0;
            for (int i = 0; i < k; i++)
            {
                if (bestI[i] < 0) continue;
                double w = _weight[bestI[i]] / (Math.Sqrt(bestD[i]) + 0.05);
                int label = _labels[bestI[i]];
                double cur;
                votes.TryGetValue(label, out cur);
                votes[label] = cur + w;
                total += w;
            }
            return votes.OrderByDescending(v => v.Value).Take(top)
                        .Select(v => new Prediction { Label = v.Key, Score = (float)(v.Value / total) })
                        .ToArray();
        }

        public Prediction Best(float[] q)
        {
            Prediction[] r = Rank(q, 1);
            return r.Length > 0 ? r[0] : null;
        }
    }
}

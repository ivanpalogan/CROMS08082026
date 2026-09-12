using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace CROMS.Data
{
    /// <summary>
    /// The spellings this office already holds, used to repair a name or place that OCR
    /// read ALMOST right.
    /// <para/>
    /// A civil registry is not a stream of strangers: the same families come back for a
    /// birth, then a marriage, then a death, and the office's own records are the
    /// authority on how their names are spelled. So a marriage certificate that reads
    /// "TALOST" where the registry already holds TALOSIG — for the same father, on the
    /// same document — is a misread of a name we have, not a new name.
    /// <para/>
    /// This is deliberately NOT a dictionary or a guess. A reading is only replaced when
    /// it is close to exactly ONE value the office has recorded before and clearly closer
    /// to that one than to any other; anything else is left exactly as it was read, and
    /// every replacement is reported to the operator beside the original reading.
    /// <para/>
    /// PLACES ONLY, and that limit is the whole point. This was built for names too and
    /// measured before it was trusted: on the office's own marriage certificate it
    /// repaired two spouse names correctly and also turned "Albert" — the husband's
    /// FATHER, Alberto — into "GILBERT", the husband, because those two names are two
    /// characters apart and only one of them was in the registry. That is not a tuning
    /// problem, it is the shape of the technique: the more names an office accumulates,
    /// the more near-neighbours every reading has, so a name repair gets LESS safe as the
    /// database grows. And a wrong name that looks right is worse than a garbled one the
    /// operator can see is garbled — nobody accepts "OFLEERT", everybody accepts
    /// "GILBERT". Barangays, municipalities, provinces and hospitals are a genuinely
    /// closed, small set the office controls, so the same reasoning does not apply to
    /// them.
    /// </summary>
    public static class DocVocabulary
    {
        /// <summary>Place text: hospitals, barangays, municipalities, provinces, residences.</summary>
        public const string Places = "places";

        /// <summary>Province names on their own, for telling a place string apart.</summary>
        public const string Provinces = "provinces";

        /// <summary>Municipality and city names on their own, same purpose.</summary>
        public const string Municipalities = "municipalities";

        /// <summary>
        /// The provinces of the Philippines. A FIXED, public, closed list - which is exactly
        /// why it is safe to keep here when the office's own `provinces` table is not.
        /// <para/>
        /// This office has one province on file (its own), so without this a birth registered
        /// anywhere else could not have its province recognised at all. Note the distinction
        /// this list does NOT cross: it is used only to SPLIT a place string that already
        /// says these words, never to add a province the scan did not contain.
        /// </summary>
        private static readonly string[] PhProvinces =
        {
            "Abra","Agusan del Norte","Agusan del Sur","Aklan","Albay","Antique","Apayao",
            "Aurora","Basilan","Bataan","Batanes","Batangas","Benguet","Biliran","Bohol",
            "Bukidnon","Bulacan","Cagayan","Camarines Norte","Camarines Sur","Camiguin",
            "Capiz","Catanduanes","Cavite","Cebu","Cotabato","Davao de Oro","Davao del Norte",
            "Davao del Sur","Davao Occidental","Davao Oriental","Dinagat Islands",
            "Eastern Samar","Guimaras","Ifugao","Ilocos Norte","Ilocos Sur","Iloilo","Isabela",
            "Kalinga","La Union","Laguna","Lanao del Norte","Lanao del Sur","Leyte",
            "Maguindanao del Norte","Maguindanao del Sur","Marinduque","Masbate","Metro Manila",
            "Misamis Occidental","Misamis Oriental","Mountain Province","Negros Occidental",
            "Negros Oriental","Northern Samar","Nueva Ecija","Nueva Vizcaya",
            "Occidental Mindoro","Oriental Mindoro","Palawan","Pampanga","Pangasinan","Quezon",
            "Quirino","Rizal","Romblon","Samar","Sarangani","Siquijor","Sorsogon",
            "South Cotabato","Southern Leyte","Sultan Kudarat","Sulu","Surigao del Norte",
            "Surigao del Sur","Tarlac","Tawi-Tawi","Zambales","Zamboanga del Norte",
            "Zamboanga del Sur","Zamboanga Sibugay",
        };

        private static readonly object Gate = new object();
        private static Dictionary<string, List<string>> _pools;
        private static DateTime _loadedAt = DateTime.MinValue;
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

        /// <summary>True when a vocabulary could be loaded (a machine with no database still works, just without repairs).</summary>
        public static bool IsAvailable
        {
            get { var p = Pools(); return p != null && p.Count > 0 && p.Values.Any(v => v.Count > 0); }
        }

        /// <summary>Forget the cached vocabulary so the next lookup re-reads the registry.</summary>
        public static void Invalidate()
        {
            lock (Gate) { _pools = null; _loadedAt = DateTime.MinValue; }
        }

        private static Dictionary<string, List<string>> Pools()
        {
            lock (Gate)
            {
                if (_pools != null && DateTime.Now - _loadedAt < Ttl) return _pools;
                _pools = Load();
                _loadedAt = DateTime.Now;
                return _pools;
            }
        }

        /// <summary>
        /// Read every name and place the registry already holds. Any failure — no
        /// database, a table missing on an older install — leaves the pool empty rather
        /// than throwing: a repair is an improvement, never a requirement.
        /// </summary>
        private static Dictionary<string, List<string>> Load()
        {
            var places = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] placeQueries =
            {
                "SELECT place_of_birth, mother_residence, father_residence, parents_marriage_place FROM births",
                "SELECT place_of_death, place_of_disposal FROM deaths",
                "SELECT name FROM hospitals UNION SELECT name FROM municipalities UNION " +
                "SELECT name FROM provinces UNION SELECT name FROM barangays UNION SELECT name FROM churches",
                "SELECT value FROM reference_library WHERE category IN " +
                "('PlaceOfBirth','PlaceOfMarriage','PlaceOfDeath','Municipality','Province','Barangay','Hospital','Church')"
            };

            foreach (string sql in placeQueries) Harvest(sql, places);

            // Kept as their own pools, because telling "which part of this text is the
            // province" needs province names ALONE - the mixed `places` pool cannot answer it.
            var provinces = new HashSet<string>(PhProvinces, StringComparer.OrdinalIgnoreCase);
            Harvest("SELECT name FROM provinces", provinces);

            var municipalities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Harvest("SELECT name FROM municipalities", municipalities);

            return new Dictionary<string, List<string>>(StringComparer.Ordinal)
            {
                { Places, places.ToList() },
                { Provinces, provinces.ToList() },
                { Municipalities, municipalities.ToList() },
            };
        }

        private static void Harvest(string sql, HashSet<string> into)
        {
            try
            {
                DataTable t = Db.Pull(sql);
                if (t == null) return;
                foreach (DataRow row in t.Rows)
                    foreach (DataColumn col in t.Columns)
                    {
                        string raw = Convert.ToString(row[col]);
                        if (string.IsNullOrWhiteSpace(raw)) continue;

                        Add(into, raw);
                        // "Cagayan Valley Medical Center, Tuguegarao City, Cagayan" is also
                        // three usable place values on its own.
                        foreach (string part in raw.Split(','))
                            Add(into, part);
                    }
            }
            catch { /* no database, or an older schema: the vocabulary is simply smaller */ }
        }

        private static void Add(HashSet<string> into, string value)
        {
            string v = Regex.Replace((value ?? "").Trim(), @"\s{2,}", " ");
            // Below four letters there is not enough signal to repair anything safely, and
            // the office's test data is full of short junk ("SDFG", "dfg") that a loose
            // threshold would happily match a real reading onto.
            if (v.Length < 4) return;
            if (v.Count(char.IsLetter) < 4) return;
            into.Add(v);
        }

        /// <summary>
        /// The office's spelling for ONE place, or null to keep the reading as it is.
        /// <para/>
        /// Returns a value only when exactly one recorded spelling is within the allowance
        /// and the next-closest is strictly further away. Ties are refused on purpose: if
        /// the reading sits between two names the office holds, choosing either would be a
        /// coin flip printed onto a civil-registry record.
        /// </summary>
        public static string Snap(string category, string reading, out int distance)
        {
            distance = int.MaxValue;
            if (string.IsNullOrWhiteSpace(reading)) return null;

            Dictionary<string, List<string>> pools = Pools();
            List<string> pool;
            if (pools == null || !pools.TryGetValue(category, out pool) || pool.Count == 0) return null;

            string probe = Normalise(reading);
            if (probe.Length < 4) return null;

            string best = null; int bestD = int.MaxValue, secondD = int.MaxValue;
            bool bestTied = false;

            foreach (string candidate in pool)
            {
                string target = Normalise(candidate);
                if (target.Length < 4) continue;
                // A cheap length gate before the expensive distance: two strings whose
                // lengths differ by more than the allowance cannot possibly be within it.
                if (Math.Abs(target.Length - probe.Length) > Allowance(probe.Length)) continue;

                int d = Distance(probe, target);
                if (d < bestD)
                {
                    secondD = bestD; bestD = d; best = candidate; bestTied = false;
                }
                else if (d == bestD)
                {
                    // Same distance to a DIFFERENT spelling is a genuine ambiguity; the same
                    // distance to the same normalised text is not.
                    if (best != null && !string.Equals(Normalise(best), target, StringComparison.Ordinal))
                        bestTied = true;
                }
                else if (d < secondD) secondD = d;
            }

            if (best == null || bestTied) return null;
            if (bestD == 0) return null;                       // already correct, nothing to repair
            if (bestD > Allowance(probe.Length)) return null;
            // A clear MARGIN, not merely "closer": one edit further away is well within the
            // noise of a bad scan, so a runner-up that close means the reading has not
            // actually been identified.
            if (secondD - bestD < 2) return null;

            distance = bestD;
            return best;
        }

        /// <summary>
        /// How far a reading may sit from a recorded spelling and still be treated as the
        /// same word. Scales with length because one wrong character in a four-letter name
        /// is a quarter of the word, while two in a ten-letter name is a fifth.
        /// </summary>
        private static int Allowance(int length)
        {
            if (length <= 4) return 1;
            if (length <= 7) return 2;
            return 3;
        }

        /// <summary>Case, spacing and punctuation are not part of the identity of a name.</summary>
        private static string Normalise(string s)
        {
            return Regex.Replace((s ?? "").ToLowerInvariant(), @"[^a-z0-9ñ]", "");
        }

        /// <summary>
        /// Break "HUYON HUYON TIGAON CAMARINES SUR" into its facility, municipality and
        /// province parts.
        /// <para/>
        /// The three cells of a Place of Birth are separated on the paper by RULED LINES, not
        /// by punctuation, so OCR reads the row back as one run of words with no commas in it.
        /// Splitting on commas therefore put the whole run in the facility cell and left
        /// Municipality and Province empty - and, because the composite is rebuilt from those
        /// parts, Place of Birth and Hospital ended up showing the identical string.
        /// <para/>
        /// The province is matched from the END, longest first, so "Davao del Norte" wins over
        /// "Norte". Only then is the remainder searched for a municipality. Nothing is guessed:
        /// a run this cannot recognise is returned whole as the facility, exactly as before.
        /// </summary>
        public static bool SplitPlace(string place, out string facility,
                                      out string municipality, out string province)
        {
            facility = (place ?? "").Trim();
            municipality = "";
            province = "";
            if (facility.Length == 0) return false;

            // Punctuated text already states its own parts, so honour them rather than
            // re-deriving: last = province, the one before it = municipality.
            string[] parts = facility.Split(',')
                .Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            if (parts.Length >= 2)
            {
                province = parts[parts.Length - 1];
                municipality = parts.Length >= 3 ? parts[parts.Length - 2] : "";
                int keep = parts.Length - (parts.Length >= 3 ? 2 : 1);
                facility = string.Join(", ", parts.Take(keep));
                return true;
            }

            string[] words = facility.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2) return false;

            province = TakeTail(ref words, Provinces, 4);
            if (province.Length == 0)
            {
                facility = string.Join(" ", words);
                return false;
            }

            municipality = TakeTail(ref words, Municipalities, 4);
            if (municipality.Length == 0) municipality = TakeCityTail(ref words);

            facility = string.Join(" ", words);
            return true;
        }

        /// <summary>
        /// Removes and returns the longest run of trailing words that names something in
        /// <paramref name="pool"/>, in the pool's own spelling. Longest-first so a two-word
        /// name is never truncated to its last word.
        /// </summary>
        private static string TakeTail(ref string[] words, string pool, int maxWords)
        {
            List<string> names = Pool(pool);
            if (names.Count == 0) return "";

            int max = Math.Min(maxWords, words.Length - 1);   // never consume the whole string
            for (int take = max; take >= 1; take--)
            {
                string tail = string.Join(" ", words.Skip(words.Length - take));
                string key = Normalise(tail);
                string hit = names.FirstOrDefault(n => Normalise(n) == key);
                if (hit == null) continue;
                words = words.Take(words.Length - take).ToArray();
                return hit;
            }
            return "";
        }

        /// <summary>
        /// A trailing "... TUGUEGARAO CITY" is a municipality even when the office has never
        /// recorded it. "City" is part of the official name of a Philippine city, so this
        /// reads what the page says rather than inferring anything.
        /// </summary>
        private static string TakeCityTail(ref string[] words)
        {
            if (words.Length < 3) return "";
            if (!words[words.Length - 1].Equals("City", StringComparison.OrdinalIgnoreCase))
                return "";
            string name = words[words.Length - 2] + " " + words[words.Length - 1];
            words = words.Take(words.Length - 2).ToArray();
            return name;
        }

        private static List<string> Pool(string category)
        {
            var pools = Pools();
            List<string> list;
            return pools != null && pools.TryGetValue(category, out list)
                ? list : new List<string>();
        }

        private static int Distance(string a, string b)
        {
            if (a == b) return 0;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                Array.Copy(cur, prev, cur.Length);
            }
            return prev[b.Length];
        }
    }
}

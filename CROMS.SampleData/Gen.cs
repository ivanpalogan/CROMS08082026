using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.SampleData
{
    /// <summary>One municipality the sample people live in (read from the PSGC tables already loaded).</summary>
    internal sealed class Muni
    {
        public int Id; public string Name;
        public List<string> Barangays = new List<string>();
    }

    /// <summary>A fictional person. Nobody here is real; every combination is generated.</summary>
    internal sealed class Person
    {
        public string First, Middle, Last, Sex;
        public DateTime Dob;
        public Muni M; public string Barangay, House;
        public string Occupation, Religion;
        public string Full { get { return string.Join(" ", new[] { First, Middle, Last }.Where(s => !string.IsNullOrWhiteSpace(s))); } }
        /// <summary>Stored the way the registration screens join a residence: house, province, municipality, barangay.</summary>
        public string Residence { get { return string.Join(", ", new[] { House, "Cagayan", M.Name, Barangay }.Where(s => !string.IsNullOrWhiteSpace(s))); } }
    }

    internal static class G
    {
        public const string Marker = "SAMPLE DATA 2026";
        public static readonly Random R = new Random(20261009);   // fixed seed: the same data every time
        public static readonly DateTime Today = DateTime.Today;
        public static int UserId;
        public static int ProvinceId;
        public static List<Muni> Munis = new List<Muni>();
        public static readonly List<string> Notes = new List<string>();

        // ----------------------------------------------------------------------- random helpers
        public static T Pick<T>(IList<T> l) { return l[R.Next(l.Count)]; }
        public static bool Chance(double p) { return R.NextDouble() < p; }
        public static int Between(int a, int b) { return R.Next(a, b + 1); }
        public static T Weighted<T>(IList<T> items, IList<int> weights)
        {
            int total = weights.Sum(), x = R.Next(total), acc = 0;
            for (int i = 0; i < items.Count; i++) { acc += weights[i]; if (x < acc) return items[i]; }
            return items[items.Count - 1];
        }
        /// <summary>A moment on <paramref name="day"/> inside office hours, never later than "now" when the day is today.</summary>
        public static DateTime Office(DateTime day, int fromHour = 8, int toHour = 16)
        {
            DateTime t = day.Date.AddHours(fromHour).AddMinutes(R.Next(0, (toHour - fromHour) * 60));
            if (day.Date == Today && t > DateTime.Now) t = DateTime.Now.AddMinutes(-R.Next(5, 90));
            if (t < day.Date) t = day.Date.AddHours(7);
            return t;
        }
        /// <summary>Move a date to a working day without leaving [min, max].</summary>
        public static DateTime Workday(DateTime d, DateTime min, DateTime max)
        {
            DateTime x = d.Date;
            while ((x.DayOfWeek == DayOfWeek.Saturday || x.DayOfWeek == DayOfWeek.Sunday) && x < max) x = x.AddDays(1);
            while ((x.DayOfWeek == DayOfWeek.Saturday || x.DayOfWeek == DayOfWeek.Sunday) && x > min) x = x.AddDays(-1);
            return x;
        }
        public static DateTime Min(DateTime a, DateTime b) { return a < b ? a : b; }

        // ----------------------------------------------------------------------- names (all invented combinations)
        static readonly string[] Surnames =
        {
            "Aggabao","Agatep","Alonzo","Antolin","Bacani","Baccay","Balisi","Binuya","Cabalza","Cabauatan","Caguioa","Callueng",
            "Carag","Castillejo","Catolos","Dalauidao","Dumaguing","Dumlao","Gammad","Gattu","Guiab","Lappay","Lasam","Maguigad",
            "Mallillin","Macaraeg","Mamauag","Narag","Pagulayan","Pascua","Pattung","Ponce","Quiambao","Rubio","Siddayao","Taguba",
            "Taguinod","Tumaliuan","Tumaneng","Ubaldo","Valdez","Villaflor","Visaya","Zipagan","Ancheta","Bagunu","Balauitan",
            "Camacho","Dacanay","Gaspar","Hermosa","Ibarra","Labuguen","Mabborang","Nolasco","Orcullo","Palattao","Rabang"
        };
        static readonly string[] Male =
        {
            "Alfredo","Benedict","Carlito","Dennis","Eduardo","Felix","Gerald","Herminio","Ismael","Jerome","Kenneth","Lorenzo","Marvin",
            "Nestor","Orlando","Patrick","Quirino","Rodel","Sherwin","Teodoro","Vicente","Wilfredo","Arnel","Bryan","Cedrick","Danilo",
            "Elmer","Froilan","Glenn","Harold","Jericho","Kristoffer","Lemuel","Mario","Noel","Oscar","Paulo","Raymund","Salvador","Tomas"
        };
        static readonly string[] Female =
        {
            "Angelica","Beatriz","Cecilia","Dolores","Elena","Fe","Gemma","Hazel","Irene","Jocelyn","Katrina","Lourdes","Marites","Norma",
            "Olivia","Perla","Queenie","Rowena","Susana","Teresita","Vilma","Wilma","Yolanda","Zenaida","Annabelle","Bernadette","Carmela",
            "Divina","Erlinda","Flordeliza","Gina","Helen","Imelda","Joanna","Kristine","Lorna","Marissa","Nenita","Pamela","Rizza","Tessa"
        };
        static readonly string[] Religions = { "Roman Catholic", "Iglesia ni Cristo", "Born Again Christian", "Seventh-day Adventist", "Islam", "Church Of Christ" };
        static readonly int[] ReligionW = { 72, 8, 8, 5, 3, 4 };
        static readonly string[] MaleJobs = { "Farmer", "Driver", "Fisherman", "Laborer", "Teacher", "Government Employee", "Vendor", "Self-employed", "Overseas Filipino Worker (OFW)", "Business Owner", "Engineer" };
        static readonly string[] FemaleJobs = { "Housewife", "Teacher", "Vendor", "Nurse", "Government Employee", "Self-employed", "Business Owner" };

        public static string Given(bool male) { return Pick(male ? Male : Female); }
        public static string Surname()
        {
            return Pick(Surnames);
        }
        public static string Religion() { return Weighted(Religions, ReligionW); }
        public static string Job(bool male) { return Pick(male ? MaleJobs : FemaleJobs); }

        /// <summary>Weighted Cagayan municipality; Penablanca is where this office is, so most people live there.</summary>
        public static Muni PickMuni()
        {
            var names = new[] { "Penablanca", "Tuguegarao City", "Iguig", "Amulung", "Enrile", "Tuao" };
            var w = new[] { 62, 18, 6, 6, 4, 4 };
            for (int guard = 0; guard < 10; guard++)
            {
                string n = Weighted(names, w);
                Muni m = Munis.FirstOrDefault(x => LearningLibrary.Normalize(x.Name) == LearningLibrary.Normalize(n));
                if (m != null && m.Barangays.Count > 0) return m;
            }
            return Munis.First(x => x.Barangays.Count > 0);
        }
        public static Muni Home() { return Munis.First(x => LearningLibrary.Normalize(x.Name) == "penablanca"); }

        static readonly string[] Houses = { "Purok 1", "Purok 2", "Purok 3", "Purok 4", "Purok 5", "Purok 6", "Sitio Centro", "Sitio Riverside", "12 Rizal St.", "45 Mabini St.", "Blk 3 Lot 8", "Purok 7" };
        public static Person NewPerson(string last, string middle, bool male, DateTime dob, Muni m = null)
        {
            m = m ?? PickMuni();
            return new Person
            {
                First = Given(male), Middle = middle, Last = last, Sex = male ? "Male" : "Female", Dob = dob,
                M = m, Barangay = Pick(m.Barangays), House = Pick(Houses), Religion = Religion(), Occupation = Job(male)
            };
        }
        public static int Age(DateTime dob, DateTime on)
        {
            int a = on.Year - dob.Year;
            if (on < dob.AddYears(a)) a--;
            return a;
        }

        // ----------------------------------------------------------------------- database helpers
        public static MySqlParameter P(string n, object v) { return new MySqlParameter(n, v ?? DBNull.Value); }
        public static long Insert(string table, IDictionary<string, object> v)
        {
            var keys = v.Keys.ToList();
            return Db.Insert("INSERT INTO `" + table + "` (" + string.Join(", ", keys.Select(k => "`" + k + "`")) + ") VALUES (" +
                             string.Join(", ", keys.Select(k => "@" + k)) + ")", keys.Select(k => P("@" + k, v[k])).ToArray());
        }
        public static int Scalar(string sql, params MySqlParameter[] ps)
        {
            DataTable t = Db.Pull(sql, ps);
            return t.Rows.Count == 0 || t.Rows[0][0] == DBNull.Value ? 0 : Convert.ToInt32(t.Rows[0][0]);
        }
        public static bool Already(string table, string column)
        {
            return Scalar("SELECT COUNT(*) FROM `" + table + "` WHERE `" + column + "` LIKE @m", P("@m", "%" + Marker + "%")) > 0;
        }
        public static string DbName() { return Db.Pull("SELECT DATABASE()").Rows[0][0].ToString(); }

        /// <summary>Next YYYY-K-#### for a registry table, scoped to the year the entry was made (not necessarily this year).</summary>
        public static string NextReg(string table, char kind, int year)
        {
            int next = Scalar("SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no,'-',-1) AS UNSIGNED)),0)+1 FROM `" + table + "` WHERE registry_no LIKE @p",
                              P("@p", year + "-" + kind + "-%"));
            try
            {
                next = Math.Max(next, Scalar("SELECT COALESCE(MAX(CAST(SUBSTRING_INDEX(registry_no,'-',-1) AS UNSIGNED)),0)+1 FROM deleted_records WHERE source_table=@t AND registry_no LIKE @p",
                                             P("@t", table), P("@p", year + "-" + kind + "-%")));
            }
            catch (MySqlException) { /* migration 86 not applied: nothing reserved */ }
            return string.Format("{0}-{1}-{2:D4}", year, kind, next);
        }

        public static void Load()
        {
            DataTable u = Db.Pull("SELECT id FROM users WHERE role='Admin' AND is_active=1 ORDER BY id LIMIT 1");
            if (u.Rows.Count == 0) throw new InvalidOperationException("No active Admin user to attribute the sample data to.");
            UserId = Convert.ToInt32(u.Rows[0][0]);
            DataTable fu = Db.Pull("SELECT id, username, full_name FROM users WHERE id=@i", P("@i", UserId));
            Session.User = new CurrentUser { Id = UserId, Username = fu.Rows[0]["username"].ToString(), FullName = fu.Rows[0]["full_name"].ToString(), Role = "Admin" };

            DataTable p = Db.Pull("SELECT id FROM provinces WHERE name='Cagayan' LIMIT 1");
            if (p.Rows.Count == 0) throw new InvalidOperationException("Province 'Cagayan' is missing from the provinces table (migration 29).");
            ProvinceId = Convert.ToInt32(p.Rows[0][0]);
            foreach (DataRow r in Db.Pull("SELECT id, name FROM municipalities WHERE province_id=@p ORDER BY name", P("@p", ProvinceId)).Rows)
            {
                var m = new Muni { Id = Convert.ToInt32(r["id"]), Name = r["name"].ToString() };
                string n = LearningLibrary.Normalize(m.Name);
                if (n == "penablanca" || n == "tuguegarao city" || n == "iguig" || n == "amulung" || n == "enrile" || n == "tuao")
                    foreach (DataRow b in Db.Pull("SELECT name FROM barangays WHERE municipality_id=@m AND name NOT LIKE '%(Pob.)%' ORDER BY name", P("@m", m.Id)).Rows)
                        m.Barangays.Add(b["name"].ToString());
                Munis.Add(m);
            }
            if (Home().Barangays.Count == 0) throw new InvalidOperationException("No barangays for Penablanca in the barangays table (migration 29).");
        }

        public static void Note(string s) { Notes.Add(s); Console.WriteLine("  note: " + s); }
    }
}

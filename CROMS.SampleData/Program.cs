using System;
using System.Collections.Generic;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>
    /// Fictional sample data for demos. Usage:
    ///   CROMS.SampleData.exe --database croms_demo --seed [--only births,deaths,marriages,petitions,certs,breqs,queue]
    ///   CROMS.SampleData.exe --database croms_demo --cleanup --confirm
    ///   CROMS.SampleData.exe --database croms_demo --counts
    /// --database must equal the schema the connection string actually reaches (a guard against seeding the wrong one).
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string want = ArgValue(args, "--database");
            if (string.IsNullOrWhiteSpace(want)) { Console.WriteLine("Give --database <schema> (the schema you intend to write to)."); return 2; }
            string actual;
            try { actual = G.DbName(); }
            catch (Exception ex) { Console.WriteLine("Cannot reach the database: " + ex.Message); return 2; }
            if (!string.Equals(want, actual, StringComparison.OrdinalIgnoreCase))
            { Console.WriteLine("REFUSED: --database says '" + want + "' but the connection reaches '" + actual + "'."); return 2; }
            Console.WriteLine("Database: " + actual + (actual == "croms" ? "   (LIVE registry)" : "   (demo/test)"));

            try
            {
                G.Load();
                if (args.Contains("--counts")) { Counts(); return 0; }
                if (args.Contains("--cleanup"))
                {
                    if (!args.Contains("--confirm")) { Console.WriteLine("REFUSED: add --confirm to remove the sample rows."); return 2; }
                    return Cleanup.Run();
                }
                if (!args.Contains("--seed")) { Console.WriteLine("Nothing to do: use --seed, --cleanup --confirm or --counts."); return 2; }

                var only = (ArgValue(args, "--only") ?? "births,deaths,marriages,petitions,certs,breqs,queue").Split(',').Select(s => s.Trim().ToLowerInvariant()).ToList();
                Console.WriteLine("Seeding as user id " + G.UserId + " ...");
                if (only.Contains("births")) SeedBirths.Run();
                if (only.Contains("deaths")) SeedDeaths.Run();
                if (only.Contains("marriages")) SeedMarriages.Run();
                if (only.Contains("petitions")) SeedServices.Petitions();
                if (only.Contains("certs")) SeedServices.CertificateRequests();
                if (only.Contains("breqs")) SeedServices.Breqs();
                if (only.Contains("queue")) SeedQueue.Run();
                Counts();
                if (G.Notes.Count > 0) { Console.WriteLine("\nNotes:"); G.Notes.ForEach(n => Console.WriteLine("  - " + n)); }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED: " + ex.Message);
                Console.WriteLine(ex);
                return 1;
            }
        }

        static readonly string[] Tables =
        {
            "births","deaths","marriage_licenses","marriages","petitions","transactions","certificate_requests","payments","payment_items","releases",
            "psa_copy_requests","psa_transmittal_batches","queue_tickets","queue_ticket_services","kiosk_ctc_intake","document_requirements","marriage_case_history"
        };

        static void Counts()
        {
            Console.WriteLine("\nRow counts in " + G.DbName());
            foreach (string t in Tables)
                Console.WriteLine("  {0,-26} {1,6}", t, G.Scalar("SELECT COUNT(*) FROM `" + t + "`"));
        }

        static string ArgValue(string[] a, string name)
        {
            int i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}

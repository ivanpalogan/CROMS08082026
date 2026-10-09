using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CROMS.Data;

namespace CROMS.SampleData
{
    /// <summary>
    /// Removes exactly the rows this seeder wrote - found by the "SAMPLE DATA 2026" tag in each table's own
    /// text column (and, for child rows, by the ids of the tagged parents). Nothing is matched by a bare id
    /// range: an id is not an identity once rows have been deleted (2026-09-13 lesson). audit_log is never
    /// touched; the audit rows the services wrote stay as the append-only trail.
    /// </summary>
    internal static class Cleanup
    {
        static string In(IEnumerable<int> ids) { var l = ids.ToList(); return l.Count == 0 ? "NULL" : string.Join(",", l); }
        static List<int> Ids(string sql) { return Db.Pull(sql, G.P("@m", "%" + G.Marker + "%")).AsEnumerable().Select(r => Convert.ToInt32(r[0])).ToList(); }

        static void Show(string label, int n) { Console.WriteLine("  {0,-34} {1,5} deleted", label, n); }

        public static int Run()
        {
            Console.WriteLine("Removing rows tagged '" + G.Marker + "' ...");
            var births = Ids("SELECT id FROM births WHERE remarks LIKE @m");
            var deaths = Ids("SELECT id FROM deaths WHERE remarks LIKE @m");
            var marriages = Ids("SELECT id FROM marriages WHERE remarks LIKE @m");
            var licences = Ids("SELECT id FROM marriage_licenses WHERE remarks LIKE @m");
            var petitions = Ids("SELECT id FROM petitions WHERE remarks LIKE @m");
            var breqs = Ids("SELECT id FROM psa_copy_requests WHERE remarks LIKE @m");
            var tickets = Ids("SELECT id FROM queue_tickets WHERE purpose LIKE @m");
            var txns = Ids("SELECT transaction_id FROM certificate_requests WHERE purpose LIKE @m AND transaction_id IS NOT NULL");
            var batches = Db.Pull("SELECT DISTINCT batch_id FROM psa_transmittal_items WHERE record_table='marriages' AND record_id IN (" + In(marriages) + ")")
                            .AsEnumerable().Select(r => Convert.ToInt32(r[0])).ToList();

            // payments: tagged, or tied to a tagged transaction / request / licence / petition
            var pays = Ids("SELECT id FROM payments WHERE remarks LIKE @m OR transaction_id IN (" + In(txns) + ") " +
                           "OR (source_table='psa_copy_requests' AND source_id IN (" + In(breqs) + ")) " +
                           "OR (source_table='marriage_licenses' AND source_id IN (" + In(licences) + ")) " +
                           "OR (source_table='petitions' AND source_id IN (" + In(petitions) + "))");
            Db.Push("DELETE FROM payment_items WHERE payment_id IN (" + In(pays) + ")");
            Show("payments", Db.Push("DELETE FROM payments WHERE id IN (" + In(pays) + ")"));

            Show("releases", Db.Push("DELETE FROM releases WHERE transaction_id IN (" + In(txns) + ")"));
            Show("kiosk CTC intake", Db.Push("DELETE FROM kiosk_ctc_intake WHERE remarks LIKE @m", G.P("@m", "%" + G.Marker + "%")));
            Show("queue ticket services", Db.Push("DELETE FROM queue_ticket_services WHERE ticket_id IN (" + In(tickets) + ")"));
            Show("queue tickets", Db.Push("DELETE FROM queue_tickets WHERE id IN (" + In(tickets) + ")"));
            Show("certificate requests", Db.Push("DELETE FROM certificate_requests WHERE purpose LIKE @m", G.P("@m", "%" + G.Marker + "%")));
            Show("transactions", Db.Push("DELETE FROM transactions WHERE id IN (" + In(txns) + ")"));

            Db.Push("DELETE FROM psa_copy_history WHERE request_id IN (" + In(breqs) + ")");
            Show("PSA copy requests", Db.Push("DELETE FROM psa_copy_requests WHERE id IN (" + In(breqs) + ")"));
            Db.Push("DELETE FROM psa_transmittal_items WHERE record_table='marriages' AND record_id IN (" + In(marriages) + ")");
            Show("PSA transmittal batches",
                Db.Push("DELETE FROM psa_transmittal_batches WHERE id IN (" + In(batches) + ") AND NOT EXISTS (SELECT 1 FROM psa_transmittal_items i WHERE i.batch_id = psa_transmittal_batches.id)"));

            Db.Push("DELETE FROM marriage_case_history WHERE (entity='Marriage' AND entity_id IN (" + In(marriages) + ")) OR (entity='License' AND entity_id IN (" + In(licences) + ")) " +
                    "OR (entity='Birth' AND entity_id IN (" + In(births) + ")) OR (entity='Batch' AND entity_id IN (" + In(batches) + "))");
            Db.Push("DELETE FROM document_requirements WHERE (owner_type='License' AND owner_id IN (" + In(licences) + ")) OR (owner_type='Marriage' AND owner_id IN (" + In(marriages) + ")) " +
                    "OR (owner_type='Birth' AND owner_id IN (" + In(births) + ")) OR (owner_type='Petition' AND owner_id IN (" + In(petitions) + "))");
            Db.Push("DELETE FROM marriage_copies WHERE marriage_id IN (" + In(marriages) + ")");

            Show("petitions", Db.Push("DELETE FROM petitions WHERE id IN (" + In(petitions) + ")"));
            Show("marriages", Db.Push("DELETE FROM marriages WHERE id IN (" + In(marriages) + ")"));
            Show("marriage licences", Db.Push("DELETE FROM marriage_licenses WHERE id IN (" + In(licences) + ")"));
            Show("deaths", Db.Push("DELETE FROM deaths WHERE id IN (" + In(deaths) + ")"));
            Show("births", Db.Push("DELETE FROM births WHERE id IN (" + In(births) + ")"));
            Console.WriteLine("Done. audit_log rows written by the services are kept (append-only).");
            return 0;
        }
    }
}

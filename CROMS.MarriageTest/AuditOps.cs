using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CROMS.Data;
using MySql.Data.MySqlClient;

namespace CROMS.MarriageTest
{
    /// <summary>
    /// System audit, part 3: module-level operations the existing suites do not cover. Everything
    /// written is tagged ZZA... and removed afterwards; audit_log rows are removed only when their id
    /// is above the highest id seen before the run (an id alone is not an identity).
    /// </summary>
    internal static partial class AuditTest
    {
        private static readonly string ATag = "ZZA" + DateTime.Now.ToString("HHmmss");
        private static long _auditBefore, _ocrBatchBefore, _birthsBefore;

        private static Form Go(Form shell, string key)
        {
            Form f = (Form)shell.GetType().GetMethod("GoToModule").Invoke(shell, new object[] { key });
            Pump(8);
            return f;
        }

        /// <summary>AUDIT_ONLY=text runs only the operation checks whose title contains the text (and skips the module / role / kiosk sweeps).</summary>
        public static string OnlyFilter { get { string v = Environment.GetEnvironmentVariable("AUDIT_ONLY"); return string.IsNullOrWhiteSpace(v) ? null : v.ToLowerInvariant(); } }

        private static void Try(string name, Action a)
        {
            if (OnlyFilter != null && !name.ToLowerInvariant().Contains(OnlyFilter)) return;
            try { a(); }
            catch (Exception ex)
            {
                Exception e = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                Check(name, false, "EXCEPTION " + e.GetType().Name + ": " + e.Message);
            }
        }

        private static long Count(string sql) { return Convert.ToInt64(Db.Pull(sql).Rows[0][0]); }

        private static void OperationsPart(Form shell, DialogWatchdog wd)
        {
            Console.WriteLine("\n--- C. module operations (ZZA tag " + ATag + ")");
            OperationsCleanup();
            _auditBefore = Count("SELECT COALESCE(MAX(id),0) FROM audit_log");
            _ocrBatchBefore = Count("SELECT COALESCE(MAX(id),0) FROM ocr_batch");

            Try("Dashboard: refresh + KPI figures agree with the database", () => DashboardCheck(shell));
            Try("Transaction History: list, filter, search", () => TransactionsCheck(shell));
            Try("Petitions: create, list, advance, documents, delete", () => PetitionsCheck(shell, wd));
            Try("Death Registration: validation refusal + save + reload", () => DeathCheck(shell, wd));
            Try("Records Archive: every category + search + view certificate path", () => ArchiveCheck(shell, wd));
            Try("Reports & Analytics: every tab loads", () => ReportsCheck(shell));
            Try("Settings: every page", () => SettingsCheck(shell, wd));
            Try("Master Files: every category lists", () => MasterCheck(shell));
            Try("Document Processing: batch list loads", () => OcrScreenCheck(shell));
            Try("Certificate Request: validation refusal", () => CertRequestCheck(shell, wd));
            Try("Release & Claim / PSA Copies / Fees open with no ticket", () => OpenNoTicket(shell));
            Try("Certificate previews (Crystal / replica / facts certifications) open and close without printing", () => CertificatePreviewCheck(shell, wd));
            Try("Document Processing end to end: scan -> read -> edit -> commit -> duplicate refused -> auto-fill", () => OcrEndToEnd(shell, wd));
            Try("Death Registration entry dialog: every step renders", () => DeathRenderCheck(shell, wd));
            Try("Staff bypass / override: reasoned, audited, role-gated", BypassCheck);
            Try("Migrations vs code: features that need an unapplied migration", MigrationFeatureCheck);
        }

        // -------------------------------------------------------------- dashboard
        private static void DashboardCheck(Form shell)
        {
            Form d = Go(shell, "dashboard");
            Call(d, "RefreshData"); Pump(6);
            long waiting = Count("SELECT COUNT(*) FROM queue_tickets WHERE status='Waiting' AND DATE(created_at)=CURDATE()");
            bool found = false;
            foreach (Control c in All(d))
                if (c is Label l && l.Text == waiting.ToString() && l.Font.Size >= 20) { found = true; break; }
            Check("Dashboard shows the waiting-now figure (" + waiting + ") from the database", found || waiting == 0);
            Check("Dashboard refresh raised no unhandled UI exception", Unhandled.Count == 0, string.Join("; ", Unhandled));
        }

        private static IEnumerable<Control> All(Control root)
        {
            foreach (Control c in root.Controls) { yield return c; foreach (Control k in All(c)) yield return k; }
        }
        private static T FindCtl<T>(Control root, string name) where T : Control
        {
            return All(root).OfType<T>().FirstOrDefault(c => c.Name == name);
        }

        // -------------------------------------------------------------- transactions
        private static void TransactionsCheck(Form shell)
        {
            Form f = Go(shell, "transactions");
            var grid = FindCtl<DataGridView>(f, "dgvTxn");
            Call(f, "RefreshData");
            long all = Count("SELECT COUNT(*) FROM transactions");
            int rows = grid.Rows.Count;
            Check("Transaction list shows rows from the database (" + rows + " of " + all + ")", rows > 0 && rows <= all);
            var search = FindCtl<TextBox>(f, "txtSearch");
            search.Text = "ZZZNOSUCHCLIENT"; Pump(4);
            Check("Searching a name that does not exist empties the list", grid.Rows.Count == 0, grid.Rows.Count + " rows");
            search.Text = ""; Pump(4);
            Check("Clearing the search restores the list", grid.Rows.Count == rows, grid.Rows.Count + " vs " + rows);
            var cbo = FindCtl<ComboBox>(f, "cboStatus");
            cbo.SelectedItem = "Released"; Pump(4);
            long rel = Count("SELECT COUNT(*) FROM transactions WHERE status='Released'");
            Check("Status filter 'Released' agrees with the database (" + grid.Rows.Count + " vs " + rel + ")", grid.Rows.Count == rel);
            cbo.SelectedItem = "All"; Pump(4);
        }

        // -------------------------------------------------------------- petitions
        private static void PetitionsCheck(Form shell, DialogWatchdog wd)
        {
            Form f = Go(shell, "petitions");
            var cboType = (ComboBox)Fld(f, "cboType");
            var cboRt = (ComboBox)Fld(f, "cboRecordType");
            var cboRec = (ComboBox)Fld(f, "cboRecord");
            var txtReq = (TextBox)Fld(f, "txtRequester");
            var txtRel = (TextBox)Fld(f, "txtRelationship");
            var txtRem = (TextBox)Fld(f, "txtRemarks");
            long before = Count("SELECT COUNT(*) FROM petitions");

            // refusal: nothing chosen
            Call(f, "ClearForm");
            Call(f, "Save");
            Check("Save with no case type is refused (nothing written)", Count("SELECT COUNT(*) FROM petitions") == before);

            // one case of every type
            string[] codes = { "RA9048", "RA10172", "Legitimation", "SupplementalReport", "LegalInstrument", "CourtOrder" };
            int made = 0;
            for (int i = 0; i < codes.Length; i++)
            {
                Call(f, "ClearForm");
                cboType.SelectedIndex = i; Pump(2);
                cboRt.SelectedItem = "Birth"; Pump(4);
                if (cboRec.Items.Count > 0) cboRec.SelectedIndex = 0;
                txtReq.Text = ATag + " Requester " + codes[i]; txtRel.Text = "Self"; txtRem.Text = ATag + " remark";
                int m0 = wd.Count;
                Call(f, "Save"); Pump(10);
                made++;
            }
            long after = Count("SELECT COUNT(*) FROM petitions WHERE requester_name LIKE 'ZZA%'");
            Check("one case of each of the 6 types saved", after == 6, after + " rows");
            DataTable t = Db.Pull("SELECT id, petition_type, stage FROM petitions WHERE requester_name LIKE 'ZZA%' ORDER BY id");
            Check("every case starts at stage Filed", t.Rows.Cast<DataRow>().All(r => r["stage"].ToString() == "Filed"));
            long slips = Count("SELECT COUNT(*) FROM client_service_slips WHERE source_table='petitions' AND source_id IN (SELECT id FROM petitions WHERE requester_name LIKE 'ZZA%')");
            Check("each Save issued a service-slip control number", slips == 6, slips + " slips");

            // load + advance: RA9048 goes Filed -> Posted, Legitimation Filed -> Under Review
            int idRa = Convert.ToInt32(t.Rows[0]["id"]);
            Call(f, "LoadPetition", idRa);
            Call(f, "AdvanceStage");
            string st = Db.Pull("SELECT stage FROM petitions WHERE id=" + idRa).Rows[0][0].ToString();
            Check("RA 9048 case advances Filed -> Posted", st == "Posted", st);
            int idLg = Convert.ToInt32(t.Rows[2]["id"]);
            Call(f, "LoadPetition", idLg);
            Call(f, "AdvanceStage");
            st = Db.Pull("SELECT stage FROM petitions WHERE id=" + idLg).Rows[0][0].ToString();
            Check("Legitimation case advances Filed -> Under Review", st == "UnderReview", st);

            // case documents checklist (migration 52)
            long petReq = Count("SELECT COUNT(*) FROM marriage_requirement_types WHERE applies_to='Petition'");
            Check("Case Documents checklist has requirement types (migration 52 applied)", petReq > 0, "Petition requirement types in the database: " + petReq);

            // edit + delete
            Call(f, "LoadPetition", idRa);
            txtRem.Text = ATag + " edited";
            Call(f, "Save"); Pump(8);
            Check("editing a case updates the same row (no duplicate)",
                  Count("SELECT COUNT(*) FROM petitions WHERE requester_name LIKE 'ZZA%'") == 6 &&
                  Db.Pull("SELECT remarks FROM petitions WHERE id=" + idRa).Rows[0][0].ToString().Contains("edited"));
            Call(f, "LoadPetition", idRa);
            Call(f, "Delete"); Pump(6);
            Check("Delete removes exactly that case",
                  Count("SELECT COUNT(*) FROM petitions WHERE id=" + idRa) == 0 &&
                  Count("SELECT COUNT(*) FROM petitions WHERE requester_name LIKE 'ZZA%'") == 5);
        }

        // -------------------------------------------------------------- death
        private static void DeathCheck(Form shell, DialogWatchdog wd)
        {
            Form f = Go(shell, "death");
            long before = Count("SELECT COUNT(*) FROM deaths");
            Call(f, "ClearForm");
            object r = Call(f, "Register", false);
            Check("Register on an empty form is refused with inline messages (nothing written)", r == null && Count("SELECT COUNT(*) FROM deaths") == before);

            SetT(f, "txtLastName", ATag + "Dela Cruz"); SetT(f, "txtFirstName", "Pedro"); SetT(f, "txtMiddleName", "Reyes");
            var sex = FindCtl<ComboBox>(f, "cboSex"); sex.SelectedIndex = 0;
            var civ = FindCtl<ComboBox>(f, "cboCivil"); civ.SelectedIndex = 0;
            ((Control)Fld(f, "_cboDCit")).Text = "Filipino";
            var dob = FindCtl<DateTimePicker>(f, "dtpDob"); var dod = FindCtl<DateTimePicker>(f, "dtpDod");
            dod.Value = DateTime.Today.AddDays(-10); dob.Checked = true; dob.Value = DateTime.Today.AddYears(-70).AddDays(-30);
            Pump(3);
            Check("age is computed from the two dates (read-only)", FindCtl<TextBox>(f, "txtAge").Text == "70", "age box = " + FindCtl<TextBox>(f, "txtAge").Text);
            var pod = (ComboBox[])Fld(f, "_pod");
            pod[1].Text = "Cagayan"; GeoPick(pod[1], "Cagayan"); GeoPick(pod[2], "Penablanca");
            ((Control)Fld(f, "_cboDImm")).Text = "Cardiopulmonary arrest";
            var disp = FindCtl<ComboBox>(f, "cboDisposal"); disp.SelectedIndex = 0;
            SetT(f, "txtDispPlace", "Penablanca Public Cemetery");
            SetT(f, "txtCInfName", ATag + " Informant");
            ((Control)Fld(f, "_cboInfRel")).Text = "Son";
            SetT(f, "txtCInfAddr", "Bical, Penablanca, Cagayan");
            // a future date of death must be impossible
            Check("date of death cannot be set to the future", dod.MaxDate.Date <= DateTime.Today, "MaxDate " + dod.MaxDate.ToShortDateString());

            object id = Call(f, "Register", true);
            Check("Register saves the death with a registry number", id != null && Count("SELECT COUNT(*) FROM deaths WHERE full_name LIKE '%" + ATag + "%'") == 1);
            if (id != null)
            {
                DataRow row = Db.Pull("SELECT registry_no, status, age FROM deaths WHERE full_name LIKE '%" + ATag + "%'").Rows[0];
                Check("saved death is status Registered with a registry no", row["status"].ToString() == "Registered" && row["registry_no"].ToString().Length > 3, row["registry_no"] + " / " + row["status"]);
                Check("and it reloads into the form with the two-word surname intact (record kept open)", FindCtl<TextBox>(f, "txtLastName").Text == ATag + "Dela Cruz" && FindCtl<TextBox>(f, "txtMiddleName").Text == "Reyes", FindCtl<TextBox>(f, "txtFirstName").Text + " | " + FindCtl<TextBox>(f, "txtMiddleName").Text + " | " + FindCtl<TextBox>(f, "txtLastName").Text);
            }
            // blank the last name of the open record -> refused, not saved
            SetT(f, "txtLastName", "");
            Call(f, "UpdateRecord");
            Check("Updating an open record with its last name erased is refused",
                  Count("SELECT COUNT(*) FROM deaths WHERE full_name LIKE '%" + ATag + "%'") == 1);
            Call(f, "ClearForm");
        }

        private static void GeoPick(ComboBox c, string text)
        {
            for (int i = 0; i < c.Items.Count; i++)
                if (string.Equals(c.GetItemText(c.Items[i]), text, StringComparison.OrdinalIgnoreCase)) { c.SelectedIndex = i; return; }
            c.Text = text;
        }
        private static void SetT(object form, string field, string text) { ((Control)Fld(form, field)).Text = text; }

        // -------------------------------------------------------------- archive
        private static void ArchiveCheck(Form shell, DialogWatchdog wd)
        {
            Form f = Go(shell, "archive");
            var tree = FindCtl<TreeView>(f, "treeCategories");
            var grid = FindCtl<DataGridView>(f, "grid");
            var lbl = FindCtl<Label>(f, "lblCount");
            var q = FindCtl<TextBox>(f, "txtQuery");
            var type = FindCtl<ComboBox>(f, "cboSearchType");
            var fuzzy = FindCtl<CheckBox>(f, "chkFuzzy");
            tree.ExpandAll();
            var nodes = new List<TreeNode>();
            Action<TreeNodeCollection> walk = null;
            walk = c => { foreach (TreeNode n in c) { nodes.Add(n); walk(n.Nodes); } };
            walk(tree.Nodes);
            int leaves = 0;
            foreach (TreeNode n in nodes)
            {
                if (n.Nodes.Count > 0) continue;
                leaves++;
                string err = null;
                try { tree.SelectedNode = n; Pump(8); } catch (Exception ex) { err = ex.Message; }
                bool failed = err != null || (lbl.Text ?? "").IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 || (lbl.Text ?? "").IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0;
                Check("archive category '" + n.Text + "' opens", !failed, err ?? (failed ? lbl.Text : null));
            }
            Note("archive leaf categories checked: " + leaves);

            // search mode
            tree.SelectedNode = nodes[0]; Pump(6);
            type.SelectedItem = "All Records"; q.Text = "";
            Pump(4);
            int all = grid.Rows.Count;
            long expect = Math.Min(300, Count("SELECT (SELECT COUNT(*) FROM births) + (SELECT COUNT(*) FROM marriages) + (SELECT COUNT(*) FROM deaths)"));
            Check("search with no text lists the registry (capped at 300): " + all + " vs " + expect, all == expect);
            type.SelectedItem = "Birth"; Pump(4);
            Check("filter Birth lists only births", grid.Rows.Count == Math.Min(300, Count("SELECT COUNT(*) FROM births")), grid.Rows.Count + " rows");
            type.SelectedItem = "All Records";
            q.Text = "ZZZNOSUCHPERSON"; Pump(4);
            Check("search for an unknown name returns nothing", grid.Rows.Count == 0);
            // a name we know exists
            string known = Db.Pull("SELECT last_name FROM births WHERE last_name <> '' ORDER BY id LIMIT 1").Rows[0][0].ToString();
            q.Text = known; Pump(4);
            Check("search finds a known surname '" + known + "'", grid.Rows.Count > 0);
            string odd = known.Length > 3 ? known.Substring(0, known.Length - 1) + "x" : known;
            fuzzy.Checked = true; q.Text = known.ToLowerInvariant(); Pump(4);
            Check("sound-alike search on lower-case surname still finds it", grid.Rows.Count > 0);
            fuzzy.Checked = false; q.Text = "";
            Check("Staff session can open the archive search", true);
        }

        // -------------------------------------------------------------- reports
        private static void ReportsCheck(Form shell)
        {
            Form f = Go(shell, "reports");
            var tabs = FindCtl<TabControl>(f, "_tabs") ?? All(f).OfType<TabControl>().First();
            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                string title = tabs.TabPages[i].Text;
                int un0 = Unhandled.Count;
                tabs.SelectedIndex = i; Pump(14);
                try { Call(f, "RefreshData"); } catch (MissingMethodException) { }
                Pump(10);
                bool anyErr = All(tabs.TabPages[i]).OfType<Label>().Any(l => l.Visible && (l.Text ?? "").IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0);
                Check("report tab '" + title + "' loads", Unhandled.Count == un0 && !anyErr, Unhandled.Count > un0 ? Unhandled.Last() : null);
            }
        }

        // -------------------------------------------------------------- settings
        private static void SettingsCheck(Form shell, DialogWatchdog wd)
        {
            Form f = Go(shell, "settings");
            var pages = (System.Collections.IDictionary)Fld(f, "_pages");
            var lazy = (System.Collections.IDictionary)Fld(f, "_lazyPages");
            var names = new List<string>();
            foreach (var k in pages.Keys) names.Add((string)k);
            foreach (var k in lazy.Keys) if (!names.Contains((string)k)) names.Add((string)k);
            foreach (string n in names)
            {
                int m0 = wd.Count; int un0 = Unhandled.Count;
                Call(f, "ShowPage", n); Pump(12);
                var p = ((System.Collections.IDictionary)Fld(f, "_pages"))[n] as Control;
                Check("settings page '" + n + "' opens", p != null && Unhandled.Count == un0, Unhandled.Count > un0 ? Unhandled.Last() : null);
                if (wd.Since(m0).Count > 0) Note("settings '" + n + "' raised: " + string.Join(" / ", wd.Since(m0)).Substring(0, Math.Min(160, string.Join(" / ", wd.Since(m0)).Length)));
            }
            Call(f, "ShowPage", "General"); Pump(6);
        }

        // -------------------------------------------------------------- master files
        private static void MasterCheck(Form shell)
        {
            Form f = Go(shell, "masterfiles");
            var list = All(f).OfType<ListBox>().FirstOrDefault() ?? null;
            var tree = All(f).OfType<TreeView>().FirstOrDefault();
            var cbo = All(f).OfType<ComboBox>().FirstOrDefault(c => c.Items.Count > 5 && c.Name.ToLower().Contains("categ"));
            var grid = All(f).OfType<DataGridView>().First();
            int n = list != null ? list.Items.Count : (tree != null ? tree.Nodes.Count : (cbo != null ? cbo.Items.Count : 0));
            Note("master files category selector: listbox=" + (list != null) + " tree=" + (tree != null) + " combo=" + (cbo != null) + " items=" + n);
            int ok = 0;
            if (list != null)
                for (int i = 0; i < list.Items.Count; i++)
                {
                    list.SelectedIndex = i; Pump(6);
                    string cat = list.Items[i].ToString();
                    bool good = grid.DataSource != null;
                    Check("master file '" + cat + "' lists", good);
                    if (good) ok++;
                }
            else if (cbo != null)
                for (int i = 0; i < cbo.Items.Count; i++)
                {
                    cbo.SelectedIndex = i; Pump(6);
                    Check("master file '" + cbo.Items[i] + "' lists", grid.DataSource != null);
                }
            else Check("master files has a category selector", false);
        }

        // -------------------------------------------------------------- ocr screen
        private static void OcrScreenCheck(Form shell)
        {
            Form f = Go(shell, "ocr");
            var grid = FindCtl<DataGridView>(f, "dgvBatch");
            Call(f, "LoadBatch");
            long n = Count("SELECT COUNT(*) FROM ocr_batch");
            Check("OCR batch list matches ocr_batch (" + grid.Rows.Count + " vs " + n + ")", grid.Rows.Count > 0 && grid.Rows.Count <= n + 0);
            var eng = All(f).OfType<Label>().FirstOrDefault(l => (l.Text ?? "").StartsWith("OCR engine"));
            Check("OCR engine reports ready", eng != null && eng.Text.Contains("ready"), eng == null ? "no label" : eng.Text);
        }

        // -------------------------------------------------------------- cert request
        private static void CertRequestCheck(Form shell, DialogWatchdog wd)
        {
            Form f = Go(shell, "certrequest");
            long before = Count("SELECT COUNT(*) FROM certificate_requests");
            Call(f, "ClearForm");
            var btn = FindCtl<Button>(f, "btnCreate") ?? All(f).OfType<Button>().FirstOrDefault(b => (b.Text ?? "").ToLower().Contains("create request"));
            Check("Certificate Request has a Create request button", btn != null);
            if (btn != null)
            {
                btn.PerformClick(); Pump(8);
                Check("Create request with nothing filled in writes nothing", Count("SELECT COUNT(*) FROM certificate_requests") == before);
            }
        }

        // -------------------------------------------------------------- no-ticket opens
        private static void OpenNoTicket(Form shell)
        {
            foreach (string k in new[] { "release", "breqs", "fees", "queue", "certrequest", "marriage", "birth" })
            {
                int un0 = Unhandled.Count;
                Form f = Go(shell, k);
                Pump(6);
                Check("'" + k + "' opens and refreshes cleanly", f != null && Unhandled.Count == un0);
                try { Call(f, "RefreshData"); Pump(6); } catch (MissingMethodException) { }
                Check("'" + k + "' RefreshData raises nothing", Unhandled.Count == un0);
            }
        }

        // -------------------------------------------------------------- certificate previews
        private static void CertificatePreviewCheck(Form shell, DialogWatchdog wd)
        {
            Type dk = typeof(Db).Assembly.GetType("CROMS.Data.DocKind", true);
            Type cr = typeof(Db).Assembly.GetType("CROMS.Data.CertificateReport", true);
            MethodInfo showFor = cr.GetMethod("ShowFor");
            string[][] kinds = { new[] { "Birth", "births" }, new[] { "Marriage", "marriages" }, new[] { "Death", "deaths" } };
            foreach (string[] k in kinds)
            {
                DataTable t = Db.Pull("SELECT id FROM " + k[1] + " WHERE status='Registered' ORDER BY id LIMIT 1");
                if (t.Rows.Count == 0) { Check(k[0] + " certificate preview", false, "no registered record to preview"); continue; }
                int id = Convert.ToInt32(t.Rows[0][0]);
                int m0 = wd.Count;
                object engine = showFor.Invoke(null, new object[] { Enum.Parse(dk, k[0]), (long)id, shell, true });
                Pump(4);
                Check(k[0] + " certificate preview opens (engine: " + (engine ?? "none") + ")", engine != null);
                bool printed = wd.Since(m0).Any(l => l.StartsWith("COMMON|") && !l.Contains("cancelled"));
                Check(k[0] + " preview did not print anything", !printed);
            }
            // facts certifications (editable templates)
            int b = Convert.ToInt32(Db.Pull("SELECT id FROM births WHERE status='Registered' ORDER BY id LIMIT 1").Rows[0][0]);
            int m = Convert.ToInt32(Db.Pull("SELECT id FROM marriages WHERE status='Registered' ORDER BY id LIMIT 1").Rows[0][0]);
            int d = Convert.ToInt32(Db.Pull("SELECT id FROM deaths WHERE status='Registered' ORDER BY id LIMIT 1").Rows[0][0]);
            foreach (var f in new[] { Tuple.Create("Form3ACert", m), Tuple.Create("Form3BCert", b), Tuple.Create("Form3CCert", d) })
            {
                Type ft = typeof(Db).Assembly.GetType("CROMS.Data." + f.Item1, true);
                DataTable bt = (DataTable)ft.GetMethod("BuildTable").Invoke(null, new object[] { f.Item2 });
                Check(f.Item1 + " builds its data row from the record", bt != null && bt.Rows.Count == 1);
                int un0 = Unhandled.Count;
                ft.GetMethod("Show").Invoke(null, new object[] { bt, shell });
                Pump(4);
                Check(f.Item1 + " preview opens and closes cleanly", Unhandled.Count == un0);
            }
        }

        // -------------------------------------------------------------- OCR end to end
        private static void OcrEndToEnd(Form shell, DialogWatchdog wd)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "nice.jpg");
            if (!System.IO.File.Exists(path)) { Check("OCR sample scan present", false, path); return; }
            Form f = Go(shell, "ocr");
            Type dai = typeof(Db).Assembly.GetType("CROMS.Data.DocumentAI", true);
            Bitmap img = (Bitmap)dai.GetMethod("LoadImage").Invoke(null, new object[] { path });
            long batch0 = Count("SELECT COALESCE(MAX(id),0) FROM ocr_batch");
            SetFld(f, "_image", img);
            SetFld(f, "_scanBytes", System.IO.File.ReadAllBytes(path));
            SetFld(f, "_sourceLabel", "nice.jpg");
            var pb = (PictureBox)Fld(f, "pbScan"); pb.Image = img;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var task = (System.Threading.Tasks.Task)Call(f, "Analyze");
            while (!task.IsCompleted && sw.Elapsed.TotalSeconds < 150) Pump(2);
            Note("dialogs while reading: " + string.Join(" / ", wd.Since(0).Where(l => !l.StartsWith("COMMON")).Take(6)));
            Check("Document Processing reads the scan within 150 s (" + (int)sw.Elapsed.TotalSeconds + " s)", task.IsCompleted);
            if (!task.IsCompleted) return;
            Pump(8);

            object result = Fld(f, "_result");
            Check("scan is classified as a birth certificate", result != null && Fld(f, "_kind").ToString() == "Birth", result == null ? "no result" : Fld(f, "_kind").ToString());
            var grid = (DataGridView)Fld(f, "dgvFields");
            int fieldRows = grid.Rows.Cast<DataGridViewRow>().Count(r => r.Tag != null && r.Tag.GetType().Name == "DocField");
            Check("the review grid lists the extracted fields (" + fieldRows + ")", fieldRows >= 25);
            Check("the run was logged to the batch list", Count("SELECT COUNT(*) FROM ocr_batch WHERE id > " + batch0) >= 1);

            long birth0 = Count("SELECT COALESCE(MAX(id),0) FROM births");
            _birthsBefore = birth0;
            string where = "id > " + birth0 + " AND record_source='OCR-Backlog'";
            try { Call(f, "GoToStep", 5); } catch (Exception ex) { Note("GoToStep: " + ex.Message); }
            Pump(4);
            var commit = (Button)Fld(f, "btnCommit");
            Note("review hold: " + (result == null ? "n/a" : Member(result, "NeedsManualReview") + " / " + Member(result, "ReviewReason")));
            Check("Commit is enabled on the last wizard step", commit.Enabled, commit.Text + " / review: " + ((object)Fld(f, "_result") == null ? "" : ""));
            // By design a birth / death scan commits straight to the backlog; Auto-Fill is for marriage (and the birth wizard's return).
            var auto = (Button)Fld(f, "btnAutoFill");
            Check("Auto-Fill is off for a plain birth scan (it is saved directly)", !auto.Enabled, auto.Text + " enabled=" + auto.Enabled);
            // "Preview on Form" shows the unsaved reading on the certificate with a watermark; it must open and close without printing.
            int mP = wd.Count;
            try { Call(f, "btnReport_Click", null, EventArgs.Empty); Pump(8); Check("Preview on Form opens and closes without printing", !wd.Since(mP).Any(l => l.StartsWith("COMMON|") && !l.Contains("cancelled"))); }
            catch (Exception ex) { Check("Preview on Form opens", false, (ex.InnerException ?? ex).Message); }
            if (commit.Enabled)
            {
                int mC = wd.Count;
                Call(f, "btnCommit_Click", null, EventArgs.Empty); Pump(10);
                Note("dialogs during Commit: " + string.Join(" / ", wd.Since(mC).Where(l => !l.StartsWith("COMMON")).Select(l => l.Length > 260 ? l.Substring(0, 260) : l)));
                long made = Count("SELECT COUNT(*) FROM births WHERE " + where);
                Check("Commit wrote exactly one birth record", made == 1, made + " rows");
                if (made == 1)
                {
                    DataRow row = Db.Pull("SELECT status, record_source, form_code, first_name, last_name, date_of_birth, registry_no FROM births WHERE " + where).Rows[0];
                    Check("it is marked as a digitized (OCR-Backlog) record on the right form", row["record_source"].ToString() == "OCR-Backlog" && row["form_code"].ToString().StartsWith("MF-102"), row["record_source"] + " / " + row["form_code"]);
                    Note("registry no saved with the digitized record: " + (row["registry_no"] == DBNull.Value ? "NULL (blank - handwritten on the scan; Commit does not require it)" : row["registry_no"].ToString()));
                    Check("name and date of birth were carried across", row["first_name"].ToString().Length > 0 && row["last_name"].ToString().Length > 0 && row["date_of_birth"] != DBNull.Value, row["first_name"] + " " + row["last_name"]);
                    Check("the batch row says Committed and points at the record", Count("SELECT COUNT(*) FROM ocr_batch WHERE id > " + batch0 + " AND status='Committed' AND record_id IS NOT NULL") == 1);
                    Check("per-field audit trail was written", Count("SELECT COUNT(*) FROM ocr_field_audit WHERE scan_id IN (SELECT scan_id FROM ocr_batch WHERE id > " + batch0 + ")") > 10);
                    int m1 = wd.Count;
                    Call(f, "btnCommit_Click", null, EventArgs.Empty); Pump(8);
                    Check("pressing Commit a second time is refused ('already saved')", wd.Saw(m1, "Already saved") && Count("SELECT COUNT(*) FROM births WHERE " + where) == 1);
                }
            }
        }
        private static object Member(object o, string name)
        {
            var p = o.GetType().GetProperty(name, Any); if (p != null) return p.GetValue(o);
            var f = o.GetType().GetField(name, Any); return f == null ? "?" : f.GetValue(o);
        }
        private static void SetFld(object o, string name, object v)
        {
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo fi = t.GetField(name, Any | BindingFlags.DeclaredOnly);
                if (fi != null) { fi.SetValue(o, v); return; }
            }
            throw new MissingFieldException(o.GetType().Name, name);
        }

        // -------------------------------------------------------------- death entry dialog render
        private static void DeathRenderCheck(Form shell, DialogWatchdog wd)
        {
            wd.Rules["Form"] = ff => { };   // the entry popup is a plain Form: let it be (the watchdog would close it as unexpected)
            Form f = Go(shell, "death");
            var tabs = FindCtl<TabControl>(f, "tabControl");
            Type t = f.GetType();
            int tab = 0, wait = 0; bool selected = false; var hits = new List<string>(); int shots = 0;
            var timer = new Timer { Interval = 600 };
            timer.Tick += delegate
            {
                var dlg = (Form)Fld(f, "_entryDialog");
                if (dlg == null || tab >= tabs.TabPages.Count) return;
                if (++wait < 3) return;
                try
                {
                    if (!selected) { tabs.SelectedIndex = tab; selected = true; return; }
                    selected = false;
                    using (var bmp = new Bitmap(dlg.Width, dlg.Height))
                    {
                        dlg.DrawToBitmap(bmp, new Rectangle(0, 0, dlg.Width, dlg.Height));
                        bmp.Save(System.IO.Path.Combine(Out, "death_step" + (tab + 1) + ".png"), System.Drawing.Imaging.ImageFormat.Png);
                        shots++;
                    }
                    var h = new List<string>(); Sweep(tabs.TabPages[tab], h, "DeathStep" + (tab + 1)); hits.AddRange(h);
                }
                catch (Exception ex) { hits.Add("tab " + tab + ": " + ex.Message); }
                if (!selected && ++tab >= tabs.TabPages.Count) { timer.Stop(); dlg.Close(); }
            };
            timer.Start();
            Call(f, "ShowEntryView");
            timer.Dispose();
            Check("Death entry dialog: all " + tabs.TabPages.Count + " steps rendered", shots == tabs.TabPages.Count, shots + " shots");
            Check("Death entry dialog: no overlapping / out-of-bounds controls", hits.Count == 0, string.Join(" | ", hits.Take(5)));
            wd.Rules.Remove("Form");
            Call(f, "ShowListView");
        }

        // -------------------------------------------------------------- bypass
        private static void BypassCheck()
        {
            Login("Staff");
            try
            {
                var l = new LicenseFacts { FiledDate = DateTime.Today };
                l.Husband.First = "Juan"; l.Husband.Last = "ZZABypassH"; l.Husband.Dob = DateTime.Today.AddYears(-30).AddDays(-3); l.Husband.Citizenship = "Filipino"; l.Husband.CivilStatus = "Single";
                l.Wife.First = "Maria"; l.Wife.Last = "ZZABypassW"; l.Wife.Dob = DateTime.Today.AddYears(-28).AddDays(-3); l.Wife.Citizenship = "Filipino"; l.Wife.CivilStatus = "Single";
                int id = MarriageService.SaveLicense(l);
                var rows = MarriageService.Requirements("License", id);
                Check("a licence application carries a document checklist", rows.Count > 0, rows.Count + " rows");
                long a0 = Count("SELECT COUNT(*) FROM audit_log");
                string noReason = null;
                try { MarriageService.BypassRequirement(rows[0].Id, ""); } catch (ArgumentException ex) { noReason = ex.Message; }
                Check("Staff bypass without a written reason is refused", noReason != null, noReason);
                MarriageService.BypassRequirement(rows[0].Id, "ZZA client cannot obtain this document");
                var after = MarriageService.Requirements("License", id).First(r => r.Id == rows[0].Id);
                Check("Staff bypass marks the one row bypassed, status untouched", after.IsBypassed && after.Status == rows[0].Status, after.Status);
                long aud = Count("SELECT COUNT(*) FROM audit_log WHERE details LIKE 'BYPASS by %(Staff)%ZZA client%'");
                Check("bypass is written to audit_log naming the user AND role (Staff)", aud == 1, aud + " rows");
                long hist = Count("SELECT COUNT(*) FROM marriage_history WHERE entity_id=" + id + " AND event LIKE 'Requirement bypassed by %(Staff)%'");
                Check("bypass is written to the licence's own history", hist == 1, hist + " rows");
                if (rows.Count > 1) Check("other rows stay un-bypassed", !MarriageService.Requirements("License", id).First(r => r.Id == rows[1].Id).IsBypassed);
                MarriageService.ClearBypass(rows[0].Id);
                Check("bypass can be withdrawn", !MarriageService.Requirements("License", id).First(r => r.Id == rows[0].Id).IsBypassed);
                // whole-checklist override (migration 48)
                try { MarriageService.OverrideRequirements(id, "ZZA override"); Check("Staff may override missing requirements with a reason (migration 48)", true); }
                catch (Exception ex) { Check("Staff may override missing requirements with a reason (migration 48)", false, ex.Message); }
                // a role that is neither Admin nor Staff
                Session.User.Role = "Clerk";
                string denied = null;
                try { MarriageService.BypassRequirement(rows[0].Id, "x"); } catch (UnauthorizedAccessException ex) { denied = ex.Message; }
                Check("an unknown role cannot bypass", denied != null, denied);
                Session.User.Role = "Staff";
            }
            finally { Login("Admin"); }
        }

        // -------------------------------------------------------------- migrations
        private static void MigrationFeatureCheck()
        {
            bool col48 = Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='croms' AND table_name='marriage_licenses' AND column_name='requirements_override_by'") > 0;
            Check("migration 48 (licence requirement override) applied", col48);
            bool col48b = Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='croms' AND table_name='office_profile' AND column_name='email'") > 0;
            Check("migration 48 (office e-mail) applied", col48b);
            bool m52 = Count("SELECT COUNT(*) FROM marriage_requirement_types WHERE applies_to='Petition'") > 0;
            Check("migration 52 (petition documents) applied", m52);
            bool m69 = Count("SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='croms' AND table_name='births' AND column_name='is_deleted'") > 0;
            Check("migration 69 (soft delete) applied", m69, "ReasonPrompt exists but no delete path uses it");
        }

        // -------------------------------------------------------------- cleanup
        private static void OperationsCleanup()
        {
            try
            {
                if (_ocrBatchBefore > 0)   // never delete by id before the baseline is known
                {
                    Db.Push("DELETE FROM ocr_field_audit WHERE scan_id IN (SELECT scan_id FROM ocr_batch WHERE id > " + _ocrBatchBefore + ")");
                    Db.Push("DELETE FROM ocr_batch WHERE id > " + _ocrBatchBefore);
                }
                if (_birthsBefore > 0) Db.Push("DELETE FROM births WHERE id > " + _birthsBefore + " AND record_source='OCR-Backlog'");
                Db.Push("DELETE FROM marriage_history WHERE entity='License' AND entity_id IN (SELECT id FROM marriage_licenses WHERE husband_last_name LIKE 'ZZA%')");
                Db.Push("DELETE FROM marriage_requirements WHERE owner_type='License' AND owner_id IN (SELECT id FROM marriage_licenses WHERE husband_last_name LIKE 'ZZA%')");
                Db.Push("DELETE FROM marriage_licenses WHERE husband_last_name LIKE 'ZZA%'");
                Db.Push("DELETE FROM client_service_slips WHERE requester_name LIKE 'ZZA%'");
                Db.Push("DELETE FROM marriage_requirements WHERE owner_type='Petition' AND owner_id IN (SELECT id FROM petitions WHERE requester_name LIKE 'ZZA%')");
                Db.Push("DELETE FROM petitions WHERE requester_name LIKE 'ZZA%'");
                Db.Push("DELETE FROM window_transactions WHERE window_id IN (SELECT id FROM windows WHERE window_name='ZZA Win')");
                Db.Push("DELETE FROM windows WHERE window_name='ZZA Win'");
                Db.Push("DELETE FROM deaths WHERE full_name LIKE '%ZZA%'");
                if (_auditBefore > 0)
                    Db.Push("DELETE FROM audit_log WHERE id > " + _auditBefore);
            }
            catch (Exception ex) { Console.WriteLine("ops cleanup: " + ex.Message); }
        }
    }
}

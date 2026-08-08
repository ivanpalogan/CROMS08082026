# Handoff — Certificate Request: Split Client Name + Refresh Button

**Module:** Client Services → Certificate Request
**Files touched:** `CROMS/Forms/CertificateRequestForm.Designer.cs`, `CROMS/Forms/CertificateRequestForm.cs`
**Do this in Claude Code** (these are `.cs` edits).

## Summary of change
- Replace the single **Client Name** textbox with three side-by-side fields: **First**, **Middle**, **Last** (on the existing name row, using `PlaceholderText`).
- First + Last required; Middle optional.
- The three parts are joined into `"First Middle Last"` and stored in the existing `transactions.client_name` column — **no DB schema change**. The Recent Requests grid "Client" column keeps working.
- Add a **Refresh** button next to Clear that reloads the Recent Requests grid.

## Design notes / confirmations
- `PlaceholderText` requires .NET Framework 4.7.2+. Project targets 4.8 → OK. If placeholders don't render, swap to real labels ("First / Middle / Last").
- If separate `first_name` / `middle_name` / `last_name` DB columns are wanted later (better for search/reports), that's a schema + query change — not covered here.

---

## 1) CertificateRequestForm.Designer.cs

### a) Replace the `// txtClient` control block with three textboxes
```csharp
            // 
            // txtFirst
            // 
            this.txtFirst.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtFirst.Location = new System.Drawing.Point(146, 35);
            this.txtFirst.Name = "txtFirst";
            this.txtFirst.Size = new System.Drawing.Size(178, 25);
            this.txtFirst.TabIndex = 1;
            this.txtFirst.PlaceholderText = "First";
            // 
            // txtMiddle
            // 
            this.txtMiddle.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtMiddle.Location = new System.Drawing.Point(334, 35);
            this.txtMiddle.Name = "txtMiddle";
            this.txtMiddle.Size = new System.Drawing.Size(178, 25);
            this.txtMiddle.TabIndex = 2;
            this.txtMiddle.PlaceholderText = "Middle";
            // 
            // txtLast
            // 
            this.txtLast.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.txtLast.Location = new System.Drawing.Point(522, 35);
            this.txtLast.Name = "txtLast";
            this.txtLast.Size = new System.Drawing.Size(182, 25);
            this.txtLast.TabIndex = 3;
            this.txtLast.PlaceholderText = "Last";
```

### b) Add a `// btnRefresh` control block (after the `btnClear` block)
```csharp
            // 
            // btnRefresh
            // 
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnRefresh.Location = new System.Drawing.Point(411, 256);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(103, 35);
            this.btnRefresh.TabIndex = 16;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
```

### c) In `InitializeComponent()` field construction, replace `this.txtClient = new ...;`
```csharp
            this.txtFirst = new System.Windows.Forms.TextBox();
            this.txtMiddle = new System.Windows.Forms.TextBox();
            this.txtLast = new System.Windows.Forms.TextBox();
            this.btnRefresh = new System.Windows.Forms.Button();
```

### d) In the `grpDetails.Controls.Add(...)` list, replace `this.grpDetails.Controls.Add(this.txtClient);`
```csharp
            this.grpDetails.Controls.Add(this.txtFirst);
            this.grpDetails.Controls.Add(this.txtMiddle);
            this.grpDetails.Controls.Add(this.txtLast);
            this.grpDetails.Controls.Add(this.btnRefresh);
```

### e) In the bottom private field declarations, replace `private System.Windows.Forms.TextBox txtClient;`
```csharp
        private System.Windows.Forms.TextBox txtFirst;
        private System.Windows.Forms.TextBox txtMiddle;
        private System.Windows.Forms.TextBox txtLast;
        private System.Windows.Forms.Button btnRefresh;
```

### f) (Optional, cosmetic) Fix tab order
`txtFirst/Middle/Last` now use TabIndex 1–3, which collide with `cboCertType` (3). Renumber the later controls (`cboCertType = 4`, `cboRecordType = 5`, `cboRecord = 6`, ... ) if tab order matters.

---

## 2) CertificateRequestForm.cs

### a) Add `using System.Linq;` at the top.

### b) Add helper + refresh handler
```csharp
        /// <summary>Joins the three name parts into "First Middle Last", skipping blanks.</summary>
        private string FullClientName()
        {
            string[] parts = { txtFirst.Text.Trim(), txtMiddle.Text.Trim(), txtLast.Text.Trim() };
            return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        }

        private void btnRefresh_Click(object sender, EventArgs e) => LoadRequests();
```

### c) In `btnCreate_Click`, replace the validation block
```csharp
            if (string.IsNullOrWhiteSpace(txtFirst.Text) || string.IsNullOrWhiteSpace(txtLast.Text))
            {
                MessageBox.Show("First name and last name are required.", "Missing data",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
```

### d) In `btnCreate_Click`, change the transaction insert client param
```csharp
                    new MySqlParameter("@client", FullClientName()));
```
(was `txtClient.Text.Trim()`)

### e) In `ClearForm`, replace `txtClient.Clear();`
```csharp
            txtFirst.Clear();
            txtMiddle.Clear();
            txtLast.Clear();
```

---

## Build check
- Compile; open Certificate Request.
- Confirm three name boxes render with placeholders and the Refresh button sits after Clear.
- Create a request with First + Last only, then First + Middle + Last; verify the grid "Client" column shows the joined name.
- Click Refresh after creating a record from another screen to confirm the grid reloads.

namespace CROMS.Forms
{
    partial class PetitionsForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Every control on this screen is built responsively in the code-behind
        /// (<c>BuildLayout()</c>) rather than declared here — the same pattern this codebase
        /// already uses for Release &amp; Claim, Queue Management and Certificate Request, so the
        /// two-card layout can dock/anchor/reflow instead of sitting at fixed Designer points, and
        /// so a Visual Studio designer-regeneration (which has silently dropped hand-added
        /// controls on this project before) has nothing here to drop.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // PetitionsForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = CROMS.Modules.UiTheme.PageBg;
            this.ClientSize = new System.Drawing.Size(1449, 900);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "PetitionsForm";
            this.Text = "Petitions & Case Tracking";
            this.ResumeLayout(false);
        }

        #endregion
    }
}

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    partial class WpfBasedPropertyPage
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer _components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (_components != null))
            {
                _components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.wpfHostPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // wpfHostPanel
            // 
            this.wpfHostPanel.AutoSize = true;
            this.wpfHostPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wpfHostPanel.Location = new System.Drawing.Point(0, 0);
            this.wpfHostPanel.Name = "wpfHostPanel";
            this.wpfHostPanel.TabIndex = 0;
            // 
            // WpfBasedPropertyPage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowOnly;
            this.Controls.Add(this.wpfHostPanel);
            this.Name = "WpfBasedPropertyPage";
            this.Load += new System.EventHandler(this.WpfPropertyPage_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel wpfHostPanel;
    }
}
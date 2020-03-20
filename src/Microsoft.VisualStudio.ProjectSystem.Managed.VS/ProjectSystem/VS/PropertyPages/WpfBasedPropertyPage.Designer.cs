// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Microsoft.VisualStudio.ProjectSystem.VS.PropertyPages
{
    partial class WpfBasedPropertyPage
    {
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

        private System.Windows.Forms.Panel wpfHostPanel;
    }
}

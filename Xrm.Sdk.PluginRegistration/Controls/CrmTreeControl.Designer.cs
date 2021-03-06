namespace Xrm.Sdk.PluginRegistration.Controls
{
    partial class CrmTreeControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
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
            this.trvPlugins = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // trvPlugins
            // 
            this.trvPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.trvPlugins.Location = new System.Drawing.Point(0, 0);
            this.trvPlugins.Name = "trvPlugins";
            this.trvPlugins.Size = new System.Drawing.Size(208, 110);
            this.trvPlugins.TabIndex = 2;
            this.trvPlugins.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.trvPlugins_AfterCheck);
            this.trvPlugins.AfterLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.trvPlugins_AfterLabelEdit);
            this.trvPlugins.DoubleClick += new System.EventHandler(this.trvPlugins_DoubleClick);
            this.trvPlugins.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trvPlugins_AfterSelect);
            this.trvPlugins.MouseDown += new System.Windows.Forms.MouseEventHandler(this.trvPlugins_MouseDown);
            this.trvPlugins.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.trvPlugins_KeyPress);
            this.trvPlugins.KeyUp += new System.Windows.Forms.KeyEventHandler(this.trvPlugins_KeyUp);
            this.trvPlugins.BeforeLabelEdit += new System.Windows.Forms.NodeLabelEditEventHandler(this.trvPlugins_BeforeLabelEdit);
            this.trvPlugins.KeyDown += new System.Windows.Forms.KeyEventHandler(this.trvPlugins_KeyDown);
            this.trvPlugins.Click += new System.EventHandler(this.trvPlugins_Click);
            // 
            // CrmTreeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.trvPlugins);
            this.Name = "CrmTreeControl";
            this.Size = new System.Drawing.Size(208, 110);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView trvPlugins;

    }
}

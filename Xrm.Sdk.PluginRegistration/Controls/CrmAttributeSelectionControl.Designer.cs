namespace Xrm.Sdk.PluginRegistration.Controls
{
    partial class CrmAttributeSelectionControl
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
            this.btnSelect = new System.Windows.Forms.Button();
            this.txtAttributes = new System.Windows.Forms.TextBox();
            this.txtDisabledMessage = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnSelect
            // 
            this.btnSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelect.Location = new System.Drawing.Point(326, 0);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(36, 20);
            this.btnSelect.TabIndex = 14;
            this.btnSelect.Text = "&...";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // txtAttributes
            // 
            this.txtAttributes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAttributes.Location = new System.Drawing.Point(0, 0);
            this.txtAttributes.Multiline = true;
            this.txtAttributes.Name = "txtAttributes";
            this.txtAttributes.ReadOnly = true;
            this.txtAttributes.Size = new System.Drawing.Size(320, 20);
            this.txtAttributes.TabIndex = 13;
            this.txtAttributes.WordWrap = false;
            // 
            // txtDisabledMessage
            // 
            this.txtDisabledMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDisabledMessage.Location = new System.Drawing.Point(0, 0);
            this.txtDisabledMessage.Name = "txtDisabledMessage";
            this.txtDisabledMessage.Size = new System.Drawing.Size(362, 20);
            this.txtDisabledMessage.TabIndex = 15;
            this.txtDisabledMessage.Visible = false;
            // 
            // CrmAttributeSelectionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.txtAttributes);
            this.Controls.Add(this.txtDisabledMessage);
            this.Name = "CrmAttributeSelectionControl";
            this.Size = new System.Drawing.Size(362, 20);
            this.EnabledChanged += new System.EventHandler(this.CrmAttributeSelectionControl_EnabledChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.TextBox txtAttributes;
        private System.Windows.Forms.TextBox txtDisabledMessage;

    }
}

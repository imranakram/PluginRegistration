namespace Xrm.Sdk.PluginRegistration.Forms
{
    partial class ManagedIdentityForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTarget = new System.Windows.Forms.Label();
            this.lblIdentity = new System.Windows.Forms.Label();
            this.cboIdentities = new System.Windows.Forms.ComboBox();
            this.btnNew = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.grpDetails = new System.Windows.Forms.GroupBox();
            this.lblDetails = new System.Windows.Forms.Label();
            this.lblSigning = new System.Windows.Forms.Label();
            this.lnkDocs = new System.Windows.Forms.LinkLabel();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpDetails.SuspendLayout();
            this.SuspendLayout();
            //
            // lblTarget
            //
            this.lblTarget.AutoEllipsis = true;
            this.lblTarget.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblTarget.Location = new System.Drawing.Point(12, 12);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Size = new System.Drawing.Size(456, 32);
            this.lblTarget.TabIndex = 0;
            //
            // lblIdentity
            //
            this.lblIdentity.AutoSize = true;
            this.lblIdentity.Location = new System.Drawing.Point(12, 53);
            this.lblIdentity.Name = "lblIdentity";
            this.lblIdentity.Size = new System.Drawing.Size(91, 13);
            this.lblIdentity.TabIndex = 1;
            this.lblIdentity.Text = "Managed identity:";
            //
            // cboIdentities
            //
            this.cboIdentities.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboIdentities.FormattingEnabled = true;
            this.cboIdentities.Location = new System.Drawing.Point(112, 50);
            this.cboIdentities.Name = "cboIdentities";
            this.cboIdentities.Size = new System.Drawing.Size(356, 21);
            this.cboIdentities.TabIndex = 2;
            this.cboIdentities.SelectedIndexChanged += new System.EventHandler(this.cboIdentities_SelectedIndexChanged);
            //
            // btnNew
            //
            this.btnNew.Location = new System.Drawing.Point(112, 77);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(75, 23);
            this.btnNew.TabIndex = 3;
            this.btnNew.Text = "&New...";
            this.btnNew.UseVisualStyleBackColor = true;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            //
            // btnEdit
            //
            this.btnEdit.Location = new System.Drawing.Point(193, 77);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 23);
            this.btnEdit.TabIndex = 4;
            this.btnEdit.Text = "&Edit...";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            //
            // btnDelete
            //
            this.btnDelete.Location = new System.Drawing.Point(274, 77);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 5;
            this.btnDelete.Text = "&Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            //
            // grpDetails
            //
            this.grpDetails.Controls.Add(this.lblDetails);
            this.grpDetails.Location = new System.Drawing.Point(12, 110);
            this.grpDetails.Name = "grpDetails";
            this.grpDetails.Size = new System.Drawing.Size(456, 90);
            this.grpDetails.TabIndex = 6;
            this.grpDetails.TabStop = false;
            this.grpDetails.Text = "Details";
            //
            // lblDetails
            //
            this.lblDetails.Location = new System.Drawing.Point(8, 18);
            this.lblDetails.Name = "lblDetails";
            this.lblDetails.Size = new System.Drawing.Size(440, 64);
            this.lblDetails.Padding = new System.Windows.Forms.Padding(4);
            this.lblDetails.TabIndex = 0;
            //
            // lblSigning
            //
            this.lblSigning.Location = new System.Drawing.Point(12, 208);
            this.lblSigning.Name = "lblSigning";
            this.lblSigning.Size = new System.Drawing.Size(456, 30);
            this.lblSigning.TabIndex = 7;
            this.lblSigning.Text = "The assembly or package must be signed with a certificate, and the app registration or managed identity in Azure needs a matching federated credential.";
            //
            // lnkDocs
            //
            this.lnkDocs.AutoSize = true;
            this.lnkDocs.Location = new System.Drawing.Point(12, 240);
            this.lnkDocs.Name = "lnkDocs";
            this.lnkDocs.Size = new System.Drawing.Size(220, 13);
            this.lnkDocs.TabIndex = 8;
            this.lnkDocs.TabStop = true;
            this.lnkDocs.Text = "How to set up managed identity (Microsoft Learn)";
            this.lnkDocs.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkDocs_LinkClicked);
            //
            // btnOk
            //
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(312, 271);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 10;
            this.btnOk.Text = "&Save";
            this.btnOk.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(393, 271);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // ManagedIdentityForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(480, 306);
            this.Controls.Add(this.lblTarget);
            this.Controls.Add(this.lblIdentity);
            this.Controls.Add(this.cboIdentities);
            this.Controls.Add(this.btnNew);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.grpDetails);
            this.Controls.Add(this.lblSigning);
            this.Controls.Add(this.lnkDocs);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManagedIdentityForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Managed Identity";
            this.Load += new System.EventHandler(this.ManagedIdentityForm_Load);
            this.grpDetails.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.Label lblIdentity;
        private System.Windows.Forms.ComboBox cboIdentities;
        private System.Windows.Forms.Button btnNew;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.GroupBox grpDetails;
        private System.Windows.Forms.Label lblDetails;
        private System.Windows.Forms.Label lblSigning;
        private System.Windows.Forms.LinkLabel lnkDocs;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}

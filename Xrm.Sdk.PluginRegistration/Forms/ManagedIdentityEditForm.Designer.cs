namespace Xrm.Sdk.PluginRegistration.Forms
{
    partial class ManagedIdentityEditForm
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
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblApplicationId = new System.Windows.Forms.Label();
            this.txtApplicationId = new System.Windows.Forms.TextBox();
            this.lblTenantId = new System.Windows.Forms.Label();
            this.txtTenantId = new System.Windows.Forms.TextBox();
            this.lblScope = new System.Windows.Forms.Label();
            this.cboScope = new System.Windows.Forms.ComboBox();
            this.lblVersion = new System.Windows.Forms.Label();
            this.numVersion = new System.Windows.Forms.NumericUpDown();
            this.lblVersionHint = new System.Windows.Forms.Label();
            this.lblSolution = new System.Windows.Forms.Label();
            this.txtSolution = new System.Windows.Forms.TextBox();
            this.btnSolution = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numVersion)).BeginInit();
            this.SuspendLayout();
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(38, 13);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name:";
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(150, 12);
            this.txtName.MaxLength = 100;
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(290, 20);
            this.txtName.TabIndex = 1;
            //
            // lblApplicationId
            //
            this.lblApplicationId.AutoSize = true;
            this.lblApplicationId.Location = new System.Drawing.Point(12, 41);
            this.lblApplicationId.Name = "lblApplicationId";
            this.lblApplicationId.Size = new System.Drawing.Size(128, 13);
            this.lblApplicationId.TabIndex = 2;
            this.lblApplicationId.Text = "Application (client) ID:";
            //
            // txtApplicationId
            //
            this.txtApplicationId.Location = new System.Drawing.Point(150, 38);
            this.txtApplicationId.Name = "txtApplicationId";
            this.txtApplicationId.Size = new System.Drawing.Size(290, 20);
            this.txtApplicationId.TabIndex = 3;
            //
            // lblTenantId
            //
            this.lblTenantId.AutoSize = true;
            this.lblTenantId.Location = new System.Drawing.Point(12, 67);
            this.lblTenantId.Name = "lblTenantId";
            this.lblTenantId.Size = new System.Drawing.Size(58, 13);
            this.lblTenantId.TabIndex = 4;
            this.lblTenantId.Text = "Tenant ID:";
            //
            // txtTenantId
            //
            this.txtTenantId.Location = new System.Drawing.Point(150, 64);
            this.txtTenantId.Name = "txtTenantId";
            this.txtTenantId.Size = new System.Drawing.Size(290, 20);
            this.txtTenantId.TabIndex = 5;
            //
            // lblScope
            //
            this.lblScope.AutoSize = true;
            this.lblScope.Location = new System.Drawing.Point(12, 93);
            this.lblScope.Name = "lblScope";
            this.lblScope.Size = new System.Drawing.Size(76, 13);
            this.lblScope.TabIndex = 6;
            this.lblScope.Text = "Subject scope:";
            //
            // cboScope
            //
            this.cboScope.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboScope.FormattingEnabled = true;
            this.cboScope.Location = new System.Drawing.Point(150, 90);
            this.cboScope.Name = "cboScope";
            this.cboScope.Size = new System.Drawing.Size(150, 21);
            this.cboScope.TabIndex = 7;
            //
            // lblVersion
            //
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(12, 120);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(45, 13);
            this.lblVersion.TabIndex = 8;
            this.lblVersion.Text = "Version:";
            //
            // numVersion
            //
            this.numVersion.Location = new System.Drawing.Point(150, 117);
            this.numVersion.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            this.numVersion.Name = "numVersion";
            this.numVersion.Size = new System.Drawing.Size(50, 20);
            this.numVersion.TabIndex = 9;
            //
            // lblVersionHint
            //
            this.lblVersionHint.Location = new System.Drawing.Point(206, 113);
            this.lblVersionHint.Name = "lblVersionHint";
            this.lblVersionHint.Size = new System.Drawing.Size(234, 28);
            this.lblVersionHint.TabIndex = 10;
            this.lblVersionHint.Text = "Microsoft recommends version 2 (federated credential built from the certificate DN).";
            //
            // lblSolution
            //
            this.lblSolution.AutoSize = true;
            this.lblSolution.Location = new System.Drawing.Point(12, 150);
            this.lblSolution.Name = "lblSolution";
            this.lblSolution.Size = new System.Drawing.Size(48, 13);
            this.lblSolution.TabIndex = 11;
            this.lblSolution.Text = "Solution:";
            //
            // txtSolution
            //
            this.txtSolution.Location = new System.Drawing.Point(150, 147);
            this.txtSolution.Name = "txtSolution";
            this.txtSolution.ReadOnly = true;
            this.txtSolution.Size = new System.Drawing.Size(256, 20);
            this.txtSolution.TabIndex = 12;
            //
            // btnSolution
            //
            this.btnSolution.Location = new System.Drawing.Point(412, 146);
            this.btnSolution.Name = "btnSolution";
            this.btnSolution.Size = new System.Drawing.Size(28, 22);
            this.btnSolution.TabIndex = 13;
            this.btnSolution.Text = "...";
            this.btnSolution.UseVisualStyleBackColor = true;
            this.btnSolution.Click += new System.EventHandler(this.btnSolution_Click);
            //
            // btnOk
            //
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(284, 183);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 14;
            this.btnOk.Text = "&Save";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(365, 183);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // ManagedIdentityEditForm
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(452, 218);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblApplicationId);
            this.Controls.Add(this.txtApplicationId);
            this.Controls.Add(this.lblTenantId);
            this.Controls.Add(this.txtTenantId);
            this.Controls.Add(this.lblScope);
            this.Controls.Add(this.cboScope);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.numVersion);
            this.Controls.Add(this.lblVersionHint);
            this.Controls.Add(this.lblSolution);
            this.Controls.Add(this.txtSolution);
            this.Controls.Add(this.btnSolution);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManagedIdentityEditForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Managed Identity";
            ((System.ComponentModel.ISupportInitialize)(this.numVersion)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblApplicationId;
        private System.Windows.Forms.TextBox txtApplicationId;
        private System.Windows.Forms.Label lblTenantId;
        private System.Windows.Forms.TextBox txtTenantId;
        private System.Windows.Forms.Label lblScope;
        private System.Windows.Forms.ComboBox cboScope;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.NumericUpDown numVersion;
        private System.Windows.Forms.Label lblVersionHint;
        private System.Windows.Forms.Label lblSolution;
        private System.Windows.Forms.TextBox txtSolution;
        private System.Windows.Forms.Button btnSolution;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}

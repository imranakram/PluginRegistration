namespace Xrm.Sdk.PluginRegistration.Forms
{
    partial class WebHookRegistrationForm
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
            this.components = new System.ComponentModel.Container();
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtEndpointUrl = new System.Windows.Forms.TextBox();
            this.lblEndpointUrl = new System.Windows.Forms.Label();
            this.lblAuth = new System.Windows.Forms.Label();
            this.lblAuthHint = new System.Windows.Forms.Label();
            this.cmbAuth = new System.Windows.Forms.ComboBox();
            this.lblValue = new System.Windows.Forms.Label();
            this.txtValue = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.dgvKeyValue = new System.Windows.Forms.DataGridView();
            this.Keys = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Values = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnGenerateWebhookSite = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.dgvKeyValue)).BeginInit();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(12, 67);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(51, 20);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Name";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(161, 64);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(627, 26);
            this.txtName.TabIndex = 1;
            // 
            // txtEndpointUrl
            // 
            this.txtEndpointUrl.Location = new System.Drawing.Point(161, 115);
            this.txtEndpointUrl.Name = "txtEndpointUrl";
            this.txtEndpointUrl.Size = new System.Drawing.Size(627, 26);
            this.txtEndpointUrl.TabIndex = 3;
            // 
            // lblEndpointUrl
            // 
            this.lblEndpointUrl.AutoSize = true;
            this.lblEndpointUrl.Location = new System.Drawing.Point(12, 118);
            this.lblEndpointUrl.Name = "lblEndpointUrl";
            this.lblEndpointUrl.Size = new System.Drawing.Size(110, 20);
            this.lblEndpointUrl.TabIndex = 2;
            this.lblEndpointUrl.Text = "Endpoint URL";
            // 
            // lblAuth
            // 
            this.lblAuth.AutoSize = true;
            this.lblAuth.Location = new System.Drawing.Point(12, 172);
            this.lblAuth.Name = "lblAuth";
            this.lblAuth.Size = new System.Drawing.Size(112, 20);
            this.lblAuth.TabIndex = 4;
            this.lblAuth.Text = "Authentication";
            // 
            // cmbAuth
            // 
            this.cmbAuth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAuth.FormattingEnabled = true;
            this.cmbAuth.Items.AddRange(new object[] {
            "HttpHeader",
            "WebhookKey",
            "HttpQueryString"});
            this.cmbAuth.Location = new System.Drawing.Point(161, 169);
            this.cmbAuth.Name = "cmbAuth";
            this.cmbAuth.Size = new System.Drawing.Size(627, 28);
            this.cmbAuth.TabIndex = 5;
            this.cmbAuth.SelectedIndexChanged += new System.EventHandler(this.cmbAuth_SelectedIndexChanged);
            // 
            // lblAuthHint
            // 
            this.lblAuthHint.AutoEllipsis = true;
            this.lblAuthHint.Location = new System.Drawing.Point(12, 202);
            this.lblAuthHint.Name = "lblAuthHint";
            this.lblAuthHint.Size = new System.Drawing.Size(776, 20);
            this.lblAuthHint.TabIndex = 12;
            this.lblAuthHint.Visible = false;
            // 
            // lblValue
            // 
            this.lblValue.AutoSize = true;
            this.lblValue.Location = new System.Drawing.Point(12, 230);
            this.lblValue.Name = "lblValue";
            this.lblValue.Size = new System.Drawing.Size(50, 20);
            this.lblValue.TabIndex = 6;
            this.lblValue.Text = "Value";
            // 
            // txtValue
            // 
            this.txtValue.Location = new System.Drawing.Point(161, 227);
            this.txtValue.Name = "txtValue";
            this.txtValue.Size = new System.Drawing.Size(627, 26);
            this.txtValue.TabIndex = 7;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(598, 445);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(91, 35);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(697, 445);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(91, 35);
            this.btnCancel.TabIndex = 9;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // dgvKeyValue
            // 
            this.dgvKeyValue.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvKeyValue.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvKeyValue.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Keys,
            this.Values});
            this.dgvKeyValue.Location = new System.Drawing.Point(16, 226);
            this.dgvKeyValue.Name = "dgvKeyValue";
            this.dgvKeyValue.RowHeadersWidth = 62;
            this.dgvKeyValue.RowTemplate.Height = 28;
            this.dgvKeyValue.Size = new System.Drawing.Size(772, 211);
            this.dgvKeyValue.TabIndex = 10;
            // 
            // Keys
            // 
            this.Keys.HeaderText = "Keys";
            this.Keys.MinimumWidth = 8;
            this.Keys.Name = "Keys";
            // 
            // Values
            // 
            this.Values.HeaderText = "Values";
            this.Values.MinimumWidth = 8;
            this.Values.Name = "Values";
            // 
            // btnGenerateWebhookSite
            // 
            this.btnGenerateWebhookSite.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnGenerateWebhookSite.Location = new System.Drawing.Point(161, 12);
            this.btnGenerateWebhookSite.Name = "btnGenerateWebhookSite";
            this.btnGenerateWebhookSite.Size = new System.Drawing.Size(627, 35);
            this.btnGenerateWebhookSite.TabIndex = 11;
            this.btnGenerateWebhookSite.Text = "Generate Test Url";
            this.toolTip1.SetToolTip(this.btnGenerateWebhookSite, "Generate a new webhook URL on Webhook.site service");
            this.btnGenerateWebhookSite.UseVisualStyleBackColor = true;
            this.btnGenerateWebhookSite.Click += new System.EventHandler(this.btnGenerateWebhookSite_Click);
            // 
            // WebHookRegistrationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 494);
            this.Controls.Add(this.btnGenerateWebhookSite);
            this.Controls.Add(this.dgvKeyValue);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtValue);
            this.Controls.Add(this.lblValue);
            this.Controls.Add(this.cmbAuth);
            this.Controls.Add(this.lblAuthHint);
            this.Controls.Add(this.lblAuth);
            this.Controls.Add(this.txtEndpointUrl);
            this.Controls.Add(this.lblEndpointUrl);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WebHookRegistrationForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "WebHook Registration";
            ((System.ComponentModel.ISupportInitialize)(this.dgvKeyValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtEndpointUrl;
        private System.Windows.Forms.Label lblEndpointUrl;
        private System.Windows.Forms.Label lblAuth;
        private System.Windows.Forms.Label lblAuthHint;
        private System.Windows.Forms.ComboBox cmbAuth;
        private System.Windows.Forms.Label lblValue;
        private System.Windows.Forms.TextBox txtValue;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.DataGridView dgvKeyValue;
        private System.Windows.Forms.DataGridViewTextBoxColumn Keys;
        private System.Windows.Forms.DataGridViewTextBoxColumn Values;
        private System.Windows.Forms.Button btnGenerateWebhookSite;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
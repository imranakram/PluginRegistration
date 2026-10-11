namespace Xrm.Sdk.PluginRegistration.Forms
{
    partial class StepRegistrationForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StepRegistrationForm));
            this.grpGeneral = new System.Windows.Forms.GroupBox();
            this.cmbServiceEndpoint = new System.Windows.Forms.ComboBox();
            this.lblFilteringAttributes = new System.Windows.Forms.Label();
            this.lblEventHandler = new System.Windows.Forms.Label();
            this.txtRank = new System.Windows.Forms.TextBox();
            this.lblRank = new System.Windows.Forms.Label();
            this.cmbUsers = new System.Windows.Forms.ComboBox();
            this.lblImpersonation = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.txtSecondaryEntity = new System.Windows.Forms.TextBox();
            this.lblSecondaryEntity = new System.Windows.Forms.Label();
            this.txtPrimaryEntity = new System.Windows.Forms.TextBox();
            this.lblPrimaryEntity = new System.Windows.Forms.Label();
            this.txtMessageName = new System.Windows.Forms.TextBox();
            this.lblMessageName = new System.Windows.Forms.Label();
            this.cmbWebhook = new System.Windows.Forms.ComboBox();
            this.cmbPlugins = new System.Windows.Forms.ComboBox();
            this.grpSecureConfiguration = new System.Windows.Forms.GroupBox();
            this.lnkInvalidSecureConfigurationId = new System.Windows.Forms.LinkLabel();
            this.lblInvalidSecureConfigurationId = new System.Windows.Forms.Label();
            this.picInvalidSecureConfigurationId = new System.Windows.Forms.PictureBox();
            this.lblAccessDenied = new System.Windows.Forms.Label();
            this.picAccessDenied = new System.Windows.Forms.PictureBox();
            this.txtSecureConfig = new System.Windows.Forms.RichTextBox();
            this.grpMode = new System.Windows.Forms.GroupBox();
            this.radModeSync = new System.Windows.Forms.RadioButton();
            this.radModeAsync = new System.Windows.Forms.RadioButton();
            this.grpDeployment = new System.Windows.Forms.GroupBox();
            this.chkDeploymentOffline = new System.Windows.Forms.CheckBox();
            this.chkDeploymentServer = new System.Windows.Forms.CheckBox();
            this.grpInvocation = new System.Windows.Forms.GroupBox();
            this.radInvocationChild = new System.Windows.Forms.RadioButton();
            this.radInvocationParent = new System.Windows.Forms.RadioButton();
            this.btnRegister = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.tipMain = new System.Windows.Forms.ToolTip(this.components);
            this.radStagePreValidation = new System.Windows.Forms.RadioButton();
            this.radStagePostOperationDeprecated = new System.Windows.Forms.RadioButton();
            this.grpUnsecureConfig = new System.Windows.Forms.GroupBox();
            this.txtUnsecureConfiguration = new System.Windows.Forms.RichTextBox();
            this.radStagePreOperation = new System.Windows.Forms.RadioButton();
            this.radStagePostOperation = new System.Windows.Forms.RadioButton();
            this.grpStage = new System.Windows.Forms.GroupBox();
            this.chkDeleteAsyncOperationIfSuccessful = new System.Windows.Forms.CheckBox();
            this.grpDescription = new System.Windows.Forms.GroupBox();
            this.txtDescription = new System.Windows.Forms.RichTextBox();
            this.tlpRightColumn = new System.Windows.Forms.TableLayoutPanel();
            this.grpGeneral.SuspendLayout();
            this.grpSecureConfiguration.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picInvalidSecureConfigurationId)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAccessDenied)).BeginInit();
            this.grpMode.SuspendLayout();
            this.grpDeployment.SuspendLayout();
            this.grpInvocation.SuspendLayout();
            this.grpUnsecureConfig.SuspendLayout();
            this.grpStage.SuspendLayout();
            this.grpDescription.SuspendLayout();
            this.tlpRightColumn.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpGeneral
            // 
            this.grpGeneral.Controls.Add(this.cmbServiceEndpoint);
            this.grpGeneral.Controls.Add(this.lblFilteringAttributes);
            this.grpGeneral.Controls.Add(this.lblEventHandler);
            this.grpGeneral.Controls.Add(this.txtRank);
            this.grpGeneral.Controls.Add(this.lblRank);
            this.grpGeneral.Controls.Add(this.cmbUsers);
            this.grpGeneral.Controls.Add(this.lblImpersonation);
            this.grpGeneral.Controls.Add(this.txtName);
            this.grpGeneral.Controls.Add(this.lblDescription);
            this.grpGeneral.Controls.Add(this.txtSecondaryEntity);
            this.grpGeneral.Controls.Add(this.lblSecondaryEntity);
            this.grpGeneral.Controls.Add(this.txtPrimaryEntity);
            this.grpGeneral.Controls.Add(this.lblPrimaryEntity);
            this.grpGeneral.Controls.Add(this.txtMessageName);
            this.grpGeneral.Controls.Add(this.lblMessageName);
            this.grpGeneral.Controls.Add(this.cmbWebhook);
            this.grpGeneral.Controls.Add(this.cmbPlugins);
            this.grpGeneral.Location = new System.Drawing.Point(12, 12);
            this.grpGeneral.Name = "grpGeneral";
            this.grpGeneral.Size = new System.Drawing.Size(451, 229);
            this.grpGeneral.TabIndex = 0;
            this.grpGeneral.TabStop = false;
            this.grpGeneral.Text = "General Configuration Information";
            // 
            // cmbServiceEndpoint
            // 
            this.cmbServiceEndpoint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbServiceEndpoint.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbServiceEndpoint.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbServiceEndpoint.DisplayMember = "NodeText";
            this.cmbServiceEndpoint.FormattingEnabled = true;
            this.cmbServiceEndpoint.Location = new System.Drawing.Point(127, 117);
            this.cmbServiceEndpoint.Name = "cmbServiceEndpoint";
            this.cmbServiceEndpoint.Size = new System.Drawing.Size(109, 21);
            this.cmbServiceEndpoint.TabIndex = 12;
            // 
            // lblFilteringAttributes
            // 
            this.lblFilteringAttributes.AutoSize = true;
            this.lblFilteringAttributes.Location = new System.Drawing.Point(9, 94);
            this.lblFilteringAttributes.Name = "lblFilteringAttributes";
            this.lblFilteringAttributes.Size = new System.Drawing.Size(93, 13);
            this.lblFilteringAttributes.TabIndex = 8;
            this.lblFilteringAttributes.Text = "Filtering Attributes:";
            // 
            // lblEventHandler
            // 
            this.lblEventHandler.AutoSize = true;
            this.lblEventHandler.Location = new System.Drawing.Point(9, 120);
            this.lblEventHandler.Name = "lblEventHandler";
            this.lblEventHandler.Size = new System.Drawing.Size(78, 13);
            this.lblEventHandler.TabIndex = 11;
            this.lblEventHandler.Text = "Event Handler:";
            // 
            // txtRank
            // 
            this.txtRank.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRank.Location = new System.Drawing.Point(127, 198);
            this.txtRank.Name = "txtRank";
            this.txtRank.Size = new System.Drawing.Size(316, 20);
            this.txtRank.TabIndex = 18;
            this.txtRank.Text = "1";
            this.txtRank.Enter += new System.EventHandler(this.txtRank_Enter);
            this.txtRank.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtRank_KeyPress);
            this.txtRank.Leave += new System.EventHandler(this.txtRank_Leave);
            // 
            // lblRank
            // 
            this.lblRank.AutoSize = true;
            this.lblRank.Location = new System.Drawing.Point(9, 200);
            this.lblRank.Name = "lblRank";
            this.lblRank.Size = new System.Drawing.Size(86, 13);
            this.lblRank.TabIndex = 17;
            this.lblRank.Text = "Execution Order:";
            // 
            // cmbUsers
            // 
            this.cmbUsers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbUsers.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbUsers.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbUsers.Location = new System.Drawing.Point(127, 170);
            this.cmbUsers.Name = "cmbUsers";
            this.cmbUsers.Size = new System.Drawing.Size(316, 21);
            this.cmbUsers.TabIndex = 16;
            this.cmbUsers.Enter += new System.EventHandler(this.cmbUsers_Enter);
            this.cmbUsers.Leave += new System.EventHandler(this.cmbUsers_Leave);
            // 
            // lblImpersonation
            // 
            this.lblImpersonation.AutoSize = true;
            this.lblImpersonation.Location = new System.Drawing.Point(9, 173);
            this.lblImpersonation.Name = "lblImpersonation";
            this.lblImpersonation.Size = new System.Drawing.Size(112, 13);
            this.lblImpersonation.TabIndex = 15;
            this.lblImpersonation.Text = "Run in User\'s Context:";
            // 
            // txtName
            // 
            this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtName.Location = new System.Drawing.Point(127, 144);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(316, 20);
            this.txtName.TabIndex = 14;
            this.txtName.TextChanged += new System.EventHandler(this.txtName_TextChanged);
            this.txtName.Enter += new System.EventHandler(this.txtName_Enter);
            this.txtName.Leave += new System.EventHandler(this.txtName_Leave);
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new System.Drawing.Point(9, 147);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(38, 13);
            this.lblDescription.TabIndex = 13;
            this.lblDescription.Text = "Name:";
            // 
            // txtSecondaryEntity
            // 
            this.txtSecondaryEntity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSecondaryEntity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtSecondaryEntity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtSecondaryEntity.Location = new System.Drawing.Point(127, 65);
            this.txtSecondaryEntity.Name = "txtSecondaryEntity";
            this.txtSecondaryEntity.Size = new System.Drawing.Size(316, 20);
            this.txtSecondaryEntity.TabIndex = 7;
            this.txtSecondaryEntity.TextChanged += new System.EventHandler(this.MessageData_TextChanged);
            this.txtSecondaryEntity.Enter += new System.EventHandler(this.txtSecondaryEntity_Enter);
            this.txtSecondaryEntity.Leave += new System.EventHandler(this.MessageEntityData_Leave);
            // 
            // lblSecondaryEntity
            // 
            this.lblSecondaryEntity.AutoSize = true;
            this.lblSecondaryEntity.Location = new System.Drawing.Point(9, 68);
            this.lblSecondaryEntity.Name = "lblSecondaryEntity";
            this.lblSecondaryEntity.Size = new System.Drawing.Size(90, 13);
            this.lblSecondaryEntity.TabIndex = 6;
            this.lblSecondaryEntity.Text = "Secondary Entity:";
            // 
            // txtPrimaryEntity
            // 
            this.txtPrimaryEntity.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPrimaryEntity.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtPrimaryEntity.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtPrimaryEntity.Location = new System.Drawing.Point(127, 39);
            this.txtPrimaryEntity.Name = "txtPrimaryEntity";
            this.txtPrimaryEntity.Size = new System.Drawing.Size(316, 20);
            this.txtPrimaryEntity.TabIndex = 5;
            this.txtPrimaryEntity.TextChanged += new System.EventHandler(this.MessageData_TextChanged);
            this.txtPrimaryEntity.Enter += new System.EventHandler(this.txtPrimaryEntity_Enter);
            this.txtPrimaryEntity.Leave += new System.EventHandler(this.MessageEntityData_Leave);
            // 
            // lblPrimaryEntity
            // 
            this.lblPrimaryEntity.AutoSize = true;
            this.lblPrimaryEntity.Location = new System.Drawing.Point(9, 42);
            this.lblPrimaryEntity.Name = "lblPrimaryEntity";
            this.lblPrimaryEntity.Size = new System.Drawing.Size(73, 13);
            this.lblPrimaryEntity.TabIndex = 4;
            this.lblPrimaryEntity.Text = "&Primary Entity:";
            // 
            // txtMessageName
            // 
            this.txtMessageName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMessageName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.txtMessageName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtMessageName.Location = new System.Drawing.Point(127, 13);
            this.txtMessageName.Name = "txtMessageName";
            this.txtMessageName.Size = new System.Drawing.Size(316, 20);
            this.txtMessageName.TabIndex = 3;
            this.txtMessageName.TextChanged += new System.EventHandler(this.MessageData_TextChanged);
            this.txtMessageName.Enter += new System.EventHandler(this.txtMessageName_Enter);
            this.txtMessageName.Leave += new System.EventHandler(this.txtMessageName_Leave);
            this.txtMessageName.Validating += new System.ComponentModel.CancelEventHandler(this.txtMessageName_Validating);
            // 
            // lblMessageName
            // 
            this.lblMessageName.AutoSize = true;
            this.lblMessageName.Location = new System.Drawing.Point(9, 16);
            this.lblMessageName.Name = "lblMessageName";
            this.lblMessageName.Size = new System.Drawing.Size(53, 13);
            this.lblMessageName.TabIndex = 1;
            this.lblMessageName.Text = "Message:";
            // 
            // cmbWebhook
            // 
            this.cmbWebhook.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbWebhook.FormattingEnabled = true;
            this.cmbWebhook.Location = new System.Drawing.Point(127, 117);
            this.cmbWebhook.Margin = new System.Windows.Forms.Padding(2);
            this.cmbWebhook.Name = "cmbWebhook";
            this.cmbWebhook.Size = new System.Drawing.Size(242, 21);
            this.cmbWebhook.TabIndex = 19;
            // 
            // cmbPlugins
            // 
            this.cmbPlugins.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbPlugins.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cmbPlugins.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cmbPlugins.FormattingEnabled = true;
            this.cmbPlugins.Location = new System.Drawing.Point(127, 117);
            this.cmbPlugins.Name = "cmbPlugins";
            this.cmbPlugins.Size = new System.Drawing.Size(316, 21);
            this.cmbPlugins.Sorted = true;
            this.cmbPlugins.TabIndex = 12;
            this.cmbPlugins.SelectedIndexChanged += new System.EventHandler(this.cmbPlugins_SelectedIndexChanged);
            // 
            // grpSecureConfiguration
            // 
            this.grpSecureConfiguration.Controls.Add(this.lnkInvalidSecureConfigurationId);
            this.grpSecureConfiguration.Controls.Add(this.lblInvalidSecureConfigurationId);
            this.grpSecureConfiguration.Controls.Add(this.picInvalidSecureConfigurationId);
            this.grpSecureConfiguration.Controls.Add(this.lblAccessDenied);
            this.grpSecureConfiguration.Controls.Add(this.picAccessDenied);
            this.grpSecureConfiguration.Controls.Add(this.txtSecureConfig);
            this.grpSecureConfiguration.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpSecureConfiguration.Location = new System.Drawing.Point(3, 225);
            this.grpSecureConfiguration.Name = "grpSecureConfiguration";
            this.grpSecureConfiguration.Size = new System.Drawing.Size(427, 168);
            this.grpSecureConfiguration.TabIndex = 9;
            this.grpSecureConfiguration.TabStop = false;
            this.grpSecureConfiguration.Text = "Secure Configuration";
            this.tipMain.SetToolTip(this.grpSecureConfiguration, "Passed as the second parameter to the Plugin\'s Constructor when it has 2 string i" +
        "nput parameters. This configuration value is NOT downloaded to the user\'s comput" +
        "er when they go offline.");
            // 
            // lnkInvalidSecureConfigurationId
            // 
            this.lnkInvalidSecureConfigurationId.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lnkInvalidSecureConfigurationId.AutoSize = true;
            this.lnkInvalidSecureConfigurationId.Location = new System.Drawing.Point(73, 151);
            this.lnkInvalidSecureConfigurationId.Name = "lnkInvalidSecureConfigurationId";
            this.lnkInvalidSecureConfigurationId.Size = new System.Drawing.Size(137, 13);
            this.lnkInvalidSecureConfigurationId.TabIndex = 10;
            this.lnkInvalidSecureConfigurationId.TabStop = true;
            this.lnkInvalidSecureConfigurationId.Text = "Reset Secure Configuration";
            this.lnkInvalidSecureConfigurationId.Visible = false;
            this.lnkInvalidSecureConfigurationId.Click += new System.EventHandler(this.lnkInvalidSecureConfigurationId_Click);
            // 
            // lblInvalidSecureConfigurationId
            // 
            this.lblInvalidSecureConfigurationId.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblInvalidSecureConfigurationId.Location = new System.Drawing.Point(73, 101);
            this.lblInvalidSecureConfigurationId.Name = "lblInvalidSecureConfigurationId";
            this.lblInvalidSecureConfigurationId.Size = new System.Drawing.Size(307, 48);
            this.lblInvalidSecureConfigurationId.TabIndex = 9;
            this.lblInvalidSecureConfigurationId.Text = "The ID specified in the Secure Configuration appears to be invalid, which usually" +
    " indicates that the step or secure configuration has been edited with the SDK.";
            this.lblInvalidSecureConfigurationId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblInvalidSecureConfigurationId.Visible = false;
            // 
            // picInvalidSecureConfigurationId
            // 
            this.picInvalidSecureConfigurationId.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.picInvalidSecureConfigurationId.Location = new System.Drawing.Point(25, 101);
            this.picInvalidSecureConfigurationId.Name = "picInvalidSecureConfigurationId";
            this.picInvalidSecureConfigurationId.Size = new System.Drawing.Size(48, 48);
            this.picInvalidSecureConfigurationId.TabIndex = 8;
            this.picInvalidSecureConfigurationId.TabStop = false;
            this.picInvalidSecureConfigurationId.Visible = false;
            // 
            // lblAccessDenied
            // 
            this.lblAccessDenied.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblAccessDenied.Location = new System.Drawing.Point(73, 24);
            this.lblAccessDenied.Name = "lblAccessDenied";
            this.lblAccessDenied.Size = new System.Drawing.Size(307, 48);
            this.lblAccessDenied.TabIndex = 6;
            this.lblAccessDenied.Text = "Unable to retrieve Secure Configuration. This may be because you do not have acce" +
    "ss to Secure Configuration values. No changes will be saved for this field.";
            this.lblAccessDenied.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblAccessDenied.Visible = false;
            // 
            // picAccessDenied
            // 
            this.picAccessDenied.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.picAccessDenied.Location = new System.Drawing.Point(25, 24);
            this.picAccessDenied.Name = "picAccessDenied";
            this.picAccessDenied.Size = new System.Drawing.Size(48, 48);
            this.picAccessDenied.TabIndex = 7;
            this.picAccessDenied.TabStop = false;
            this.picAccessDenied.Visible = false;
            // 
            // txtSecureConfig
            // 
            this.txtSecureConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSecureConfig.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSecureConfig.Location = new System.Drawing.Point(6, 19);
            this.txtSecureConfig.MaxLength = 1073741823;
            this.txtSecureConfig.Name = "txtSecureConfig";
            this.txtSecureConfig.Size = new System.Drawing.Size(419, 142);
            this.txtSecureConfig.TabIndex = 1;
            this.txtSecureConfig.Text = "";
            this.txtSecureConfig.WordWrap = false;
            this.txtSecureConfig.Enter += new System.EventHandler(this.longText_Enter);
            this.txtSecureConfig.Leave += new System.EventHandler(this.longText_Leave);
            // 
            // grpMode
            // 
            this.grpMode.Controls.Add(this.radModeSync);
            this.grpMode.Controls.Add(this.radModeAsync);
            this.grpMode.Location = new System.Drawing.Point(218, 247);
            this.grpMode.Name = "grpMode";
            this.grpMode.Size = new System.Drawing.Size(102, 65);
            this.grpMode.TabIndex = 3;
            this.grpMode.TabStop = false;
            this.grpMode.Text = "Execution Mode";
            // 
            // radModeSync
            // 
            this.radModeSync.AutoSize = true;
            this.radModeSync.Checked = true;
            this.radModeSync.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radModeSync.Location = new System.Drawing.Point(6, 42);
            this.radModeSync.Name = "radModeSync";
            this.radModeSync.Size = new System.Drawing.Size(90, 17);
            this.radModeSync.TabIndex = 2;
            this.radModeSync.TabStop = true;
            this.radModeSync.Text = "Synchronous ";
            this.tipMain.SetToolTip(this.radModeSync, "The triggering operation waits for the Plugin to complete before it continues.");
            this.radModeSync.UseVisualStyleBackColor = true;
            this.radModeSync.CheckedChanged += new System.EventHandler(this.radMode_CheckedChanged);
            // 
            // radModeAsync
            // 
            this.radModeAsync.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radModeAsync.Location = new System.Drawing.Point(6, 19);
            this.radModeAsync.Name = "radModeAsync";
            this.radModeAsync.Size = new System.Drawing.Size(94, 17);
            this.radModeAsync.TabIndex = 0;
            this.radModeAsync.Text = "Asynchronous ";
            this.tipMain.SetToolTip(this.radModeAsync, "The triggering operation does not wait for the Plugin to complete to continue.");
            this.radModeAsync.UseVisualStyleBackColor = true;
            this.radModeAsync.CheckedChanged += new System.EventHandler(this.radMode_CheckedChanged);
            // 
            // grpDeployment
            // 
            this.grpDeployment.Controls.Add(this.chkDeploymentOffline);
            this.grpDeployment.Controls.Add(this.chkDeploymentServer);
            this.grpDeployment.Location = new System.Drawing.Point(326, 247);
            this.grpDeployment.Name = "grpDeployment";
            this.grpDeployment.Size = new System.Drawing.Size(88, 65);
            this.grpDeployment.TabIndex = 4;
            this.grpDeployment.TabStop = false;
            this.grpDeployment.Text = "Deployment";
            // 
            // chkDeploymentOffline
            // 
            this.chkDeploymentOffline.AutoSize = true;
            this.chkDeploymentOffline.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDeploymentOffline.Location = new System.Drawing.Point(6, 42);
            this.chkDeploymentOffline.Name = "chkDeploymentOffline";
            this.chkDeploymentOffline.Size = new System.Drawing.Size(56, 17);
            this.chkDeploymentOffline.TabIndex = 3;
            this.chkDeploymentOffline.Text = "Offline";
            this.tipMain.SetToolTip(this.chkDeploymentOffline, "The Plugin will be downloaded and executed when the Outlook Client is Offline.");
            this.chkDeploymentOffline.UseVisualStyleBackColor = true;
            // 
            // chkDeploymentServer
            // 
            this.chkDeploymentServer.AutoSize = true;
            this.chkDeploymentServer.Checked = true;
            this.chkDeploymentServer.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDeploymentServer.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDeploymentServer.Location = new System.Drawing.Point(6, 19);
            this.chkDeploymentServer.Name = "chkDeploymentServer";
            this.chkDeploymentServer.Size = new System.Drawing.Size(57, 17);
            this.chkDeploymentServer.TabIndex = 0;
            this.chkDeploymentServer.Text = "Server";
            this.tipMain.SetToolTip(this.chkDeploymentServer, "The Plugin will trigger when the Outlook Client is Online.");
            this.chkDeploymentServer.UseVisualStyleBackColor = true;
            // 
            // grpInvocation
            // 
            this.grpInvocation.Controls.Add(this.radInvocationChild);
            this.grpInvocation.Controls.Add(this.radInvocationParent);
            this.grpInvocation.Location = new System.Drawing.Point(218, 316);
            this.grpInvocation.Name = "grpInvocation";
            this.grpInvocation.Size = new System.Drawing.Size(196, 65);
            this.grpInvocation.TabIndex = 5;
            this.grpInvocation.TabStop = false;
            this.grpInvocation.Text = "Triggering Pipeline (CRM4 Only)";
            this.grpInvocation.Visible = false;
            // 
            // radInvocationChild
            // 
            this.radInvocationChild.AutoSize = true;
            this.radInvocationChild.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radInvocationChild.Location = new System.Drawing.Point(6, 42);
            this.radInvocationChild.Name = "radInvocationChild";
            this.radInvocationChild.Size = new System.Drawing.Size(88, 17);
            this.radInvocationChild.TabIndex = 4;
            this.radInvocationChild.Text = "Child Pipeline";
            this.tipMain.SetToolTip(this.radInvocationChild, "Example: Assigning an Account will Trigger on Update message for ownerid.");
            this.radInvocationChild.UseVisualStyleBackColor = true;
            this.radInvocationChild.CheckedChanged += new System.EventHandler(this.radInvocationParent_CheckedChanged);
            // 
            // radInvocationParent
            // 
            this.radInvocationParent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radInvocationParent.AutoSize = true;
            this.radInvocationParent.Checked = true;
            this.radInvocationParent.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radInvocationParent.Location = new System.Drawing.Point(6, 19);
            this.radInvocationParent.Name = "radInvocationParent";
            this.radInvocationParent.Size = new System.Drawing.Size(96, 17);
            this.radInvocationParent.TabIndex = 0;
            this.radInvocationParent.TabStop = true;
            this.radInvocationParent.Text = "Parent Pipeline";
            this.tipMain.SetToolTip(this.radInvocationParent, "Example: Assigning an Account will Trigger on Assign message");
            this.radInvocationParent.UseVisualStyleBackColor = true;
            // 
            // btnRegister
            // 
            this.btnRegister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRegister.Location = new System.Drawing.Point(708, 414);
            this.btnRegister.Name = "btnRegister";
            this.btnRegister.Size = new System.Drawing.Size(113, 23);
            this.btnRegister.TabIndex = 1;
            this.btnRegister.Text = "&Register New Step";
            this.btnRegister.UseVisualStyleBackColor = true;
            this.btnRegister.Click += new System.EventHandler(this.btnRegister_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(827, 414);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // radStagePreValidation
            // 
            this.radStagePreValidation.AutoSize = true;
            this.radStagePreValidation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radStagePreValidation.Location = new System.Drawing.Point(6, 19);
            this.radStagePreValidation.Name = "radStagePreValidation";
            this.radStagePreValidation.Size = new System.Drawing.Size(89, 17);
            this.radStagePreValidation.TabIndex = 0;
            this.radStagePreValidation.Text = "Pre-validation";
            this.tipMain.SetToolTip(this.radStagePreValidation, "Executes before the triggering operation is executed.");
            this.radStagePreValidation.UseVisualStyleBackColor = true;
            // 
            // radStagePostOperationDeprecated
            // 
            this.radStagePostOperationDeprecated.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.radStagePostOperationDeprecated.AutoSize = true;
            this.radStagePostOperationDeprecated.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radStagePostOperationDeprecated.Location = new System.Drawing.Point(18, 358);
            this.radStagePostOperationDeprecated.Name = "radStagePostOperationDeprecated";
            this.radStagePostOperationDeprecated.Size = new System.Drawing.Size(156, 17);
            this.radStagePostOperationDeprecated.TabIndex = 3;
            this.radStagePostOperationDeprecated.Text = "Post-operation (CRM4 Only)";
            this.tipMain.SetToolTip(this.radStagePostOperationDeprecated, "Executes after the triggering operation has completed.");
            this.radStagePostOperationDeprecated.UseVisualStyleBackColor = true;
            this.radStagePostOperationDeprecated.Visible = false;
            // 
            // grpUnsecureConfig
            // 
            this.grpUnsecureConfig.Controls.Add(this.txtUnsecureConfiguration);
            this.grpUnsecureConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpUnsecureConfig.Location = new System.Drawing.Point(3, 92);
            this.grpUnsecureConfig.Name = "grpUnsecureConfig";
            this.grpUnsecureConfig.Size = new System.Drawing.Size(427, 127);
            this.grpUnsecureConfig.TabIndex = 8;
            this.grpUnsecureConfig.TabStop = false;
            this.grpUnsecureConfig.Text = "Unsecure Configuration";
            this.tipMain.SetToolTip(this.grpUnsecureConfig, resources.GetString("grpUnsecureConfig.ToolTip"));
            // 
            // txtUnsecureConfiguration
            // 
            this.txtUnsecureConfiguration.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtUnsecureConfiguration.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUnsecureConfiguration.Location = new System.Drawing.Point(6, 19);
            this.txtUnsecureConfiguration.MaxLength = 1073741823;
            this.txtUnsecureConfiguration.Name = "txtUnsecureConfiguration";
            this.txtUnsecureConfiguration.Size = new System.Drawing.Size(419, 101);
            this.txtUnsecureConfiguration.TabIndex = 1;
            this.txtUnsecureConfiguration.Text = "";
            this.txtUnsecureConfiguration.WordWrap = false;
            this.txtUnsecureConfiguration.Enter += new System.EventHandler(this.longText_Enter);
            this.txtUnsecureConfiguration.Leave += new System.EventHandler(this.longText_Leave);
            // 
            // radStagePreOperation
            // 
            this.radStagePreOperation.AutoSize = true;
            this.radStagePreOperation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radStagePreOperation.Location = new System.Drawing.Point(6, 42);
            this.radStagePreOperation.Name = "radStagePreOperation";
            this.radStagePreOperation.Size = new System.Drawing.Size(88, 17);
            this.radStagePreOperation.TabIndex = 1;
            this.radStagePreOperation.Text = "Pre-operation";
            this.tipMain.SetToolTip(this.radStagePreOperation, "Executes before the triggering operation is executed.");
            this.radStagePreOperation.UseVisualStyleBackColor = true;
            // 
            // radStagePostOperation
            // 
            this.radStagePostOperation.AutoSize = true;
            this.radStagePostOperation.Checked = true;
            this.radStagePostOperation.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radStagePostOperation.Location = new System.Drawing.Point(6, 65);
            this.radStagePostOperation.Name = "radStagePostOperation";
            this.radStagePostOperation.Size = new System.Drawing.Size(93, 17);
            this.radStagePostOperation.TabIndex = 2;
            this.radStagePostOperation.TabStop = true;
            this.radStagePostOperation.Text = "Post-operation";
            this.tipMain.SetToolTip(this.radStagePostOperation, "Executes before the triggering operation is executed.");
            this.radStagePostOperation.UseVisualStyleBackColor = true;
            // 
            // grpStage
            // 
            this.grpStage.Controls.Add(this.radStagePostOperation);
            this.grpStage.Controls.Add(this.radStagePreOperation);
            this.grpStage.Controls.Add(this.radStagePreValidation);
            this.grpStage.Location = new System.Drawing.Point(12, 247);
            this.grpStage.Name = "grpStage";
            this.grpStage.Size = new System.Drawing.Size(200, 92);
            this.grpStage.TabIndex = 2;
            this.grpStage.TabStop = false;
            this.grpStage.Text = "Eventing Pipeline Stage of Execution";
            // 
            // chkDeleteAsyncOperationIfSuccessful
            // 
            this.chkDeleteAsyncOperationIfSuccessful.Enabled = false;
            this.chkDeleteAsyncOperationIfSuccessful.Location = new System.Drawing.Point(12, 392);
            this.chkDeleteAsyncOperationIfSuccessful.Name = "chkDeleteAsyncOperationIfSuccessful";
            this.chkDeleteAsyncOperationIfSuccessful.Size = new System.Drawing.Size(402, 16);
            this.chkDeleteAsyncOperationIfSuccessful.TabIndex = 5;
            this.chkDeleteAsyncOperationIfSuccessful.Text = "Delete AsyncOperation if StatusCode = Successful";
            this.chkDeleteAsyncOperationIfSuccessful.UseVisualStyleBackColor = true;
            // 
            // grpDescription
            // 
            this.grpDescription.Controls.Add(this.txtDescription);
            this.grpDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpDescription.Location = new System.Drawing.Point(3, 3);
            this.grpDescription.Name = "grpDescription";
            this.grpDescription.Size = new System.Drawing.Size(427, 83);
            this.grpDescription.TabIndex = 6;
            this.grpDescription.TabStop = false;
            this.grpDescription.Text = "Description";
            // 
            // txtDescription
            // 
            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDescription.Location = new System.Drawing.Point(4, 15);
            this.txtDescription.MaxLength = 1073741823;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(423, 60);
            this.txtDescription.TabIndex = 1;
            this.txtDescription.Text = "";
            this.txtDescription.Enter += new System.EventHandler(this.longText_Enter);
            this.txtDescription.Leave += new System.EventHandler(this.longText_Leave);
            //
            // tlpRightColumn
            //
            this.tlpRightColumn.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpRightColumn.ColumnCount = 1;
            this.tlpRightColumn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRightColumn.Controls.Add(this.grpDescription, 0, 0);
            this.tlpRightColumn.Controls.Add(this.grpUnsecureConfig, 0, 1);
            this.tlpRightColumn.Controls.Add(this.grpSecureConfiguration, 0, 2);
            this.tlpRightColumn.Location = new System.Drawing.Point(466, 10);
            this.tlpRightColumn.Name = "tlpRightColumn";
            this.tlpRightColumn.RowCount = 3;
            this.tlpRightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 89F));
            this.tlpRightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 43F));
            this.tlpRightColumn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 57F));
            this.tlpRightColumn.Size = new System.Drawing.Size(439, 398);
            this.tlpRightColumn.TabIndex = 6;
            //
            // StepRegistrationForm
            //
            this.AcceptButton = this.btnRegister;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(907, 445);
            this.Controls.Add(this.tlpRightColumn);
            this.Controls.Add(this.chkDeleteAsyncOperationIfSuccessful);
            this.Controls.Add(this.radStagePostOperationDeprecated);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.grpDeployment);
            this.Controls.Add(this.btnRegister);
            this.Controls.Add(this.grpInvocation);
            this.Controls.Add(this.grpStage);
            this.Controls.Add(this.grpMode);
            this.Controls.Add(this.grpGeneral);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(921, 478);
            this.Name = "StepRegistrationForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Register New Step";
            this.Load += new System.EventHandler(this.StepRegistrationForm_Load);
            this.grpGeneral.ResumeLayout(false);
            this.grpGeneral.PerformLayout();
            this.grpSecureConfiguration.ResumeLayout(false);
            this.grpSecureConfiguration.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picInvalidSecureConfigurationId)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAccessDenied)).EndInit();
            this.grpMode.ResumeLayout(false);
            this.grpMode.PerformLayout();
            this.grpDeployment.ResumeLayout(false);
            this.grpDeployment.PerformLayout();
            this.grpInvocation.ResumeLayout(false);
            this.grpInvocation.PerformLayout();
            this.grpUnsecureConfig.ResumeLayout(false);
            this.grpStage.ResumeLayout(false);
            this.grpStage.PerformLayout();
            this.grpDescription.ResumeLayout(false);
            this.tlpRightColumn.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox grpGeneral;
        private System.Windows.Forms.TextBox txtMessageName;
        private System.Windows.Forms.Label lblMessageName;
        private System.Windows.Forms.TextBox txtSecondaryEntity;
        private System.Windows.Forms.Label lblSecondaryEntity;
        private System.Windows.Forms.TextBox txtPrimaryEntity;
        private System.Windows.Forms.Label lblPrimaryEntity;
        private System.Windows.Forms.ComboBox cmbPlugins;
        private System.Windows.Forms.Label lblImpersonation;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.ComboBox cmbUsers;
        private System.Windows.Forms.GroupBox grpSecureConfiguration;
        private System.Windows.Forms.RichTextBox txtSecureConfig;
        private System.Windows.Forms.GroupBox grpMode;
        private System.Windows.Forms.RadioButton radModeAsync;
        private System.Windows.Forms.RadioButton radModeSync;
        private System.Windows.Forms.GroupBox grpDeployment;
        private System.Windows.Forms.CheckBox chkDeploymentOffline;
        private System.Windows.Forms.CheckBox chkDeploymentServer;
        private System.Windows.Forms.GroupBox grpInvocation;
        private System.Windows.Forms.RadioButton radInvocationChild;
        private System.Windows.Forms.RadioButton radInvocationParent;
        private System.Windows.Forms.TextBox txtRank;
        private System.Windows.Forms.Label lblRank;
        private System.Windows.Forms.Button btnRegister;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ToolTip tipMain;
        private System.Windows.Forms.RadioButton radStagePreValidation;
        private System.Windows.Forms.RadioButton radStagePostOperationDeprecated;
        private System.Windows.Forms.GroupBox grpStage;
        private System.Windows.Forms.GroupBox grpUnsecureConfig;
        private System.Windows.Forms.RichTextBox txtUnsecureConfiguration;
        private System.Windows.Forms.Label lblAccessDenied;
        private System.Windows.Forms.PictureBox picAccessDenied;
        private Xrm.Sdk.PluginRegistration.Controls.CrmAttributeSelectionControl crmFilteringAttributes;
        private System.Windows.Forms.Label lblFilteringAttributes;
        private System.Windows.Forms.RadioButton radStagePostOperation;
        private System.Windows.Forms.RadioButton radStagePreOperation;
        private System.Windows.Forms.CheckBox chkDeleteAsyncOperationIfSuccessful;
        private System.Windows.Forms.GroupBox grpDescription;
        private System.Windows.Forms.RichTextBox txtDescription;
        private System.Windows.Forms.Label lblEventHandler;
        private System.Windows.Forms.LinkLabel lnkInvalidSecureConfigurationId;
        private System.Windows.Forms.Label lblInvalidSecureConfigurationId;
        private System.Windows.Forms.PictureBox picInvalidSecureConfigurationId;
        private System.Windows.Forms.ComboBox cmbServiceEndpoint;
        private System.Windows.Forms.ComboBox cmbWebhook;
        private System.Windows.Forms.TableLayoutPanel tlpRightColumn;
    }
}
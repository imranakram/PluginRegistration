// =====================================================================
//
//  This file is part of the Microsoft Dynamics CRM SDK code samples.
//
//  Copyright (C) Microsoft Corporation.  All rights reserved.
//
//  This source code is intended only as a supplement to Microsoft
//  Development Tools and/or on-line documentation.  See these other
//  materials for detailed information regarding Microsoft code samples.
//
//  THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
//  PARTICULAR PURPOSE.
//
// =====================================================================

namespace Xrm.Sdk.PluginRegistration.Forms
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Forms;
    using Wrappers;

    public partial class StepRegistrationForm : Form
    {
        #region Private Fields

        private CrmPluginStep m_currentStep;
        private CrmMessage m_message = null;
        private string m_messageEntityPrimaryRetrieved = null;
        private CrmMessageEntity m_messageEntityRetrieved = null;
        private string m_messageEntitySecondaryRetrieved = null;
        private CrmMessage m_messageLoaded = null;
        private string m_messageRetrieved = null;
        private CrmOrganization m_org;
        private MainControl m_orgControl;
        private bool m_secureConfigurationIdIsInvalid = false;

        private string m_stepName = string.Empty;

        //Baseline geometry captured once, used to grow crmFilteringAttributes into the
        //spare vertical space of the left column when its value does not fit on one line
        private readonly Dictionary<Control, int> m_baseTopBelowFilteringAttributes = new Dictionary<Control, int>();
        private readonly Dictionary<Control, int> m_baseTopBelowGeneral = new Dictionary<Control, int>();
        private int m_appliedFilteringAttributesGrowth = 0;
        private int m_baseFilteringAttributesHeight;
        private int m_baseGeneralHeight;
        private int m_baseLeftColumnHeight;
        private bool m_filteringAttributesLayoutCaptured = false;

        #endregion Private Fields

        #region Public Constructors

        public StepRegistrationForm(CrmOrganization org, MainControl orgControl, CrmPlugin plugin, CrmPluginStep step, CrmServiceEndpoint serviceEndpoint)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (orgControl == null)
            {
                throw new ArgumentNullException("control");
            }
            m_org = org;
            m_orgControl = orgControl;
            m_currentStep = step;

            #region Initialization of crmFilteringAttributes

            //Seems this was removed automatically by VS designer, so added here instead.
            crmFilteringAttributes = new Controls.CrmAttributeSelectionControl()
            {
                Organization = org
            };

            //
            // crmFilteringAttributes
            //
            this.crmFilteringAttributes.Anchor = ((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right);
            this.crmFilteringAttributes.Attributes = null;
            this.crmFilteringAttributes.DisabledMessage = "";
            //this.crmFilteringAttributes.EntityName = null;
            this.crmFilteringAttributes.Location = new System.Drawing.Point(127, 91);
            this.crmFilteringAttributes.Margin = new Padding(4, 5, 4, 5);
            this.crmFilteringAttributes.Name = "crmFilteringAttributes";
            //this.crmFilteringAttributes.Organization = null;
            this.crmFilteringAttributes.ScrollBars = ScrollBars.None;
            this.crmFilteringAttributes.Size = new System.Drawing.Size(316, 20);
            this.crmFilteringAttributes.TabIndex = 9;
            this.crmFilteringAttributes.WordWrap = true;

            #endregion Initialization of crmFilteringAttributes

            InitializeComponent();
            this.grpGeneral.Controls.Add(this.crmFilteringAttributes);

            CaptureFilteringAttributesLayout();
            this.crmFilteringAttributes.AttributesChanged += new EventHandler<EventArgs>(this.crmFilteringAttributes_AttributesChanged);
            this.pnlLeftColumn.SizeChanged += new EventHandler(this.pnlLeftColumn_SizeChanged);

            //Initialize the auto-complete on the Message field
            var msgList = new AutoCompleteStringCollection();
            foreach (CrmMessage msg in org.Messages.Values)
            {
                msgList.Add(msg.Name);
            }
            txtMessageName.AutoCompleteCustomSource = msgList;

            //Check whether system plugins should be added to the list
            if (plugin != null && step != null && org[step.AssemblyId][step.PluginId].IsSystemCrmEntity && (!OrganizationHelper.AllowStepRegistrationForPlugin(plugin)))
            {
                cmbPlugins.Enabled = false;
            }
            else if ((plugin != null) && (!OrganizationHelper.AllowStepRegistrationForPlugin(plugin)))
            {
                plugin = null;
            }

            //Add the plugins
            CrmPlugin selectPlugin = null;
            foreach (CrmPluginAssembly assembly in org.Assemblies.Values)
            {
                foreach (CrmPlugin pluginType in assembly.Plugins.Values)
                {
                    if (pluginType.PluginType == CrmPluginType.Plugin &&
                        OrganizationHelper.AllowStepRegistrationForPlugin(pluginType))
                    {
                        if (serviceEndpoint == null && pluginType.TypeName == CrmServiceEndpoint.ServiceBusPluginName && pluginType.CustomizationLevel == 0)
                        {
                            continue;
                            // Donot add OOB Service Bus plugin to the list when it it is not a Service Bus Step
                        }
                        else
                        {
                            if (plugin != null && plugin.PluginId == pluginType.PluginId)
                            {
                                selectPlugin = pluginType;
                                cmbPlugins.Items.Add(pluginType);
                            }
                            else
                            {
                                cmbPlugins.Items.Add(pluginType);
                            }
                        }
                    }
                }
            }

            if (cmbPlugins.Items.Count != 0)
            {
                if (selectPlugin == null)
                {
                    cmbPlugins.SelectedIndex = 0;
                }
                else
                {
                    cmbPlugins.SelectedItem = selectPlugin;
                }
            }

            //Create a user that represents the current user
            var callingUser = new CrmUser(org)
            {
                UserId = Guid.Empty,
                Name = "Calling User",
                Enabled = true
            };
            cmbUsers.Items.Add(callingUser);

            CrmServiceEndpoint selectServiceEndpoint = null;
            foreach (CrmServiceEndpoint currentServiceEndpoint in org.ServiceEndpoints.Values)
            {
                if (serviceEndpoint != null && serviceEndpoint.ServiceEndpointId == currentServiceEndpoint.ServiceEndpointId)
                {
                    selectServiceEndpoint = currentServiceEndpoint;
                }
                cmbServiceEndpoint.Items.Add(currentServiceEndpoint);
            }

            if (selectServiceEndpoint != null)
            {
                cmbServiceEndpoint.SelectedItem = selectServiceEndpoint;
            }
            else
            {
                if (cmbServiceEndpoint.Items.Count != 0)
                {
                    cmbServiceEndpoint.SelectedIndex = 0;
                }
            }

            // Webhook case
            CrmServiceEndpoint selectedWebhook = null;
            if (plugin == null && serviceEndpoint != null)
            {
                foreach (var webhook in org.Webhooks.Values)
                {
                    if (serviceEndpoint.ServiceEndpointId == webhook.ServiceEndpointId)
                    {
                        selectedWebhook = webhook;
                    }
                    cmbWebhook.Items.Add(webhook);
                }
            }

            if (selectedWebhook != null)
            {
                cmbWebhook.SelectedItem = selectedWebhook;
            }
            else
            {
                if (cmbWebhook.Items.Count != 0)
                {
                    cmbWebhook.SelectedIndex = 0;
                }
            }

            if (serviceEndpoint != null)
            {
                UpdatePluginEventHandlerControls(true, plugin == null);
            }
            else
            {
                UpdatePluginEventHandlerControls(false, false);
            }

            if (m_currentStep != null)
            {
                txtMessageName.Text = m_org.Messages[m_currentStep.MessageId].Name;

                if (org.MessageEntities.ContainsKey(m_currentStep.MessageEntityId))
                {
                    CrmMessageEntity msgEntity = Message[m_currentStep.MessageEntityId];
                    txtPrimaryEntity.Text = msgEntity.PrimaryEntity;
                    txtSecondaryEntity.Text = msgEntity.SecondaryEntity;
                }
                else
                {
                    txtPrimaryEntity.Text = "none";
                    txtSecondaryEntity.Text = "none";
                }

                if (plugin != null)
                {
                    cmbPlugins.SelectedItem = m_org[m_currentStep.AssemblyId][m_currentStep.PluginId];
                }

                if (plugin != null && serviceEndpoint != null)
                {
                    cmbServiceEndpoint.SelectedItem = m_org.ServiceEndpoints[m_currentStep.ServiceBusConfigurationId];
                }

                txtRank.Text = m_currentStep.Rank.ToString();
                switch (m_currentStep.Stage)
                {
                    case CrmPluginStepStage.PreValidation:
                        radStagePreValidation.Checked = true;
                        break;

                    case CrmPluginStepStage.PreOperation:
                        radStagePreOperation.Checked = true;
                        break;

                    case CrmPluginStepStage.PostOperation:
                        radStagePostOperation.Checked = true;
                        break;

                    case CrmPluginStepStage.PostOperationDeprecated:
                        radStagePostOperationDeprecated.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException("CrmPluginStepStage = " + m_currentStep.Stage.ToString());
                }

                switch (m_currentStep.Mode)
                {
                    case CrmPluginStepMode.Asynchronous:

                        radModeAsync.Checked = true;
                        break;

                    case CrmPluginStepMode.Synchronous:

                        radModeSync.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException("Mode = " + m_currentStep.Mode.ToString());
                }

                switch (m_currentStep.Deployment)
                {
                    case CrmPluginStepDeployment.Both:

                        chkDeploymentOffline.Checked = true;
                        chkDeploymentServer.Checked = true;
                        break;

                    case CrmPluginStepDeployment.ServerOnly:

                        chkDeploymentOffline.Checked = false;
                        chkDeploymentServer.Checked = true;
                        break;

                    case CrmPluginStepDeployment.OfflineOnly:

                        chkDeploymentOffline.Checked = true;
                        chkDeploymentServer.Checked = false;
                        break;

                    default:
                        throw new NotImplementedException("Deployment = " + m_currentStep.Deployment.ToString());
                }

                switch (m_currentStep.InvocationSource)
                {
                    case null:
                    case CrmPluginStepInvocationSource.Parent:

                        radInvocationParent.Checked = true;
                        break;

                    case CrmPluginStepInvocationSource.Child:

                        radInvocationChild.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException("InvocationSource = " + m_currentStep.InvocationSource.ToString());
                }

                txtDescription.Text = m_currentStep.Description;

                txtSecureConfig.Text = m_currentStep.SecureConfiguration;

                string stepName;
                //if (this.m_currentStep.IsProfiled && org.Plugins[this.m_currentStep.PluginId].IsProfilerPlugin)
                //{
                //    //If the current step is a profiler step, the form that is displayed should use the configuration from the original step.
                //    // ProfilerConfiguration profilerConfig = OrganizationHelper.RetrieveProfilerConfiguration(this.m_currentStep);
                //    // stepName = profilerConfig.OriginalEventHandlerName;
                //    // txtUnsecureConfiguration.Text = profilerConfig.Configuration;
                //}
                //else
                //{
                txtUnsecureConfiguration.Text = m_currentStep.UnsecureConfiguration;
                stepName = m_currentStep.Name;
                //}

                if (stepName == GenerateDescription())
                {
                    m_stepName = GenerateDescription();
                }
                else
                {
                    m_stepName = null;
                    txtName.Text = stepName;
                }

                if (MessageEntity != null)
                {
                    crmFilteringAttributes.EntityName = MessageEntity.PrimaryEntity;
                }

                crmFilteringAttributes.Attributes = m_currentStep.FilteringAttributes;
                ApplyFilteringAttributesLayout();
                chkDeleteAsyncOperationIfSuccessful.Checked = m_currentStep.DeleteAsyncOperationIfSuccessful;
                chkDeleteAsyncOperationIfSuccessful.Enabled = (m_currentStep.Mode == CrmPluginStepMode.Asynchronous);

                Text = "Update Existing Step";
                btnRegister.Text = "Update";

                CheckAttributesSupported();
            }
            else if (!radStagePostOperation.Enabled && radStagePostOperationDeprecated.Enabled)
            {
                radStagePostOperationDeprecated.Checked = true;
            }

            //Check if permissions for the secure configuration was denied
            if (org.SecureConfigurationPermissionDenied)
            {
                picAccessDenied.Visible = true;
                lblAccessDenied.Visible = true;
                txtSecureConfig.Visible = false;

                picAccessDenied.Image = CrmResources.LoadImage("AccessDenied");

                lblAccessDenied.Left = picAccessDenied.Right;

                int groupLeft = (grpSecureConfiguration.ClientSize.Width - (lblAccessDenied.Right - picAccessDenied.Left)) / 2;
                int groupTop = (grpSecureConfiguration.ClientSize.Height - lblAccessDenied.Height) / 2;

                picAccessDenied.Top = groupTop;
                lblAccessDenied.Top = groupTop;

                picAccessDenied.Left = groupLeft;
                lblAccessDenied.Left = picAccessDenied.Right;
            }
            else if (null != step && step.SecureConfigurationRecordIdInvalid)
            {
                picInvalidSecureConfigurationId.Visible = true;
                lblInvalidSecureConfigurationId.Visible = true;
                lnkInvalidSecureConfigurationId.Visible = true;
                txtSecureConfig.Visible = false;

                picInvalidSecureConfigurationId.Image = CrmResources.LoadImage("AccessDenied");
                lblInvalidSecureConfigurationId.Left = picInvalidSecureConfigurationId.Right;

                int groupLeft = (grpSecureConfiguration.ClientSize.Width - (lblInvalidSecureConfigurationId.Right - picInvalidSecureConfigurationId.Left)) / 2;
                int groupTop = (grpSecureConfiguration.ClientSize.Height - (lnkInvalidSecureConfigurationId.Bottom - picInvalidSecureConfigurationId.Top)) / 2;

                picInvalidSecureConfigurationId.Top = groupTop;
                lblInvalidSecureConfigurationId.Top = groupTop;
                lnkInvalidSecureConfigurationId.Top = lblInvalidSecureConfigurationId.Bottom + 6;

                picInvalidSecureConfigurationId.Left = groupLeft;
                lblInvalidSecureConfigurationId.Left = picAccessDenied.Right;
                lnkInvalidSecureConfigurationId.Left = lblInvalidSecureConfigurationId.Left;

                m_secureConfigurationIdIsInvalid = true;
            }

            LoadEntities();
            CheckDeploymentSupported();
        }

        #endregion Public Constructors

        #region Public Properties

        public CrmMessage Message
        {
            get
            {
                if (txtMessageName.TextLength != 0)
                {
                    string message = txtMessageName.Text.Trim();
                    if (!message.Equals(m_messageRetrieved, StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_messageRetrieved = message;
                        m_message = m_org.FindMessage(message);

                        m_messageEntityPrimaryRetrieved = null;
                        m_messageEntitySecondaryRetrieved = null;
                        m_messageEntityRetrieved = null;
                    }
                }
                else
                {
                    m_messageRetrieved = null;
                    m_message = null;
                    m_messageEntityPrimaryRetrieved = null;
                    m_messageEntitySecondaryRetrieved = null;
                    m_messageEntityRetrieved = null;
                }

                return m_message;
            }
        }

        public CrmMessageEntity MessageEntity
        {
            get
            {
                if (Message != null)
                {
                    string primaryEntity = txtPrimaryEntity.Text.Trim();
                    if (string.IsNullOrEmpty(primaryEntity))
                    {
                        primaryEntity = "none";
                    }

                    string secondaryEntity = txtSecondaryEntity.Text.Trim();
                    if (string.IsNullOrEmpty(secondaryEntity))
                    {
                        secondaryEntity = "none";
                    }

                    if (!string.Equals(primaryEntity, m_messageEntityPrimaryRetrieved, StringComparison.InvariantCultureIgnoreCase) ||
                        !string.Equals(secondaryEntity, m_messageEntitySecondaryRetrieved, StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_messageEntityPrimaryRetrieved = primaryEntity;
                        m_messageEntitySecondaryRetrieved = secondaryEntity;
                        m_messageEntityRetrieved = Message.FindMessageEntity(primaryEntity, secondaryEntity);
                    }
                }
                else
                {
                    m_messageEntityPrimaryRetrieved = null;
                    m_messageEntitySecondaryRetrieved = null;
                    m_messageEntityRetrieved = null;
                }

                return m_messageEntityRetrieved;
            }
        }

        #endregion Public Properties

        #region Private Methods

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            CrmPluginStep step = new CrmPluginStep(m_org);

            bool isDeploymentOfflineChecked = (chkDeploymentOffline.Enabled && chkDeploymentOffline.Checked);

            //The Server Deployment box should be considered "Checked" when it is enabled and checked OR when it is a service
            //endpoint Step (this is the case when the combo box is enabled)
            bool isDeploymentServerChecked = (chkDeploymentServer.Enabled && chkDeploymentServer.Checked) || cmbServiceEndpoint.Visible;

            #region Extract Information

            //Validate information
            if (!isDeploymentOfflineChecked && !isDeploymentServerChecked)
            {
                MessageBox.Show("At least one Step Deployment must be specified", "Step Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkDeploymentServer.Focus();
                return;
            }
            else if (Message == null)
            {
                MessageBox.Show("Invalid Message Name specified. Please re-enter the message name",
                    "Invalid Message Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMessageName.Focus();
                return;
            }
            else if (radModeAsync.Checked && !(radStagePostOperation.Checked || radStagePostOperationDeprecated.Checked))
            {
                MessageBox.Show("Asynchronous Execution Mode requires registration in one of the Post Stages. Please change the Mode or the Stage.",
                    "Step Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (txtName.TextLength == 0)
            {
                MessageBox.Show("Name is a required field.", "Step Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }
            else if (MessageEntity == null)
            {
                MessageBox.Show("Invalid Primary Entity or Secondary Entity specified. Please re-enter the data.",
                    "Invalid Entity Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrimaryEntity.Focus();
                return;
            }
            else if (MessageEntity.PrimaryEntity != txtPrimaryEntity.Text && txtPrimaryEntity.Text.Length > 0)
            {
                MessageBox.Show("Invalid Primary Entity specified. Please re-enter the data.",
                    "Invalid Entity Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPrimaryEntity.Focus();
                return;
            }
            else if (MessageEntity.SecondaryEntity != txtSecondaryEntity.Text && txtSecondaryEntity.Text.Length > 0)
            {
                MessageBox.Show("Invalid Secondary Entity specified. Please re-enter the data.",
                    "Invalid Entity Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSecondaryEntity.Focus();
                return;
            }
            else if (cmbPlugins.Items.Count > 0 && cmbPlugins.SelectedIndex < 0)
            {
                MessageBox.Show("Plugin was not specified. This a required field.", "Step Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbPlugins.Focus();
                return;
            }
            else if (cmbUsers.SelectedIndex < 0)
            {
                MessageBox.Show("User was not specified. This a required field.", "Step Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbUsers.Focus();
                return;
            }
            else if (txtName.TextLength == 0)
            {
                txtName.Text = GenerateDescription();
            }
            else if (isDeploymentOfflineChecked &&
                txtSecureConfig.TextLength != 0 && txtSecureConfig.Text.Trim().Length != 0)
            {
                MessageBox.Show(this, "Secure Configuration is not supported for Steps deployed Offline.",
                    "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CrmPlugin plugin = null;

            if (cmbPlugins.Visible)
            {
                plugin = (CrmPlugin)cmbPlugins.SelectedItem;
            }

            if (cmbServiceEndpoint.Visible)
            {
                if ((radModeSync.Checked))
                {
                    MessageBox.Show("Only asynchronous steps are supported for Service Endpoint plug-ins.",
                        "Step Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (cmbServiceEndpoint.SelectedIndex < 0)
                {
                    MessageBox.Show("Service Endpoint must be selected for Service Endpoint plug-ins.",
                        "Step Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (Message.SupportsFilteredAttributes && crmFilteringAttributes.Enabled)
            {
                if (crmFilteringAttributes.AllAttributes && Message.Name != "Create")
                {
                    if (MessageBox.Show("Registering steps filtering on updates of ALL attributes is highly discouraged for performance reasons.\nPlease reconsider this pattern.\n\nYes, I want to specify explicit attributes.\nNo, I don't care about performance now.", "Registration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        return;
                    }
                }
                else if (!crmFilteringAttributes.HasAttributes)
                {
                    MessageBox.Show("Filtering Attributes must be specified for this Message / Entity.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            step.MessageId = Message.MessageId;
            // Only set MessageEntityId if primary entity is not "none"
            // When primary entity is "none", leave MessageEntityId as Guid.Empty to avoid
            // triggering Dataverse dependency calculation errors for stale sdkmessagefilter records
            if (!string.Equals(MessageEntity?.PrimaryEntity, "none", StringComparison.InvariantCultureIgnoreCase))
            {
                step.MessageEntityId = MessageEntity.MessageEntityId;
            }
            else
            {
                step.MessageEntityId = Guid.Empty;
            }
            step.AssemblyId = plugin?.AssemblyId ?? Guid.Empty;
            step.FilteringAttributes = crmFilteringAttributes.Attributes;
            step.PluginId = plugin?.PluginId ?? ((CrmServiceEndpoint)cmbWebhook.SelectedItem).EntityId;
            step.ImpersonatingUserId = ((CrmUser)cmbUsers.SelectedItem).UserId;
            step.Name = txtName.Text;
            step.UnsecureConfiguration = txtUnsecureConfiguration.Text;
            step.Description = txtDescription.Text;

            if (m_currentStep?.EventHandler != null)
            {
                step.EventHandler = m_currentStep?.EventHandler;
            }

            if (txtSecureConfig.Visible)
            {
                step.SecureConfiguration = txtSecureConfig.Text;
            }

            step.ServiceBusConfigurationId = cmbServiceEndpoint.Visible ? ((CrmServiceEndpoint)cmbServiceEndpoint.SelectedItem).ServiceEndpointId : Guid.Empty;

            step.Rank = int.Parse(txtRank.Text);
            step.Mode = (radModeAsync.Checked ? CrmPluginStepMode.Asynchronous : CrmPluginStepMode.Synchronous);

            if (radStagePreValidation.Checked)
            {
                step.Stage = CrmPluginStepStage.PreValidation;
            }
            else if (radStagePreOperation.Checked)
            {
                step.Stage = CrmPluginStepStage.PreOperation;
            }
            else if (radStagePostOperation.Checked)
            {
                step.Stage = CrmPluginStepStage.PostOperation;
            }
            else if (radStagePostOperationDeprecated.Checked)
            {
                step.Stage = CrmPluginStepStage.PostOperationDeprecated;
            }
            else
            {
                throw new NotImplementedException("Unkown Plug-in Stage checked");
            }

            if (null != m_currentStep)
            {
                step.Enabled = m_currentStep.Enabled;
            }

            if (isDeploymentServerChecked && isDeploymentOfflineChecked)
            {
                step.Deployment = CrmPluginStepDeployment.Both;
            }
            else if (isDeploymentOfflineChecked)
            {
                step.Deployment = CrmPluginStepDeployment.OfflineOnly;
            }
            else
            {
                step.Deployment = CrmPluginStepDeployment.ServerOnly;
            }

            if (grpInvocation.Enabled)
            {
                step.InvocationSource = (radInvocationParent.Checked ? CrmPluginStepInvocationSource.Parent : CrmPluginStepInvocationSource.Child);
            }
            else
            {
                step.InvocationSource = null;
            }

            if (step.Mode == CrmPluginStepMode.Asynchronous)
            {
                step.DeleteAsyncOperationIfSuccessful = chkDeleteAsyncOperationIfSuccessful.Checked;
            }
            else
            {
                step.DeleteAsyncOperationIfSuccessful = false;
            }

            if (plugin != null && plugin.IsProfilerPlugin)
            {
                //step.ProfilerStepId = step.StepId;
                //step.UnsecureConfiguration = OrganizationHelper.UpdateWithStandaloneConfiguration(step).ToString();
            }
            else if (null != m_currentStep)
            {
                step.ProfilerOriginalStepId = m_currentStep.ProfilerOriginalStepId;
                step.ProfilerStepId = m_currentStep.ProfilerStepId;
            }

            #endregion Extract Information

            #region Register the Step

            bool rankChanged = false;
            try
            {
                if (m_currentStep != null)
                {
                    Guid? secureConfigurationId = m_currentStep.SecureConfigurationId;
                    if (m_currentStep.SecureConfigurationRecordIdInvalid)
                    {
                        if (m_secureConfigurationIdIsInvalid)
                        {
                            secureConfigurationId = null;
                        }
                        else
                        {
                            secureConfigurationId = Guid.Empty;
                        }
                    }

                    // If the message has changed, the images may need to change as well
                    List<CrmPluginImage> updateImages = null;
                    if (m_currentStep.MessageId != step.MessageId)
                    {
                        // Add the images for the current step to the list
                        updateImages = new List<CrmPluginImage>(m_currentStep.Images.Count);
                        updateImages.AddRange(m_currentStep.Images.Values);
                    }

                    step.StepId = m_currentStep.StepId;
                    if (!RegistrationHelper.UpdateStep(m_org, step, secureConfigurationId, updateImages))
                    {
                        DialogResult = System.Windows.Forms.DialogResult.None;
                        return;
                    }

                    ////Refresh the profiler step to have the same settings
                    //if (step.IsProfiled)
                    //{
                    //    OrganizationHelper.RefreshProfilerStep(step);
                    //}

                    rankChanged = (m_currentStep.Rank != step.Rank);

                    m_currentStep.SecureConfigurationRecordIdInvalid = m_secureConfigurationIdIsInvalid;
                    m_currentStep.Deployment = step.Deployment;
                    m_currentStep.Name = step.Name;
                    m_currentStep.ImpersonatingUserId = step.ImpersonatingUserId;
                    m_currentStep.InvocationSource = step.InvocationSource;
                    m_currentStep.MessageEntityId = step.MessageEntityId;
                    m_currentStep.MessageId = step.MessageId;
                    m_currentStep.FilteringAttributes = step.FilteringAttributes;
                    m_currentStep.Mode = step.Mode;
                    m_currentStep.Rank = step.Rank;
                    m_currentStep.DeleteAsyncOperationIfSuccessful = step.DeleteAsyncOperationIfSuccessful;
                    if (txtSecureConfig.Visible)
                    {
                        m_currentStep.SecureConfiguration = step.SecureConfiguration;
                        m_currentStep.SecureConfigurationId = step.SecureConfigurationId;
                    }
                    m_currentStep.Stage = step.Stage;
                    m_currentStep.UnsecureConfiguration = step.UnsecureConfiguration;
                    m_currentStep.Description = step.Description;
                    m_currentStep.ProfilerStepId = step.ProfilerStepId;
                    m_currentStep.EventHandler = step.EventHandler;

                    List<ICrmEntity> stepList = new List<ICrmEntity>(new ICrmEntity[] { step });
                    OrganizationHelper.UpdateDates(m_org, stepList);

                    if (m_currentStep.PluginId != step.PluginId)
                    {
                        m_orgControl.RemoveStep(step.NodeId);
                        //  m_org.Assemblies[m_currentStep.AssemblyId][m_currentStep.PluginId].RemoveStep(step.StepId); // the previous instruction does this too.

                        m_currentStep.AssemblyId = step.AssemblyId;
                        m_currentStep.PluginId = step.PluginId;
                        m_org.Assemblies[step.AssemblyId][step.PluginId].AddStep(step);
                        m_orgControl.AddStep(step);
                    }
                    else if (m_currentStep.ServiceBusConfigurationId != step.ServiceBusConfigurationId)
                    {
                        m_orgControl.RemoveStep(step.NodeId);
                        m_org.Assemblies[m_currentStep.AssemblyId][m_currentStep.PluginId].RemoveStep(step.StepId);

                        m_currentStep.ServiceBusConfigurationId = step.ServiceBusConfigurationId;
                        m_org.Assemblies[step.AssemblyId][step.PluginId].AddStep(step);
                        m_orgControl.AddStep(step);
                    }
                    else
                    {
                        m_orgControl.RefreshStep(m_currentStep);
                    }

                    step = m_currentStep;
                }
                else
                {
                    step.StepId = RegistrationHelper.RegisterStep(m_org, step);

                    List<ICrmEntity> stepList = new List<ICrmEntity>(new ICrmEntity[] { step });
                    OrganizationHelper.UpdateDates(m_org, stepList);

                    plugin?.AddStep(step);
                    m_orgControl.AddStep(step);
                    OrganizationHelper.RefreshStep(m_org, step);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Error occurred while registering the step", "Registration Error", ex);
                return;
            }

            #endregion Register the Step

            DialogResult = DialogResult.OK;
            Close();
        }

        private void CheckAttributesSupported()
        {
            //Check if we should disable the message
            if (null != Message && Message.SupportsFilteredAttributes && !string.Equals(MessageEntity?.PrimaryEntity?.ToLower(), "none"))
            {
                crmFilteringAttributes.Enabled = true;
                crmFilteringAttributes.DisabledMessage = null;
                if (m_currentStep == null)
                {   // Default to no attributes, to encourage explicitly selecting them
                    crmFilteringAttributes.ClearAttributes(Message.Name == "Create");
                }
            }
            else
            {
                crmFilteringAttributes.Enabled = false;
                crmFilteringAttributes.DisabledMessage = "Message/Entity does not support Filtered Attributes";
            }

            ApplyFilteringAttributesLayout();
        }

        private void CheckDeploymentSupported()
        {
            if (MessageEntity == null)
            {
                chkDeploymentOffline.Enabled = true;
                chkDeploymentServer.Enabled = true;
            }
            else
            {
                switch (MessageEntity.Availability)
                {
                    case CrmPluginStepDeployment.OfflineOnly:
                        chkDeploymentOffline.Enabled = true;
                        chkDeploymentServer.Enabled = false;
                        break;

                    case CrmPluginStepDeployment.ServerOnly:
                        chkDeploymentOffline.Enabled = false;
                        chkDeploymentServer.Enabled = true;
                        break;

                    case CrmPluginStepDeployment.Both:
                        chkDeploymentOffline.Enabled = true;
                        chkDeploymentServer.Enabled = true;
                        break;

                    default:
                        throw new NotImplementedException("CrmPluginStepDeployment = " + MessageEntity.Availability);
                }
            }
        }

        private void cmbPlugins_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Determine what boxes to enable
            var enableV4Controls = false;
            var enable2011Controls = true;

            //Todo: After removing SdkVersion property, removing v4 based features.
            //Enable the correct controls _(CRM2011 and above)
            grpInvocation.Enabled = enableV4Controls;
            radStagePreOperation.Enabled = enable2011Controls;
            radStagePostOperation.Enabled = enable2011Controls;
            radStagePostOperationDeprecated.Enabled = enableV4Controls;
        }

        private string GenerateDescription()
        {
            //Retrieve metadata about the filter
            string messageName;
            string primaryEntity = txtPrimaryEntity.Text;
            string secondaryEntity = txtSecondaryEntity.Text;
            if (Message == null)
            {
                messageName = txtMessageName.Text;
            }
            else
            {
                messageName = Message.Name;

                if (MessageEntity != null)
                {
                    primaryEntity = MessageEntity.PrimaryEntity;
                    secondaryEntity = MessageEntity.SecondaryEntity;
                }
            }

            //Retrieve the name of the type
            string typeName;
            if (cmbServiceEndpoint.Visible)
            {
                typeName = ((CrmServiceEndpoint)cmbServiceEndpoint.SelectedItem)?.Name;
            }
            else if (cmbPlugins.Visible)
            {
                CrmPlugin plugin = ((CrmPlugin)cmbPlugins.SelectedItem);
                typeName = plugin.IsProfilerPlugin ? "Plug-in Profiler" : ((CrmPlugin)cmbPlugins.SelectedItem).TypeName;
            }
            else if (cmbWebhook.Visible)
            {
                typeName = ((CrmServiceEndpoint)cmbWebhook.SelectedItem)?.Name;
            }
            else
            {
                typeName = null;
            }

            return RegistrationHelper.GenerateStepDescription(typeName, messageName, primaryEntity, secondaryEntity);
        }

        private void lnkInvalidSecureConfigurationId_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "The current value of the secure configuration will be overwritten when this step is saved. Continue?",
                    "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            m_secureConfigurationIdIsInvalid = false;

            txtSecureConfig.Visible = true;
            txtSecureConfig.Text = null;

            picInvalidSecureConfigurationId.Visible = false;
            lblInvalidSecureConfigurationId.Visible = false;
            lnkInvalidSecureConfigurationId.Visible = false;
        }

        private void LoadEntities()
        {
            if (m_messageLoaded == Message)
            {
                return;
            }
            else
            {
                m_messageLoaded = Message;
            }

            Control activeControl = ActiveControl;

            if (Message == null)
            {
                txtPrimaryEntity.AutoCompleteCustomSource = new AutoCompleteStringCollection();
                txtSecondaryEntity.AutoCompleteCustomSource = new AutoCompleteStringCollection();

                if (activeControl != null)
                {
                    activeControl.Focus();
                }
                return;
            }
            else
            {
                AutoCompleteStringCollection primaryList = new AutoCompleteStringCollection();
                AutoCompleteStringCollection secondaryList = new AutoCompleteStringCollection();
                foreach (CrmMessageEntity entity in Message.FindMessageEntities(null, null))
                {
                    if (!string.IsNullOrEmpty(entity.PrimaryEntity))
                    {
                        primaryList.Add(entity.PrimaryEntity);
                    }

                    if (!string.IsNullOrEmpty(entity.SecondaryEntity))
                    {
                        secondaryList.Add(entity.SecondaryEntity);
                    }
                }

                txtPrimaryEntity.AutoCompleteCustomSource = primaryList;
                txtSecondaryEntity.AutoCompleteCustomSource = secondaryList;

                if (activeControl != null)
                {
                    activeControl.Focus();
                }
            }
        }

        private void MessageData_TextChanged(object sender, EventArgs e)
        {
            if (m_stepName != null)
            {
                m_stepName = GenerateDescription();
                txtName.Text = m_stepName;
            }
        }

        private void MessageEntityData_Leave(object sender, EventArgs e)
        {
            if (MessageEntity != null)
            {
                if (!string.Equals(crmFilteringAttributes.EntityName, MessageEntity))
                {
                    crmFilteringAttributes.EntityName = MessageEntity.PrimaryEntity;
                }
            }

            CheckDeploymentSupported();
            CheckAttributesSupported();
        }

        private void radInvocationChild_CheckedChanged(object sender, System.EventArgs e)
        {
            if (m_stepName != null)
            {
                m_stepName = GenerateDescription();
                txtName.Text = m_stepName;
            }
        }

        private void radInvocationParent_CheckedChanged(object sender, System.EventArgs e)
        {
            if (m_stepName != null)
            {
                m_stepName = GenerateDescription();
                txtName.Text = m_stepName;
            }
        }

        private void radMode_CheckedChanged(object sender, EventArgs e)
        {
            chkDeleteAsyncOperationIfSuccessful.Enabled = radModeAsync.Checked;
        }

        private void StepRegistrationForm_Load(object sender, EventArgs e)
        {
            Invoke(new Action(() =>
            {
                //Add the users. We do not want to sort because the users list is sorted already and then the calling user
                //will not be at the beginning of the list
                cmbUsers.Sorted = false;
                foreach (CrmUser user in m_org.Users.Values)
                {
                    // Added this check to to prevent OutofMemoryExcetion - When an org is imported, the administrator
                    // does not have a name associated with it, Adding Null Names to ComboBox throws OutofMemoryExcetion
                    if (null != user && user.Enabled == true && user.Name != null)
                    {
                        cmbUsers.Items.Add(user);
                    }
                    // Special case to add System user
                    if (user.Name == "SYSTEM" && user.Enabled == false)
                    {
                        cmbUsers.Items.Add(user);
                    }
                }
                if (cmbUsers.Items.Count != 0)
                {
                    cmbUsers.SelectedIndex = 0;
                }
                if (m_currentStep != null)
                {
                    if (m_currentStep.ImpersonatingUserId == Guid.Empty)
                    {
                        cmbUsers.SelectedIndex = 0;
                    }
                    else
                    {
                        cmbUsers.SelectedItem = m_org.Users[m_currentStep.ImpersonatingUserId];
                    }
                }
            }));
        }

        private void txtMessageName_Leave(object sender, EventArgs e)
        {
            EnableAcceptButton();
            LoadEntities();
            CheckAttributesSupported();
        }

        private void txtMessageName_Validating(object sender, CancelEventArgs e)
        {
            if (txtMessageName.TextLength != 0 && Message == null)
            {
                MessageBox.Show("Invalid Message Name specified. Please re-enter the message name",
                    "Invalid Message Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
            else if (Message != null)
            {
                LoadEntities();
            }
        }

        private void txtName_Leave(object sender, EventArgs e)
        {
            EnableAcceptButton();
            if (txtName.TextLength == 0)
            {
                m_stepName = string.Empty;
                txtName.Text = m_stepName;
            }
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (m_stepName != null &&
                !m_stepName.Equals(txtName.Text, StringComparison.InvariantCultureIgnoreCase))
            {
                m_stepName = GenerateDescription();
            }
        }

        /// <summary>
        /// Records where everything sits before any growth is applied, so that
        /// ApplyFilteringAttributesLayout can always work from the original positions
        /// rather than accumulating offsets.
        /// </summary>
        private void CaptureFilteringAttributesLayout()
        {
            m_baseFilteringAttributesHeight = crmFilteringAttributes.Height;
            m_baseGeneralHeight = grpGeneral.Height;

            foreach (Control control in grpGeneral.Controls)
            {
                //Strictly below the row, so that lblFilteringAttributes stays put
                if (control.Top >= crmFilteringAttributes.Bottom)
                {
                    m_baseTopBelowFilteringAttributes.Add(control, control.Top);
                }
            }

            m_baseLeftColumnHeight = 0;
            foreach (Control control in pnlLeftColumn.Controls)
            {
                if (control != grpGeneral)
                {
                    m_baseTopBelowGeneral.Add(control, control.Top);
                }

                if (control.Bottom > m_baseLeftColumnHeight)
                {
                    m_baseLeftColumnHeight = control.Bottom;
                }
            }

            m_filteringAttributesLayoutCaptured = true;
        }

        /// <summary>
        /// Grows the Filtering Attributes field so a long attribute list becomes readable,
        /// but only as far as the value needs and as far as the spare height in the left
        /// column allows. Everything below it moves down by the same amount.
        /// </summary>
        private void ApplyFilteringAttributesLayout()
        {
            if (!m_filteringAttributesLayoutCaptured)
            {
                return;
            }

            int spare = pnlLeftColumn.ClientSize.Height - m_baseLeftColumnHeight;
            int wanted = crmFilteringAttributes.ContentHeight - m_baseFilteringAttributesHeight;
            int growth = crmFilteringAttributes.Enabled ? Math.Max(0, Math.Min(wanted, spare)) : 0;

            if (growth != m_appliedFilteringAttributesGrowth)
            {
                pnlLeftColumn.SuspendLayout();
                grpGeneral.SuspendLayout();

                crmFilteringAttributes.Height = m_baseFilteringAttributesHeight + growth;
                foreach (KeyValuePair<Control, int> control in m_baseTopBelowFilteringAttributes)
                {
                    control.Key.Top = control.Value + growth;
                }

                grpGeneral.Height = m_baseGeneralHeight + growth;
                foreach (KeyValuePair<Control, int> control in m_baseTopBelowGeneral)
                {
                    control.Key.Top = control.Value + growth;
                }

                grpGeneral.ResumeLayout();
                pnlLeftColumn.ResumeLayout();

                m_appliedFilteringAttributesGrowth = growth;
            }

            //Only offer a scrollbar when the value still does not fit after growing
            ScrollBars scrollBars = wanted > growth ? ScrollBars.Vertical : ScrollBars.None;
            if (crmFilteringAttributes.ScrollBars != scrollBars)
            {
                crmFilteringAttributes.ScrollBars = scrollBars;
            }
        }

        private void crmFilteringAttributes_AttributesChanged(object sender, EventArgs e)
        {
            ApplyFilteringAttributesLayout();
        }

        private void pnlLeftColumn_SizeChanged(object sender, EventArgs e)
        {
            ApplyFilteringAttributesLayout();
        }

        private void txtRank_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsNumber(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        private void UpdatePluginEventHandlerControls(bool isServiceEndpoint, bool isWebhook)
        {
            cmbServiceEndpoint.Location = cmbPlugins.Location;
            cmbServiceEndpoint.Size = cmbPlugins.Size;

            cmbWebhook.Location = cmbPlugins.Location;
            cmbWebhook.Size = cmbPlugins.Size;

            grpStage.Enabled = !isServiceEndpoint || isWebhook;

            cmbPlugins.Enabled = !isServiceEndpoint;
            cmbPlugins.Visible = !isServiceEndpoint;

            cmbServiceEndpoint.Visible = isServiceEndpoint && !isWebhook;
            cmbServiceEndpoint.Enabled = isServiceEndpoint && !isWebhook;

            cmbWebhook.Visible = isWebhook;
            cmbWebhook.Enabled = isWebhook;

            txtUnsecureConfiguration.Enabled = !isServiceEndpoint;
            txtSecureConfig.Enabled = !isServiceEndpoint;

            grpDeployment.Enabled = !isServiceEndpoint || isWebhook;
            grpMode.Enabled = !isServiceEndpoint || isWebhook;

            if (isServiceEndpoint)
            {
                chkDeploymentOffline.Checked = false;
                chkDeploymentServer.Checked = true;
                radStagePostOperation.Checked = true;
                radModeAsync.Checked = true;
            }
        }
        /// <summary>
        /// This method is used to disable the Accept button when entering a multiline text box to prevent accidental submissions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void longText_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }
        /// <summary>
        /// This method is used to re-enable the Accept button when leaving a multiline text box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void longText_Leave(object sender, EventArgs e)
        {
            EnableAcceptButton();
        }
        private void DisableAcceptButton()
        {
            AcceptButton = null;
        }
        private void EnableAcceptButton()
        {
            AcceptButton = btnRegister;
        }
        #endregion Private Methods

        private void txtMessageName_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }

        private void txtPrimaryEntity_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }

        private void txtSecondaryEntity_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }

        private void txtName_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }

        private void cmbUsers_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }

        private void cmbUsers_Leave(object sender, EventArgs e)
        {
            EnableAcceptButton();
        }

        private void txtRank_Leave(object sender, EventArgs e)
        {
            EnableAcceptButton();
        }

        private void txtRank_Enter(object sender, EventArgs e)
        {
            DisableAcceptButton();
        }
    }
}
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

namespace Xrm.Sdk.PluginRegistration.Wrappers
{
    using Controls;
    using Entities;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;

    public sealed class CrmPluginStep : ICrmEntity, ICrmTreeNode, ICloneable
    {
        public const string RelationshipStepToSecureConfig = "sdkmessageprocessingstepsecureconfigid_sdkmessageprocessingstep";
        public const string RelationshipStepToImage = "sdkmessageprocessingstepid_sdkmessageprocessingstepimage";

        private CrmOrganization m_org;
        private Guid m_assemblyId = Guid.Empty;
        private Guid m_pluginId = Guid.Empty;
        private int m_customizationLevel = 1;
        private string m_filteringAttributes = null;
        private DateTime? m_createdOn = null;
        private DateTime? m_modifiedOn = null;
        private Guid m_serviceBusConfigurationId = Guid.Empty;

        private CrmEntityDictionary<CrmPluginImage> m_imageReadOnlyList = null;
        private Dictionary<Guid, CrmPluginImage> m_imageList = new Dictionary<Guid, CrmPluginImage>();

        public CrmPluginStep(CrmOrganization org)
        {
            m_org = org;
            Enabled = true;
            Mode = CrmPluginStepMode.Synchronous;
            Stage = CrmPluginStepStage.PostOperationDeprecated;
            Rank = 1;
            Deployment = CrmPluginStepDeployment.ServerOnly;
        }

        public CrmPluginStep(CrmOrganization org, Guid assemblyId, Guid pluginId, Guid stepId, Guid messageId, Guid messageEntityId,
            string name, string unsecureConfiguration, Guid secureConfigurationId, string secureConfiguration,
            Guid impersonatingUserId, CrmPluginStepMode mode, CrmPluginStepStage stage,
            CrmPluginStepDeployment deployment, CrmPluginStepInvocationSource? invocationSource, int rank, bool enabled,
            int customizationLevel, DateTime? createdOn, DateTime? modifiedOn, string filteringAttributes, Guid serviceBusConfigurationId)
            : this(org)
        {
            AssemblyId = assemblyId;
            Deployment = deployment;
            Name = name;
            Enabled = enabled;
            ImpersonatingUserId = impersonatingUserId;
            InvocationSource = invocationSource;
            MessageEntityId = messageEntityId;
            MessageId = messageId;
            Mode = mode;
            PluginId = pluginId;
            Rank = rank;
            SecureConfigurationId = secureConfigurationId;
            SecureConfiguration = secureConfiguration;
            Stage = stage;
            StepId = stepId;
            CustomizationLevel = customizationLevel;
            UnsecureConfiguration = unsecureConfiguration;
            FilteringAttributes = filteringAttributes;
            m_createdOn = createdOn;
            m_modifiedOn = modifiedOn;
            m_serviceBusConfigurationId = serviceBusConfigurationId;
        }

        public CrmPluginStep(CrmOrganization org, Guid assemblyId, SdkMessageProcessingStep step, string secureConfig)
            : this(org)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            RefreshFromSdkMessageProcessingStep(assemblyId, step, secureConfig);
        }

        #region Properties

        /// <summary>
        /// Retrieves the Created On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? CreatedOn
        {
            get
            {
                return m_createdOn;
            }
        }

        /// <summary>
        /// Retrieves the Modified On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? ModifiedOn
        {
            get
            {
                return m_modifiedOn;
            }
        }

        [Browsable(false)]
        public CrmOrganization Organization
        {
            get
            {
                return m_org;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                else if (m_org == null)
                {
                    m_org = value;
                    foreach (CrmPluginImage image in m_imageList.Values)
                    {
                        if (image.Organization == null)
                        {
                            image.Organization = m_org;
                        }
                        m_org.AddImage(this, image);
                    }
                }
                else
                {
                    throw new NotSupportedException("Cannot change the Organization once it has been set");
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid AssemblyId
        {
            get
            {
                return m_assemblyId;
            }

            set
            {
                if (value == m_assemblyId)
                {
                    return;
                }

                m_assemblyId = value;

                if (m_imageList != null)
                {
                    foreach (CrmPluginImage image in m_imageList.Values)
                    {
                        image.AssemblyId = value;
                    }
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid PluginId
        {
            get
            {
                return m_pluginId;
            }

            set
            {
                if (value == m_pluginId)
                {
                    return;
                }

                m_pluginId = value;

                if (m_imageList != null)
                {
                    foreach (CrmPluginImage image in m_imageList.Values)
                    {
                        image.PluginId = value;
                    }
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public EntityReference EventHandler { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid ServiceBusConfigurationId
        {
            get
            {
                return m_serviceBusConfigurationId;
            }

            set
            {
                m_serviceBusConfigurationId = value;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid StepId { get; set; }

        [Browsable(false)]
        public bool Enabled { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginStepMode Mode { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginStepStage Stage { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid MessageId { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public int Rank { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string FilteringAttributes { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginStepDeployment Deployment { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginStepInvocationSource? InvocationSource { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Name { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Description { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string UnsecureConfiguration { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid SecureConfigurationId { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string SecureConfiguration { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid ImpersonatingUserId { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid MessageEntityId { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public bool DeleteAsyncOperationIfSuccessful { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public int CustomizationLevel
        {
            get
            {
                return m_customizationLevel;
            }

            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Invalid CustomizationLevel specified");
                }

                m_customizationLevel = value;
            }
        }

        [Browsable(false)]
        public CrmPluginImage this[Guid imageId]
        {
            get
            {
                return m_imageList[imageId];
            }
        }

        [Browsable(false)]
        public CrmEntityDictionary<CrmPluginImage> Images
        {
            get
            {
                if (m_imageReadOnlyList == null)
                {
                    m_imageReadOnlyList = new CrmEntityDictionary<CrmPluginImage>(m_imageList);
                }

                return m_imageReadOnlyList;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public bool SecureConfigurationRecordIdInvalid { get; set; }

        [Browsable(false)]
        public bool IsProfiled
        {
            get
            {
                return (null != ProfilerStepId);
            }
        }

        [Browsable(false)]
        public Guid? ProfilerStepId { get; set; }

        /// <summary>
        /// ID for the Step that is being profiled
        /// </summary>
        [Browsable(false)]
        public Guid? ProfilerOriginalStepId { get; set; }

        #endregion Properties

        #region Public Helper Methods

        public void RefreshFromSdkMessageProcessingStep(Guid assemblyId, SdkMessageProcessingStep step, string secureConfig)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            if (step.SupportedDeployment != null)
            {
                Deployment = (CrmPluginStepDeployment)Enum.ToObject(typeof(CrmPluginStepDeployment), step.SupportedDeployment.Value);
            }

            if (step.StateCode != null)
            {
                Enabled = (step.StateCode.Value == SdkMessageProcessingStepState.Enabled);
            }

            if (step.ImpersonatingUserId != null)
            {
                ImpersonatingUserId = step.ImpersonatingUserId.Id;
            }

#pragma warning disable 0612
            if (step.InvocationSource != null)
            {
                InvocationSource = (CrmPluginStepInvocationSource)Enum.ToObject(typeof(CrmPluginStepInvocationSource), step.InvocationSource.Value);
            }
#pragma warning restore 0612

            if (step.SdkMessageFilterId != null)
            {
                MessageEntityId = step.SdkMessageFilterId.Id;
            }

            if (step.SdkMessageId != null)
            {
                MessageId = step.SdkMessageId.Id;
            }

            if (step.Mode != null)
            {
                Mode = (CrmPluginStepMode)Enum.ToObject(typeof(CrmPluginStepMode), step.Mode.Value);
            }

            if (step.CreatedOn != null && (step.CreatedOn.HasValue))
            {
                m_createdOn = step.CreatedOn.Value;
            }

            if (step.ModifiedOn != null && (step.ModifiedOn.HasValue))
            {
                m_modifiedOn = step.ModifiedOn.Value;
            }

#pragma warning disable 0612
            if (step.PluginTypeId != null)
            {
                PluginId = step.PluginTypeId.Id;
            }
#pragma warning restore 0612

            if (step.Rank != null && (step.Rank.HasValue))
            {
                Rank = step.Rank.Value;
            }

            if (step.SdkMessageProcessingStepSecureConfigId != null)
            {
                SecureConfigurationId = step.SdkMessageProcessingStepSecureConfigId.Id;
                SecureConfiguration = secureConfig;
            }
            else
            {
                SecureConfiguration = null;
                SecureConfigurationId = Guid.Empty;
            }

            // Step can be unregistered as ExchangeRate plugin Can be replaced to ISV Plugin. So 'customizationlevel' logic is no longer valid here.
            CustomizationLevel = 1;

            if (step.Stage != null)
            {
                Stage = (CrmPluginStepStage)Enum.ToObject(typeof(CrmPluginStepStage), step.Stage.Value);
            }

            AssemblyId = assemblyId;

            if (step.SdkMessageProcessingStepId != null)
            {
                StepId = step.SdkMessageProcessingStepId.Value;
            }

            if (step.Configuration != null)
            {
                UnsecureConfiguration = step.Configuration;
            }
            else
            {
                UnsecureConfiguration = null;
            }

            if (step.EventHandler != null)
            {
                EventHandler = step.EventHandler;
                if (EventHandler.LogicalName == ServiceEndpoint.EntityLogicalName)
                {
                    ServiceBusConfigurationId = step.EventHandler.Id;
                }
            }

            Name = step.Name;

            Description = step.Description;

            FilteringAttributes = step.FilteringAttributes;

            if (step.AsyncAutoDelete != null)
            {
                DeleteAsyncOperationIfSuccessful = (bool)step.AsyncAutoDelete;
            }
        }

        public override string ToString()
        {
            return NodeText;
        }

        #region Management Methods

        public void AddImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            m_imageList.Add(image.ImageId, image);

            if (Organization != null)
            {
                Organization.AddImage(this, image);
            }
        }

        public void ClearImages()
        {
            m_imageList.Clear();

            if (Organization != null)
            {
                Organization.ClearImages(StepId);
            }
        }

        public void RemoveImage(Guid imageId)
        {
            if (m_imageList.ContainsKey(imageId))
            {
                m_imageList.Remove(imageId);

                if (Organization != null)
                {
                    Organization.RemoveImage(this, imageId);
                }
            }
            else
            {
                throw new ArgumentException("Invalid Image Id", "imageId");
            }
        }

        #endregion Management Methods

        #endregion Public Helper Methods

        #region Private Helper Methods

        private string ConvertNullStringToEmpty(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }
            else
            {
                return val;
            }
        }

        #endregion Private Helper Methods

        #region ICrmTreeItem Members

        [XmlIgnore]
        [Browsable(false)]
        public string NodeText
        {
            get
            {
                return string.Format("({0}) {1}", NodeTypeLabel, string.IsNullOrWhiteSpace(Name) ? Description : Name);
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid NodeId
        {
            get
            {
                return StepId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                if (m_imageList == null || m_imageList.Count == 0)
                {
                    return new List<ICrmTreeNode> { };
                }
                else
                {
                    var children = new CrmPluginImage[m_imageList.Count];
                    m_imageList.Values.CopyTo(children, 0);

                    return children.ToList<ICrmTreeNode>();
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                if (IsProfiled)
                {
                    return CrmTreeNodeImageType.StepProfiled;
                }
                else if (Enabled)
                {
                    return CrmTreeNodeImageType.StepEnabled;
                }
                else
                {
                    return CrmTreeNodeImageType.StepDisabled;
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                if (IsProfiled)
                {
                    return CrmTreeNodeImageType.StepProfiledSelected;
                }
                else if (Enabled)
                {
                    return CrmTreeNodeImageType.StepEnabledSelected;
                }
                else
                {
                    return CrmTreeNodeImageType.StepDisabledSelected;
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                return CrmTreeNodeType.Step;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                return "Step";
            }
        }

        #endregion ICrmTreeItem Members

        #region ICrmEntity Members

        [XmlIgnore]
        [Browsable(false)]
        public string EntityType
        {
            get
            {
                return Entities.SdkMessageProcessingStep.EntityLogicalName;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid EntityId
        {
            get
            {
                return StepId;
            }
        }

        public Dictionary<string, object> GenerateCrmEntities()
        {
            Dictionary<string, object> entityList = new Dictionary<string, object>();
            //Check for Permissions denied
            bool saveSecureConfiguration = true;
            if (Organization != null)
            {
                saveSecureConfiguration = !Organization.SecureConfigurationPermissionDenied;
            }

            //Create the secure configuration entity
            SdkMessageProcessingStep sdkStep = new SdkMessageProcessingStep();
            // For Create cases, SecureConfig != null , SecureConfigurationId = null

            //For Update cases SecureConfigurationId != null , SecureConfig=null
            if (!string.IsNullOrEmpty(SecureConfiguration))
            {
                SdkMessageProcessingStepSecureConfig sdkSecureConfig = new SdkMessageProcessingStepSecureConfig();
                if (SecureConfigurationId != Guid.Empty)
                {
                    sdkSecureConfig.SdkMessageProcessingStepSecureConfigId = new Guid?();
                    sdkSecureConfig["sdkmessageprocessingstepsecureconfigid"] = SecureConfigurationId;

                    sdkStep.SdkMessageProcessingStepSecureConfigId = new EntityReference();
                    sdkStep.SdkMessageProcessingStepSecureConfigId.LogicalName = SdkMessageProcessingStepSecureConfig.EntityLogicalName;
                    sdkStep.SdkMessageProcessingStepSecureConfigId.Id = SecureConfigurationId;
                }

                sdkSecureConfig.SecureConfig = SecureConfiguration;

                entityList.Add(SdkMessageProcessingStepSecureConfig.EntityLogicalName, sdkSecureConfig);
            }

            //Create the main entity
            if (StepId != Guid.Empty)
            {
                sdkStep.SdkMessageProcessingStepId = new Guid?();
                sdkStep["sdkmessageprocessingstepid"] = StepId;
            }

            sdkStep.Configuration = UnsecureConfiguration;

            // A step loaded for a webhook or service endpoint has the endpoint as AssemblyId and a
            // plug-in id (the built-in Service Bus plug-in), so the endpoint configuration comes first
            if (ServiceBusConfigurationId != Guid.Empty)
            {
                sdkStep.EventHandler = new EntityReference(ServiceEndpoint.EntityLogicalName, ServiceBusConfigurationId);
            }
            else if (AssemblyId != Guid.Empty && PluginId != Guid.Empty)
            {
                sdkStep.EventHandler = new EntityReference(PluginType.EntityLogicalName, PluginId);
            }
            else
            {
                sdkStep.EventHandler = new EntityReference(ServiceEndpoint.EntityLogicalName, PluginId);
            }

            sdkStep.Name = Name;

            sdkStep.Mode = new OptionSetValue();
            sdkStep.Mode.Value = (int)Mode;

            sdkStep.Rank = new int?();
            sdkStep["rank"] = Rank;

            if (null != InvocationSource)
            {
#pragma warning disable 0612
                sdkStep.InvocationSource = new OptionSetValue((int)InvocationSource);
#pragma warning restore 0612
            }

            sdkStep.SdkMessageId = new EntityReference();
            sdkStep.SdkMessageId.LogicalName = SdkMessage.EntityLogicalName;

            if (MessageId == Guid.Empty)
            {
                sdkStep.SdkMessageId = null;
            }
            else
            {
                sdkStep.SdkMessageId.Id = MessageId;
            }

            if (MessageEntityId != Guid.Empty)
            {
                sdkStep.SdkMessageFilterId = new EntityReference(SdkMessageFilter.EntityLogicalName, MessageEntityId);
            }
            else if (StepId != Guid.Empty)
            {
                // Update case: explicit clear of sdkmessagefilterid when transitioning to "none"
                // This ensures any existing SdkMessageFilter reference on the step is removed.
                // For create cases (StepId == Guid.Empty) we continue to omit the field entirely
                // to avoid triggering Dataverse dependency calculation errors for some orgs.
                sdkStep.SdkMessageFilterId = null;
            }
            sdkStep.ImpersonatingUserId = new EntityReference();
            sdkStep.ImpersonatingUserId.LogicalName = SystemUser.EntityLogicalName;

            if (ImpersonatingUserId != Guid.Empty)
            {
                sdkStep.ImpersonatingUserId.Id = ImpersonatingUserId;
            }
            else
            {
                sdkStep.ImpersonatingUserId = null;
            }

            sdkStep.Stage = new OptionSetValue();
            sdkStep.Stage.Value = (int)Stage;

            sdkStep.SupportedDeployment = new OptionSetValue();
            sdkStep.SupportedDeployment.Value = (int)Deployment;

            if (!string.IsNullOrEmpty(FilteringAttributes))
            {
                sdkStep.FilteringAttributes = FilteringAttributes;
            }
            else if (StepId != Guid.Empty)
            {
                // Update: explicitly clear filtering attributes so existing values are removed
                sdkStep.FilteringAttributes = string.Empty;
            }
            // Create with no filtering attributes: omit the field entirely to avoid
            // triggering Dataverse dependency calculation on the entity type code

            sdkStep.AsyncAutoDelete = DeleteAsyncOperationIfSuccessful;

            sdkStep.Description = Description;

            entityList.Add(SdkMessageProcessingStep.EntityLogicalName, sdkStep);

            return entityList;
        }

        private static CrmEntityColumn[] m_entityColumns = null;

        [XmlIgnore]
        public static CrmEntityColumn[] Columns
        {
            get
            {
                if (m_entityColumns == null)
                {
                    m_entityColumns = new CrmEntityColumn[] {
                        new CrmEntityColumn("Name", "Name", typeof(string)),
                        new CrmEntityColumn("CreatedOn", "Created On", typeof(string)),
                        new CrmEntityColumn("ModifiedOn", "Modified On", typeof(string)),
                        new CrmEntityColumn("Message", "Message", typeof(string)),
                        new CrmEntityColumn("PrimaryEntity", "PrimaryEntity", typeof(string)),
                        new CrmEntityColumn("SecondaryEntity", "SecondaryEntity", typeof(string)),
                        new CrmEntityColumn("TypeName", "TypeName", typeof(string)),
                        new CrmEntityColumn("FilteringAttributes", "Filtering Attributes", typeof(string)),
                        new CrmEntityColumn("Impersonate", "Impersonate", typeof(string)),
                        new CrmEntityColumn("Mode", "Mode", typeof(string)),
                        new CrmEntityColumn("Deployment", "Deployment", typeof(string)),
                        new CrmEntityColumn("Invocation", "Invocation", typeof(string)),
                        new CrmEntityColumn("Stage", "Stage", typeof(string)),
                        new CrmEntityColumn("Enabled", "Enabled", typeof(bool)),
                        new CrmEntityColumn("Rank", "Rank", typeof(int)),
                        new CrmEntityColumn("Description", "Description", typeof(string)),
                        new CrmEntityColumn("UnsecureConfiguration", "Unsecure Configuration", typeof(string)),
                        new CrmEntityColumn("SecureConfiguration", "Secure Configuration", typeof(string)),
                        new CrmEntityColumn("MessageId", "MessageId", typeof(string)),
                        new CrmEntityColumn("MessageEntityId", "MessageEntityId", typeof(string)),
                        new CrmEntityColumn("Id", "StepId", typeof(Guid)),
                        new CrmEntityColumn("ServiceBusConfigurationId", "ServiceBus Config", typeof(Guid)),
                        new CrmEntityColumn("DeleteAsyncOperationIfSuccessful", "Delete Async.", typeof(string))
                        };
                }

                return m_entityColumns;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                Dictionary<string, object> valueList = new Dictionary<string, object>();
                valueList.Add("Id", StepId);
                valueList.Add("Name", ConvertNullStringToEmpty(Name));
                valueList.Add("CreatedOn", CreatedOn.HasValue ? CreatedOn.ToString() : "");
                valueList.Add("ModifiedOn", ModifiedOn.HasValue ? ModifiedOn.ToString() : "");

                if (MessageId == Guid.Empty)
                {
                    valueList.Add("Message", string.Empty);
                    valueList.Add("MessageId", Guid.Empty.ToString());
                    valueList.Add("MessageEntityId", MessageEntityId.ToString());
                    valueList.Add("PrimaryEntity", string.Empty);
                    valueList.Add("SecondaryEntity", string.Empty);
                }
                else if (Organization == null)
                {
                    valueList.Add("Message", MessageId.ToString());
                    valueList.Add("MessageId", MessageId.ToString());
                    valueList.Add("MessageEntityId", MessageEntityId.ToString());
                    valueList.Add("PrimaryEntity", MessageEntityId.ToString());
                    valueList.Add("SecondaryEntity", MessageEntityId.ToString());
                }
                else
                {
                    valueList.Add("MessageId", MessageId.ToString());
                    valueList.Add("MessageEntityId", MessageEntityId.ToString());

                    if (Organization.Messages.ContainsKey(MessageId))
                    {
                        valueList.Add("Message", Organization.Messages[MessageId].Name);

                        if (Organization.Messages[MessageId].MessageEntities.ContainsKey(MessageEntityId))
                        {
                            valueList.Add("PrimaryEntity", Organization.Messages[MessageId][MessageEntityId].PrimaryEntity);
                            valueList.Add("SecondaryEntity", Organization.Messages[MessageId][MessageEntityId].SecondaryEntity);
                        }
                        else if (MessageEntityId == Guid.Empty)
                        {
                            valueList.Add("PrimaryEntity", "none");
                            valueList.Add("SecondaryEntity", "none");
                        }
                        else
                        {
                            valueList.Add("PrimaryEntity", "Invalid MessageFilterId");
                            valueList.Add("SecondaryEntity", "Invalid MessageFilterId");
                        }
                    }
                    else
                    {
                        valueList.Add("Message", "Invalid Message");
                        valueList.Add("PrimaryEntity", string.Empty);
                        valueList.Add("SecondaryEntity", string.Empty);
                    }
                }
                if (Organization != null && Organization.Plugins != null && Organization.Plugins.ContainsKey(PluginId))
                {
                    valueList.Add("TypeName", Organization.Plugins[PluginId].TypeName);
                }
                else
                {
                    valueList.Add("TypeName", "Error - Unable to retrieve the TypeName");
                }

                if (ImpersonatingUserId == Guid.Empty)
                {
                    valueList.Add("Impersonate", "Calling User");
                }
                else if (Organization == null)
                {
                    valueList.Add("Impersonate", ImpersonatingUserId.ToString());
                }
                else if (Organization.Users.ContainsKey(ImpersonatingUserId))
                {
                    valueList.Add("Impersonate", Organization.Users[ImpersonatingUserId].Name);
                }
                else
                {
                    valueList.Add("Impersonate", "Invalid User");
                }
                valueList.Add("Mode", Mode.ToString());

                switch (Stage)
                {
                    case CrmPluginStepStage.PreValidation:

                        valueList.Add("Stage", "Pre Stage - Outside Transaction");
                        break;

                    case CrmPluginStepStage.PreOperation:

                        valueList.Add("Stage", "Pre Stage - Inside Transaction");
                        break;

                    case CrmPluginStepStage.PostOperation:

                        valueList.Add("Stage", "Post Stage - Inside Transaction");
                        break;

                    case CrmPluginStepStage.PostOperationDeprecated:

                        valueList.Add("Stage", "Post Stage - Outside Transaction");
                        break;

                    default:
                        throw new NotImplementedException("Stage = " + Stage.ToString());
                }

                switch (Deployment)
                {
                    case CrmPluginStepDeployment.ServerOnly:

                        valueList.Add("Deployment", "Server Only");
                        break;

                    case CrmPluginStepDeployment.OfflineOnly:

                        valueList.Add("Deployment", "Offline Only");
                        break;

                    case CrmPluginStepDeployment.Both:

                        valueList.Add("Deployment", "Server & Offline");
                        break;

                    default:
                        throw new NotImplementedException("Deployment = " + Deployment.ToString());
                }

                switch (InvocationSource)
                {
                    case CrmPluginStepInvocationSource.Parent:

                        valueList.Add("Invocation", "Parent Pipeline");
                        break;

                    case CrmPluginStepInvocationSource.Child:

                        valueList.Add("Invocation", "Child Pipeline");
                        break;
                }

                valueList.Add("Enabled", Enabled);
                valueList.Add("Rank", Rank);

                if (string.IsNullOrEmpty(FilteringAttributes))
                {
                    valueList.Add("FilteringAttributes", "All Attributes");
                }
                else
                {
                    valueList.Add("FilteringAttributes", FilteringAttributes);
                }

                valueList.Add("Description", ConvertNullStringToEmpty(Description));
                valueList.Add("UnsecureConfiguration", ConvertNullStringToEmpty(UnsecureConfiguration));
                valueList.Add("ServiceBusConfigurationId", ServiceBusConfigurationId);

                if (Organization != null && Organization.SecureConfigurationPermissionDenied)
                {
                    valueList.Add("SecureConfiguration", "Unable to Retrieve Value");
                }
                else
                {
                    valueList.Add("SecureConfiguration", ConvertNullStringToEmpty(SecureConfiguration));
                }

                if (DeleteAsyncOperationIfSuccessful)
                {
                    valueList.Add("DeleteAsyncOperationIfSuccessful", "If StatusCode = Successful");
                }
                else
                {
                    valueList.Add("DeleteAsyncOperationIfSuccessful", "Manually");
                }

                return valueList;
            }
        }

        [XmlIgnore]
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public bool IsSystemCrmEntity
        {
            get
            {
                return CustomizationLevel == 0;
            }
        }

        public void UpdateDates(DateTime? createdOn, DateTime? modifiedOn)
        {
            if (createdOn != null)
            {
                m_createdOn = createdOn;
            }

            if (modifiedOn != null)
            {
                m_modifiedOn = modifiedOn;
            }
        }

        #endregion ICrmEntity Members

        #region ICloneable Members

        public object Clone()
        {
            return Clone(true);
        }

        public CrmPluginStep Clone(bool includeOrganization)
        {
            CrmPluginStep newStep;
            if (includeOrganization)
            {
                newStep = new CrmPluginStep(m_org);
            }
            else
            {
                newStep = new CrmPluginStep(null);
            }

            newStep.m_assemblyId = m_assemblyId;
            newStep.UnsecureConfiguration = UnsecureConfiguration;
            newStep.m_createdOn = m_createdOn;
            newStep.m_customizationLevel = m_customizationLevel;
            newStep.Deployment = Deployment;
            newStep.Name = Name;
            newStep.Enabled = Enabled;
            newStep.MessageEntityId = MessageEntityId;
            newStep.m_filteringAttributes = m_filteringAttributes;
            newStep.ImpersonatingUserId = ImpersonatingUserId;
            newStep.InvocationSource = InvocationSource;
            newStep.MessageId = MessageId;
            newStep.Mode = Mode;
            newStep.m_modifiedOn = m_modifiedOn;
            newStep.m_pluginId = m_pluginId;
            newStep.Rank = Rank;
            newStep.SecureConfiguration = SecureConfiguration;
            newStep.SecureConfigurationId = SecureConfigurationId;
            newStep.Stage = Stage;
            newStep.StepId = StepId;
            newStep.m_serviceBusConfigurationId = m_serviceBusConfigurationId;
            //Create a new image list
            Dictionary<Guid, CrmPluginImage> newImageList = new Dictionary<Guid, CrmPluginImage>();
            foreach (CrmPluginImage image in m_imageList.Values)
            {
                //Clone the image
                CrmPluginImage clonedImage = image.Clone(includeOrganization);

                //Add the image to the new list
                newImageList.Add(clonedImage.ImageId, clonedImage);
            }

            //Assign the list to the new object
            newStep.m_imageList = newImageList;

            return newStep;
        }

        #endregion ICloneable Members
    }

    public enum CrmPluginStepMode
    {
        Asynchronous = 1,
        Synchronous = 0
    }

    public enum CrmPluginStepStage
    {
        [Description("Pre-validation")]
        PreValidation = 10,

        [Description("Pre-operation")]
        PreOperation = 20,

        [Description("Post-operation")]
        PostOperation = 40,

        [Description("Post-operation Deprecated")]
        PostOperationDeprecated = 50
    }

    public enum CrmPluginStepDeployment
    {
        [Description("Server only")]
        ServerOnly = 0,

        [Description("Offline only")]
        OfflineOnly = 1,

        [Description("Both")]
        Both = 2
    }

    public enum CrmPluginStepInvocationSource
    {
        Parent = 0,
        Child = 1
    }
}
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
    using System.Text;
    using System.Xml.Serialization;

    public enum CrmPluginImageType
    {
        PreImage = 0,
        PostImage = 1,
        Both = 2
    }

    public sealed class CrmPluginImage : ICrmEntity, ICrmTreeNode, ICloneable
    {
        #region Private Fields

        private static CrmEntityColumn[] m_entityColumns = null;
        private Guid m_assemblyId = Guid.Empty;
        private string m_attributes = null;
        private DateTime? m_createdOn = null;
        private int m_customizationLevel = 1;
        private string m_entityAlias = null;
        private Guid m_imageId = Guid.Empty;
        private DateTime? m_modifiedOn = null;
        private CrmOrganization m_org;
        private Guid m_pluginId = Guid.Empty;
        private string m_propertyName;
        private string m_propertyTitle = null;
        private string m_relatedAttribute = null;
        private Guid m_stepId = Guid.Empty;
        private CrmPluginImageType m_type = CrmPluginImageType.PostImage;

        #endregion Private Fields

        #region Public Constructors

        public CrmPluginImage(CrmOrganization org)
        {
            m_org = org;
        }

        public CrmPluginImage(CrmOrganization org, Guid assemblyId, Guid pluginId, Guid stepId, Guid imageId, string attributes,
            string relatedAttribute, string entityAlias, CrmPluginImageType imageType, string messagePropertyName, int customizationLevel,
            DateTime? createdOn, DateTime? modifiedOn)
            : this(org)
        {
            AssemblyId = assemblyId;
            PluginId = pluginId;
            StepId = stepId;
            ImageId = imageId;
            Attributes = attributes;
            RelatedAttribute = relatedAttribute;
            EntityAlias = entityAlias;
            ImageType = imageType;
            MessagePropertyName = messagePropertyName;
            CustomizationLevel = customizationLevel;
            UpdateDates(createdOn, modifiedOn);
        }

        public CrmPluginImage(CrmOrganization org, Guid assemblyId, Guid pluginId, SdkMessageProcessingStepImage image)
            : this(org)
        {
            RefreshFromSdkMessageProcessingStepImage(assemblyId, pluginId, image);
        }

        #endregion Public Constructors

        #region Public Properties

        [XmlIgnore]
        public static CrmEntityColumn[] Columns
        {
            get
            {
                if (m_entityColumns == null)
                {
                    m_entityColumns = new CrmEntityColumn[] {
                        new CrmEntityColumn("Name", "Name", typeof(string)),
                        new CrmEntityColumn("EntityAlias", "Entity Alias", typeof(string)),
                        new CrmEntityColumn("Type", "Image Type", typeof(string)),
                        new CrmEntityColumn("Attributes", "Attributes", typeof(string)),
                        new CrmEntityColumn("PropertyName", "Property Name", typeof(string)) ,
                        new CrmEntityColumn("Id", "ImageId", typeof(Guid))};
                }

                return m_entityColumns;
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
                m_assemblyId = value;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Attributes
        {
            get
            {
                return m_attributes;
            }

            set
            {
                m_attributes = value;
            }
        }

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

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string EntityAlias
        {
            get
            {
                return m_entityAlias;
            }

            set
            {
                m_entityAlias = value;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid EntityId
        {
            get
            {
                return m_imageId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string EntityType
        {
            get
            {
                return SdkMessageProcessingStepImage.EntityLogicalName;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid ImageId
        {
            get
            {
                return m_imageId;
            }

            set
            {
                m_imageId = value;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginImageType ImageType
        {
            get
            {
                return m_type;
            }

            set
            {
                m_type = value;
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

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string MessagePropertyName
        {
            get
            {
                return m_propertyName;
            }

            set
            {
                if (!string.Equals(m_propertyName, value))
                {
                    m_propertyName = value;
                    m_propertyTitle = null;
                }
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

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Name
        {
            get;
            set;
        }

        [XmlIgnore]
        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                return null;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid NodeId
        {
            get
            {
                return m_imageId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                return CrmTreeNodeImageType.Image;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                return CrmTreeNodeImageType.ImageSelected;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeText
        {
            get
            {
                string attributeList = m_attributes;
                if (string.IsNullOrEmpty(attributeList))
                {
                    attributeList = null;
                }
                else if (attributeList.Length > 100)
                {
                    attributeList = string.Format("{0}...", attributeList.Substring(0, 97));
                }

                string imageLabel;
                switch (ImageType)
                {
                    case CrmPluginImageType.PreImage:
                        imageLabel = "Pre Image";
                        break;

                    case CrmPluginImageType.PostImage:
                        imageLabel = "Post Image";
                        break;

                    case CrmPluginImageType.Both:
                        imageLabel = "Pre & Post Image";
                        break;

                    default:
                        throw new NotImplementedException($"ImageType = {ImageType.ToString()}");
                }
                bool missingInfoFlag = false;
                //Retrieve the MessagePropertyName object
                if (m_propertyTitle == null)
                {
                    if (string.IsNullOrEmpty(MessagePropertyName))
                    {
                        m_propertyTitle = string.Empty;
                    }
                    else if (Organization != null && Organization.Steps.ContainsKey(StepId))
                    {
                        CrmPluginStep step = Organization.Steps[StepId];

                        Guid messageId;
                        string primaryEntity;
                        if (Guid.Empty == step.MessageEntityId)
                        {
                            messageId = step.MessageId;
                            primaryEntity = null;
                        }
                        else
                        {
                            if (Organization.MessageEntities.TryGetValue(step.MessageEntityId, out CrmMessageEntity messageEntity))
                            {
                                messageId = messageEntity.MessageId;
                                primaryEntity = messageEntity.PrimaryEntity;
                            }
                            else
                            {
                                primaryEntity = null;
                                messageId = Guid.Empty;
                                missingInfoFlag = true;
                            }
                        }

                        m_propertyTitle = string.Empty;
                        if (!missingInfoFlag)
                        {
                            //Determine the title of the property
                            List<ImageMessagePropertyName> validProperties = m_org.Messages[messageId].ImageMessagePropertyNames;
                            if (0 != validProperties.Count && !string.IsNullOrEmpty(m_propertyName))
                            {
                                foreach (ImageMessagePropertyName property in validProperties)
                                {
                                    if (string.Equals(property.Name, m_propertyName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        m_propertyTitle = property.Label;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        m_propertyTitle = null;
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("({0}) ", NodeTypeLabel);
                if (!string.IsNullOrEmpty(m_entityAlias))
                {
                    sb.Append(EntityAlias);
                }
                else
                {
                    sb.Append(imageLabel);
                }

                if (!string.IsNullOrEmpty(attributeList))
                {
                    sb.AppendFormat(" ({0})", attributeList);
                }

                if (!string.IsNullOrEmpty(m_propertyTitle))
                {
                    sb.AppendFormat(" - {0}", m_propertyTitle);
                }

                return sb.ToString();
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                return CrmTreeNodeType.Image;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                return "Image";
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
                }
                else
                {
                    throw new NotSupportedException("Cannot change the Organization once it has been set");
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
                m_pluginId = value;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string RelatedAttribute
        {
            get
            {
                return m_relatedAttribute;
            }

            set
            {
                m_relatedAttribute = value;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid StepId
        {
            get
            {
                return m_stepId;
            }

            set
            {
                m_stepId = value;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                Dictionary<string, object> valueList = new Dictionary<string, object>();
                valueList.Add("Id", ImageId);
                valueList.Add("Name", String.IsNullOrEmpty(Name) ? string.Empty : Name);
                valueList.Add("EntityAlias", EntityAlias);
                valueList.Add("PropertyName", MessagePropertyName);

                if (string.IsNullOrEmpty(Attributes))
                {
                    valueList.Add("Attributes", "All Attributes");
                }
                else
                {
                    valueList.Add("Attributes", Attributes);
                }

                switch (ImageType)
                {
                    case CrmPluginImageType.PreImage:

                        valueList.Add("Type", "Pre Image");
                        break;

                    case CrmPluginImageType.PostImage:

                        valueList.Add("Type", "Post Image");
                        break;

                    case CrmPluginImageType.Both:

                        valueList.Add("Type", "Pre & Post Image");
                        break;

                    default:
                        throw new NotImplementedException("ImageType = " + ImageType.ToString());
                }

                return valueList;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public object Clone()
        {
            return Clone(true);
        }

        public CrmPluginImage Clone(bool includeOrganization)
        {
            CrmPluginImage newImage;
            if (includeOrganization)
            {
                newImage = new CrmPluginImage(m_org);
            }
            else
            {
                newImage = new CrmPluginImage(null);
            }

            newImage.m_assemblyId = m_assemblyId;
            newImage.m_attributes = m_attributes;
            newImage.m_createdOn = m_createdOn;
            newImage.m_customizationLevel = m_customizationLevel;
            newImage.m_entityAlias = m_entityAlias;
            newImage.m_imageId = m_imageId;
            newImage.m_modifiedOn = m_modifiedOn;
            newImage.m_pluginId = m_pluginId;
            newImage.m_propertyName = m_propertyName;
            newImage.m_propertyTitle = m_propertyTitle;
            newImage.m_relatedAttribute = m_relatedAttribute;
            newImage.m_stepId = m_stepId;
            newImage.m_type = m_type;

            return newImage;
        }

        public Dictionary<string, object> GenerateCrmEntities()
        {
            if (Organization != null)
            {
                // Steps on webhooks and service endpoints have no assembly, so look the step up directly
                CrmPluginStep step = Organization.Steps[StepId];
                return GenerateCrmEntities(step.MessageId, step.MessageEntityId);
            }
            return GenerateCrmEntities(Guid.Empty, Guid.Empty);
        }

        public Dictionary<string, object> GenerateCrmEntities(Guid sdkMessageId, Guid sdkMessageFilterId)
        {
            if (Organization != null)
            {
                if (string.IsNullOrEmpty(MessagePropertyName))
                {
                    List<ImageMessagePropertyName> validImages = Organization.Messages[sdkMessageId].ImageMessagePropertyNames;
                    if (0 != validImages.Count)
                    {
                        //Select the first one from the list
                        MessagePropertyName = validImages[0].Name;
                    }
                }
            }

            Dictionary<string, object> entityList = new Dictionary<string, object>();
            SdkMessageProcessingStepImage image = new SdkMessageProcessingStepImage();
            if (ImageId != Guid.Empty)
            {
                image.SdkMessageProcessingStepImageId = new Guid?();
                image["sdkmessageprocessingstepimageid"] = ImageId;
            }

            if (StepId != Guid.Empty)
            {
                image.SdkMessageProcessingStepId = new EntityReference();
                image.SdkMessageProcessingStepId.LogicalName = SdkMessageProcessingStep.EntityLogicalName;
                image.SdkMessageProcessingStepId.Id = StepId;
            }

            image.ImageType = new OptionSetValue();
            image.ImageType.Value = (int)ImageType;

            image.MessagePropertyName = MessagePropertyName;

            image.Name = Name;

            image.EntityAlias = EntityAlias;

            if (!string.IsNullOrEmpty(Attributes)) // null is all attributes
            {
                image.Attributes1 = Attributes;
            }
            else
            {
                image.Attributes1 = string.Empty;
            }
            if (!string.IsNullOrEmpty(RelatedAttribute))
            {
                image.RelatedAttributeName = RelatedAttribute; //For Related Entity Information
            }

            entityList.Add(SdkMessageProcessingStepImage.EntityLogicalName, image);

            return entityList;
        }

        public void RefreshFromSdkMessageProcessingStepImage(Guid assemblyId, Guid pluginId, SdkMessageProcessingStepImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            AssemblyId = assemblyId;
            PluginId = pluginId;

            if (image.SdkMessageProcessingStepImageId != null)
            {
                ImageId = image.SdkMessageProcessingStepImageId.Value;
            }

            if (image.Attributes != null)
            {
                Attributes = image.Attributes1;
            }

            if (image.EntityAlias != null)
            {
                EntityAlias = image.EntityAlias;
            }

            if (image.MessagePropertyName != null)
            {
                MessagePropertyName = image.MessagePropertyName;
            }

            if (image.RelatedAttributeName != null)
            {
                RelatedAttribute = image.RelatedAttributeName;
            }

            if (image.SdkMessageProcessingStepId != null)
            {
                StepId = image.SdkMessageProcessingStepId.Id;
            }

            if (image.ImageType != null)
            {
                ImageType = (CrmPluginImageType)Enum.ToObject(typeof(CrmPluginImageType), image.ImageType.Value);
            }

            if (image.CustomizationLevel != null)
            {
                m_customizationLevel = image.CustomizationLevel.Value;
            }

            if (image.CreatedOn != null && (image.CreatedOn.HasValue))
            {
                m_createdOn = image.CreatedOn.Value;
            }

            if (image.ModifiedOn != null && (image.ModifiedOn.HasValue))
            {
                m_modifiedOn = image.ModifiedOn.Value;
            }

            Name = image.Name;
        }

        public override string ToString()
        {
            return NodeText;
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

        #endregion Public Methods

        #region Private Methods

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

        #endregion Private Methods
    }
}
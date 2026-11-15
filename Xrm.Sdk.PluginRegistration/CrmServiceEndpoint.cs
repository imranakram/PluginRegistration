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

namespace Xrm.Sdk.PluginRegistration
{
    using Controls;
    using Entities;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;
    using Wrappers;

    public enum CrmServiceEndpointConnectionMode
    {
        Normal = 1,
        Federated = 2
    }

    public enum CrmServiceEndpointContract
    {
        OneWay = 1,
        Queue = 2,
        Rest = 3,
        TwoWay = 4,
        Topic = 5,
        PersistentQueue = 6,
        EventHub = 7,
        Webhook = 8,
        EventGrid = 9,
        // Created and managed by Dataverse itself (e.g. Azure Synapse Link / Fabric); listed, never edited
        ManagedDataLake = 10,
        ContainerStorage = 11
    }

    public enum CrmServiceEndpointMessageFormat
    {
        BinaryXml = 1,
        Json = 2,
        TextXml = 3
    }

    public enum CrmServiceEndpointUserClaim
    {
        None = 1,
        UserId = 2,
        UserInfo = 3
    }

    public enum CrmServiceEndpointAuthType
    {
        SASKey = 2,
        SASToken = 3,
        WebhookKey = 4,
        HttpHeader = 5,
        HttpQueryString = 6,
        AccessKey = 8
    }

    [TypeDescriptionProvider(typeof(CrmServiceEndpointDescriptionProvider))]
    public sealed class CrmServiceEndpoint : ICrmEntity, ICrmTreeNode
    {
        #region Public Fields

        public const string ServiceBusPluginAssemblyName = "Microsoft.Crm.ServiceBus";
        public const string ServiceBusPluginName = "Microsoft.Crm.ServiceBus.ServiceBusPlugin";
        public readonly Guid ServiceBusPlugin = new Guid("EF521E63-CD2B-4170-99F6-447466A7161E");
        public readonly Guid ServiceBusPluginAssembly = new Guid("A430B185-D19D-428C-B156-5EBE3F391564");

        #endregion Public Fields

        #region Private Fields
        private Dictionary<Guid, CrmPluginStep> m_stepList = new Dictionary<Guid, CrmPluginStep>();
        private static CrmEntityColumn[] m_entityColumns = null;
        private CrmOrganization m_org = null;

        #endregion Private Fields

        #region Public Constructors

        public CrmServiceEndpoint(CrmOrganization org)
        {
            m_org = org;
        }

        public CrmServiceEndpoint(CrmOrganization org, ServiceEndpoint serviceEndpoint)
            : this(org)
        {
            RefreshFromServiceEndpoint(serviceEndpoint);
        }

        #endregion Public Constructors

        #region Public Properties

        [XmlIgnore]
        [Browsable(false)]
        public static CrmEntityColumn[] Columns
        {
            get
            {
                if (m_entityColumns == null)
                {
                    m_entityColumns = new CrmEntityColumn[] {
                        new CrmEntityColumn("Id", "ServiceEndpointId", typeof(Guid)),
                        new CrmEntityColumn("Name", "Name", typeof(string)),
                        new CrmEntityColumn("Description", "Description", typeof(string)),
                        new CrmEntityColumn("SolutionNamespace", "Solution Namespace", typeof(string)),
                        new CrmEntityColumn("Path", "Path", typeof(string)),
                        new CrmEntityColumn("Contract", "Contract", typeof(string)),
                        new CrmEntityColumn("UserClaim", "User Claim", typeof(string)),
                        new CrmEntityColumn("NamespaceAddress", "Namespace Address", typeof(string)),
                        new CrmEntityColumn("MessageFormat", "Message Format", typeof(string)),
                        new CrmEntityColumn("ConnectionMode", "Connection Mode", typeof(string)),
                        new CrmEntityColumn("ModifiedOn", "Modified On", typeof(string)),
                        new CrmEntityColumn("Isolatable", "Isolatable", typeof(string)),
                        new CrmEntityColumn("Url", "Url", typeof(string)),
                        new CrmEntityColumn("AuthType", "AuthType", typeof(string)),
                    };
                }

                return m_entityColumns;
            }
        }

        [Category("Information"), Browsable(false), ReadOnly(true)]
        public String AcsIssuerName
        {
            get;
            set;
        }

        [Category("Information"), Browsable(false), ReadOnly(true)]
        public String AcsManagementKey
        {
            get;
            set;
        }

        [Category("Information"), Browsable(false), ReadOnly(true)]
        public String AcsPublicCertificate
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmServiceEndpointConnectionMode ConnectionMode
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmServiceEndpointContract Contract
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? CreatedOn { get; private set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Description
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Url
        {
            get;
            set;
        }

        [Browsable(false)]
        public Guid EntityId
        {
            get { return ServiceEndpointId; }
        }

        [Browsable(false)]
        public string AuthValue
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string NamespaceAddress
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmServiceEndpointMessageFormat? MessageFormat
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string SasKeyName
        {
            get;
            set;
        }

        // Dataverse never returns the SAS key or token, like authvalue. Null means "keep the saved value".
        [Browsable(false)]
        public string SasKey
        {
            get;
            set;
        }

        [Browsable(false)]
        public string SasToken
        {
            get;
            set;
        }

        /// <summary>
        /// True for the contracts that send to a Service Bus, Relay or Event Hub namespace
        /// (namespace address + path + SAS key or token); false for the URL based ones.
        /// </summary>
        [Browsable(false)]
        public bool UsesNamespace
        {
            get { return IsNamespaceContract(Contract); }
        }

        public static bool IsNamespaceContract(CrmServiceEndpointContract contract)
        {
            switch (contract)
            {
                case CrmServiceEndpointContract.OneWay:
                case CrmServiceEndpointContract.Queue:
                case CrmServiceEndpointContract.Rest:
                case CrmServiceEndpointContract.TwoWay:
                case CrmServiceEndpointContract.Topic:
                case CrmServiceEndpointContract.PersistentQueue:
                case CrmServiceEndpointContract.EventHub:
                    return true;
                default:
                    return false;
            }
        }

        [Browsable(false)]
        public string EntityType
        {
            get { return ServiceEndpoint.EntityLogicalName; }
        }

        /// <summary>
        /// Endpoint types the tool can't register or edit, such as the Managed Data Lake endpoint
        /// Dataverse creates for itself, count as system items: no update, unregister or new steps.
        /// </summary>
        [Browsable(false)]
        public bool IsSystemCrmEntity
        {
            get { return !IsEditableContract(Contract); }
        }

        public static bool IsEditableContract(CrmServiceEndpointContract contract)
        {
            return contract == CrmServiceEndpointContract.Webhook
                || contract == CrmServiceEndpointContract.EventGrid
                || IsNamespaceContract(contract);
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? ModifiedOn { get; private set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Name
        {
            get;
            set;
        }

        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                List<CrmPluginStep> steps = new List<CrmPluginStep>();

                foreach (CrmPluginStep step in m_org.Steps.Values)
                {
                    if (step.ServiceBusConfigurationId == ServiceEndpointId || step.EventHandler.Id == ServiceEndpointId)
                    {
                        steps.Add(step);
                    }
                }
                return steps.ToList<ICrmTreeNode>();
            }
        }

        [Browsable(false)]
        public Guid NodeId
        {
            get { return ServiceEndpointId; }
        }

        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get { return Contract == CrmServiceEndpointContract.Webhook ? CrmTreeNodeImageType.WebHook : CrmTreeNodeImageType.ServiceEndpoint; }
        }

        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get { return NodeImageType; }
        }

        [Browsable(false)]
        public string NodeText
        {
            get
            {
                return string.Format("({0}) {1}", NodeTypeLabel, string.IsNullOrWhiteSpace(Name) ? Description : Name);
            }
        }

        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                switch (Contract)
                {
                    case CrmServiceEndpointContract.Webhook:
                        return CrmTreeNodeType.WebHook;
                    default:
                        return CrmTreeNodeType.ServiceEndpoint;
                }
            }
        }

        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                switch (Contract)
                {
                    case CrmServiceEndpointContract.Webhook:
                        return "WebHook";
                    default:
                        return "ServiceEndpoint";
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Path
        {
            get;
            set;
        }

        [Browsable(false)]
        public Guid PluginId
        {
            get { return ServiceBusPlugin; }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid ServiceEndpointId
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string SolutionNamespace
        {
            get;
            set;
        }

        [Browsable(false)]
        public CrmEntityDictionary<CrmPluginStep> Steps
        {
            get
            {
                Dictionary<Guid, CrmPluginStep> steps = new Dictionary<Guid, CrmPluginStep>();

                foreach (CrmPluginStep step in m_org.Steps.Values)
                {
                    if (step.ServiceBusConfigurationId == ServiceEndpointId)
                    {
                        steps.Add(step.StepId, step);
                    }
                }
                return new CrmEntityDictionary<CrmPluginStep>(steps);
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmServiceEndpointUserClaim UserClaim
        {
            get;
            set;
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmServiceEndpointAuthType AuthType
        {
            get;
            set;
        }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                Dictionary<string, object> valueList = new Dictionary<string, object>();
                valueList.Add("Id", ServiceEndpointId.ToString());
                valueList.Add("Name", Name);
                valueList.Add("Description", Description);
                valueList.Add("SolutionNamespace", SolutionNamespace);
                valueList.Add("Path", Path);
                valueList.Add("Contract", Contract.ToString());
                valueList.Add("UserClaim", UserClaim.ToString());
                valueList.Add("ConnectionMode", ConnectionMode.ToString());
                valueList.Add("ModifiedOn", ModifiedOn);
                valueList.Add("CreatedOn", CreatedOn);
                valueList.Add("Url", Url);
                valueList.Add("AuthType", AuthType.ToString());
                valueList.Add("NamespaceAddress", NamespaceAddress);
                valueList.Add("MessageFormat", MessageFormat?.ToString());

                return valueList;
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

                    foreach (CrmPluginStep step in m_stepList.Values)
                    {
                        if (step.Organization == null)
                        {
                            step.Organization = m_org;
                        }

                        Organization.AddStep(this, step);
                    }
                }
                else
                {
                    throw new NotSupportedException("Cannot change the Organization once it has been set");
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void AddStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            m_stepList.Add(step.StepId, step);

            if (Organization != null)
            {
                Organization.AddStep(this, step);
            }
        }
        public Dictionary<string, object> GenerateCrmEntities()
        {
            Dictionary<string, object> entityList = new Dictionary<string, object>();

            ServiceEndpoint serviceEndPoint = new ServiceEndpoint();
            if (ServiceEndpointId != Guid.Empty)
            {
                serviceEndPoint.Id = ServiceEndpointId;
            }

            serviceEndPoint.ConnectionMode = new OptionSetValue();
            serviceEndPoint.ConnectionMode.Value = (int)ConnectionMode;

            serviceEndPoint.AuthType = new OptionSetValue();
            serviceEndPoint.AuthType.Value = (int)AuthType;

            serviceEndPoint.Contract = new OptionSetValue();
            serviceEndPoint.Contract.Value = (int)Contract;

            serviceEndPoint.UserClaim = new OptionSetValue((int)UserClaim);

            serviceEndPoint.Description = Description;
            // Dataverse never returns authvalue, so a wrapper loaded from the tree has none. Leave it
            // out of the request then, so an update keeps the saved value instead of clearing it.
            if (AuthValue != null)
            {
                serviceEndPoint.AuthValue = AuthValue;
            }
            serviceEndPoint.Name = Name;
            serviceEndPoint.Path = Path;
            serviceEndPoint.SolutionNamespace = SolutionNamespace;
            serviceEndPoint.Url = Url;

            if (UsesNamespace)
            {
                serviceEndPoint.NamespaceAddress = NamespaceAddress;
                // 2 = Namespace Address: the tool always takes the full sb:// address
                serviceEndPoint.NamespaceFormat = new OptionSetValue(2);
                serviceEndPoint.SASKeyName = SasKeyName;
                // Same as authvalue: only send the secrets when they were entered
                if (SasKey != null)
                {
                    serviceEndPoint.SASKey = SasKey;
                }
                if (SasToken != null)
                {
                    serviceEndPoint.SASToken = SasToken;
                }
            }

            if (MessageFormat.HasValue)
            {
                serviceEndPoint.MessageFormat = new OptionSetValue((int)MessageFormat.Value);
            }

            entityList.Add(ServiceEndpoint.EntityLogicalName, serviceEndPoint);

            return entityList;
        }

        public void RefreshFromServiceEndpoint(ServiceEndpoint serviceEndPoint)
        {
            if (serviceEndPoint == null)
            {
                throw new ArgumentNullException("serviceEndPoint");
            }

            Name = serviceEndPoint.Name;
            Description = serviceEndPoint.Description;
            Path = serviceEndPoint.Path; 
            SolutionNamespace = serviceEndPoint.SolutionNamespace;
            Url = serviceEndPoint.Url;
            AuthValue = serviceEndPoint.AuthValue;
            NamespaceAddress = serviceEndPoint.NamespaceAddress;
            SasKeyName = serviceEndPoint.SASKeyName;
            SasKey = serviceEndPoint.SASKey;
            SasToken = serviceEndPoint.SASToken;
            MessageFormat = serviceEndPoint.MessageFormat == null
                ? (CrmServiceEndpointMessageFormat?)null
                : (CrmServiceEndpointMessageFormat)serviceEndPoint.MessageFormat.Value;

            if (serviceEndPoint.ServiceEndpointId != Guid.Empty)
            {
                ServiceEndpointId = serviceEndPoint.ServiceEndpointId.Value;
            }

            if (serviceEndPoint.CreatedOn != null && (serviceEndPoint.CreatedOn.HasValue))
            {
                CreatedOn = serviceEndPoint.CreatedOn.Value;
            }

            if (serviceEndPoint.ModifiedOn != null && (serviceEndPoint.ModifiedOn.HasValue))
            {
                ModifiedOn = serviceEndPoint.ModifiedOn.Value;
            }

            if (serviceEndPoint.ConnectionMode != null)
            {
                ConnectionMode = (CrmServiceEndpointConnectionMode)Enum.ToObject(typeof(CrmServiceEndpointConnectionMode), serviceEndPoint.ConnectionMode.Value);
            }

            if (serviceEndPoint.AuthType != null)
            {
                AuthType = (CrmServiceEndpointAuthType)Enum.ToObject(typeof(CrmServiceEndpointAuthType), serviceEndPoint.AuthType.Value);
            }

            if (serviceEndPoint.Contract != null)
            {
                Contract = (CrmServiceEndpointContract)Enum.ToObject(typeof(CrmServiceEndpointContract),
                    serviceEndPoint.Contract.Value);
            }
            if (serviceEndPoint.UserClaim != null)
            {
                UserClaim = (CrmServiceEndpointUserClaim)Enum.ToObject(typeof(CrmServiceEndpointUserClaim),
                    serviceEndPoint.UserClaim.Value);
            }
        }

        public void UpdateDates(DateTime? createdOn, DateTime? modifiedOn)
        {
            if (createdOn != null)
            {
                CreatedOn = createdOn;
            }

            if (modifiedOn != null)
            {
                ModifiedOn = modifiedOn;
            }
        }

        public override string ToString()
        {
            return NodeText;
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Shows only the properties that apply to the endpoint's type in the property grid: a Service
    /// Bus endpoint has no URL, a webhook no namespace, path or message format.
    /// </summary>
    internal sealed class CrmServiceEndpointDescriptionProvider : TypeDescriptionProvider
    {
        private static readonly string[] AddressProperties =
        {
            nameof(CrmServiceEndpoint.Url),
            nameof(CrmServiceEndpoint.NamespaceAddress),
            nameof(CrmServiceEndpoint.Path),
            nameof(CrmServiceEndpoint.SasKeyName),
            nameof(CrmServiceEndpoint.SolutionNamespace),
        };

        public CrmServiceEndpointDescriptionProvider()
            : base(TypeDescriptor.GetProvider(typeof(object)))
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            var descriptor = base.GetTypeDescriptor(objectType, instance);
            return instance is CrmServiceEndpoint endpoint ? new Descriptor(descriptor, endpoint) : descriptor;
        }

        internal static bool Applies(CrmServiceEndpoint endpoint, PropertyDescriptor property)
        {
            if (property.Name == nameof(CrmServiceEndpoint.MessageFormat))
            {
                return endpoint.UsesNamespace;
            }

            if (AddressProperties.Contains(property.Name))
            {
                // Dataverse leaves the fields of the other kind of address empty (or a single space)
                return !string.IsNullOrWhiteSpace(property.GetValue(endpoint) as string);
            }

            return true;
        }

        private sealed class Descriptor : CustomTypeDescriptor
        {
            private readonly CrmServiceEndpoint endpoint;

            public Descriptor(ICustomTypeDescriptor parent, CrmServiceEndpoint endpoint)
                : base(parent)
            {
                this.endpoint = endpoint;
            }

            public override PropertyDescriptorCollection GetProperties() => Filter(base.GetProperties());

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => Filter(base.GetProperties(attributes));

            private PropertyDescriptorCollection Filter(PropertyDescriptorCollection properties)
            {
                return new PropertyDescriptorCollection(properties.Cast<PropertyDescriptor>()
                    .Where(property => Applies(endpoint, property)).ToArray(), true);
            }
        }
    }
}
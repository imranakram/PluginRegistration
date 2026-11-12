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
    using Helpers;
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Serialization;

    public interface ICrmEntity
    {
        #region Public Properties

        Guid EntityId { get; }
        string EntityType { get; }
        bool IsSystemCrmEntity { get; }
        Dictionary<string, object> Values { get; }

        #endregion Public Properties

        #region Public Methods

        Dictionary<string, object> GenerateCrmEntities();

        void UpdateDates(DateTime? createdOn, DateTime? modifiedOn);

        #endregion Public Methods
    }

    public interface IWcfAuthenticationHeader
    {
        #region Public Properties

        Guid CallerId { get; set; }
        string CrmTicket { get; set; }
        string OrganizationName { get; }

        #endregion Public Properties
    }

    public class CrmEntityColumn
    {
        #region Private Fields

        private string m_label;
        private string m_name;
        private Type m_type;

        #endregion Private Fields

        #region Public Constructors

        public CrmEntityColumn(string name, string label, Type type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            m_name = name;
            m_label = label;
            m_type = type;
        }

        #endregion Public Constructors

        #region Public Properties

        public string Label
        {
            get
            {
                return m_label;
            }
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public Type Type
        {
            get
            {
                return m_type;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            return Name;
        }

        #endregion Public Methods
    }

    public sealed class CrmOrganization : ICrmTreeNode
    {
        #region Private Fields

        private Dictionary<Guid, CrmPluginAssembly> m_assemblyList = new Dictionary<Guid, CrmPluginAssembly>();
        private CrmEntityDictionary<CrmPluginAssembly> m_assemblyReadOnlyList = null;
        private Exception m_attributeException = null;
        private Dictionary<string, CrmAttribute[]> m_attributeList = new Dictionary<string, CrmAttribute[]>();
        private bool m_connected = false;
        private CrmUser m_currentUser = null;
        private string m_friendlyName = null;
        private Dictionary<Guid, CrmPluginImage> m_imageList = new Dictionary<Guid, CrmPluginImage>();
        private CrmEntityDictionary<CrmPluginImage> m_imageReadOnlyList = null;
        private Dictionary<Guid, ICrmEntity> m_invalidEntityList = new Dictionary<Guid, ICrmEntity>();
        private CrmEntityDictionary<ICrmEntity> m_invalidEntityReadOnlyList = null;
        private Dictionary<Guid, CrmMessageEntity> m_messageEntityList = new Dictionary<Guid, CrmMessageEntity>();
        private CrmEntityDictionary<CrmMessageEntity> m_messageEntityReadOnlyList = null;
        private Dictionary<Guid, CrmMessage> m_messageList = new Dictionary<Guid, CrmMessage>();
        private CrmEntityDictionary<CrmMessage> m_messageReadOnlyList = null;
        private Guid m_organizationId = Guid.Empty;
        private volatile IOrganizationService m_organizationService = null;
        private string m_organizationServiceUrl = null;
        private Dictionary<Guid, CrmPluginPackage> m_packageList = new Dictionary<Guid, CrmPluginPackage>();
        private CrmEntityDictionary<CrmPluginPackage> m_packageReadOnlyList = null;
        private Dictionary<Guid, CrmPlugin> m_pluginList = new Dictionary<Guid, CrmPlugin>();
        private CrmEntityDictionary<CrmPlugin> m_pluginReadOnlyList = null;
        private Version m_serverBuild = null;
        private Dictionary<Guid, CrmServiceEndpoint> m_serviceEndpointList = new Dictionary<Guid, CrmServiceEndpoint>();
        private CrmEntityDictionary<CrmServiceEndpoint> m_serviceEndpointReadOnlyList = null;
        private Dictionary<Guid, CrmPluginStep> m_stepList = new Dictionary<Guid, CrmPluginStep>();
        private CrmEntityDictionary<CrmPluginStep> m_stepReadOnlyList = null;
        private string m_uniqueName = null;
        private Dictionary<Guid, CrmUser> m_userList = new Dictionary<Guid, CrmUser>();
        private Dictionary<Guid, CrmServiceEndpoint> m_webhookList = new Dictionary<Guid, CrmServiceEndpoint>();
        private CrmEntityDictionary<CrmServiceEndpoint> m_webhookReadOnlyList = null;

        #endregion Private Fields

        #region Public Constructors

        public CrmOrganization(ConnectionDetail detail, Settings settings, ProgressIndicator prog, IOrganizationService Service)
        {
            if (detail == null)
            {
                throw new ArgumentNullException("detail");
            }

            OrganizationServiceUrl = detail.OrganizationServiceUrl;
            WebApplicationUrl = detail.WebApplicationUrl;

            OrganizationFriendlyName = detail.OrganizationFriendlyName;
            OrganizationUniqueName = detail.Organization;
            ServerBuild = new Version(detail.OrganizationVersion);
            OrganizationService = Service;
            ConnectionDetail = detail;

            OrganizationHelper.OpenConnection(this, settings, OrganizationHelper.LoadMessages(this), prog);
        }

        #endregion Public Constructors

        #region Public Properties

        [XmlIgnore]
        public CrmEntityDictionary<CrmPluginAssembly> Assemblies
        {
            get
            {
                if (m_assemblyReadOnlyList == null)
                {
                    m_assemblyReadOnlyList = new CrmEntityDictionary<CrmPluginAssembly>(m_assemblyList);
                }

                return m_assemblyReadOnlyList;
            }
        }

        [XmlIgnore]
        public Exception AttributeLoadException
        {
            get
            {
                return m_attributeException;
            }
        }

        [XmlIgnore]
        public bool Connected
        {
            get
            {
                return m_connected;
            }
            set
            {
                if (!value)
                {
                    //Reset when closing the connection
                    SecureConfigurationPermissionDenied = false;
                    m_currentUser = null;
                    m_invalidEntityList.Clear();
                    ProfilerPlugin = null;
                }

                m_connected = value;
            }
        }

        public ConnectionDetail ConnectionDetail { get; set; }

        [XmlIgnore]
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
        public CrmEntityDictionary<ICrmEntity> InvalidEntities
        {
            get
            {
                if (m_invalidEntityReadOnlyList == null)
                {
                    m_invalidEntityReadOnlyList = new CrmEntityDictionary<ICrmEntity>(m_invalidEntityList);
                }

                return m_invalidEntityReadOnlyList;
            }
        }

        [XmlIgnore]
        public CrmUser LoggedOnUser
        {
            get
            {
                return m_currentUser;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                else
                {
                    m_currentUser = value;
                }
            }
        }

        [XmlIgnore]
        public CrmEntityDictionary<CrmMessageEntity> MessageEntities
        {
            get
            {
                if (m_messageEntityReadOnlyList == null)
                {
                    m_messageEntityReadOnlyList = new CrmEntityDictionary<CrmMessageEntity>(m_messageEntityList);
                }

                return m_messageEntityReadOnlyList;
            }
        }

        [XmlIgnore]
        public CrmEntityDictionary<CrmMessage> Messages
        {
            get
            {
                if (m_messageReadOnlyList == null)
                {
                    m_messageReadOnlyList = new CrmEntityDictionary<CrmMessage>(m_messageList);
                }

                return m_messageReadOnlyList;
            }
        }

        [XmlIgnore]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                return new List<ICrmTreeNode> { };
            }
        }

        [XmlIgnore]
        public Guid NodeId
        {
            get
            {
                return m_organizationId;
            }
        }

        [XmlIgnore]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                return CrmTreeNodeImageType.Organization;
            }
        }

        [XmlIgnore]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                return CrmTreeNodeImageType.OrganizationSelected;
            }
        }

        [XmlIgnore]
        public string NodeText
        {
            get
            {
                return m_friendlyName;
            }
        }

        [XmlIgnore]
        public CrmTreeNodeType NodeType
        {
            get
            {
                return CrmTreeNodeType.Organization;
            }
        }

        [XmlIgnore]
        public string NodeTypeLabel
        {
            get
            {
                return "Organization";
            }
        }

        [XmlElement(ElementName = "OrganizationFriendlyName", Type = typeof(string), IsNullable = false)]
        public string OrganizationFriendlyName
        {
            get
            {
                return m_friendlyName;
            }
            set
            {
                m_friendlyName = value;
            }
        }

        [XmlElement(ElementName = "OrganizationId", IsNullable = false)]
        public Guid OrganizationId
        {
            get
            {
                return m_organizationId;
            }
            set
            {
                m_organizationId = value;
            }
        }

        [XmlElement(ElementName = "CrmServiceUrl", Type = typeof(string), IsNullable = false)]
        public string OrganizationServiceUrl
        {
            get
            {
                return m_organizationServiceUrl;
            }
            set
            {
                m_organizationServiceUrl = value;
                m_organizationService = null;
                m_connected = false;
            }
        }

        [XmlElement(ElementName = "OrganizationUniqueName", Type = typeof(string), IsNullable = false)]
        public string OrganizationUniqueName
        {
            get
            {
                return m_uniqueName;
            }
            set
            {
                m_uniqueName = value;
            }
        }

        /// <summary>
        /// True when plug-in assemblies and packages can be linked to a managed identity
        /// (online organizations that have the managedidentity table).
        /// </summary>
        [XmlIgnore]
        public bool ManagedIdentitySupported { get; internal set; }

        [XmlIgnore]
        public CrmEntityDictionary<CrmPluginPackage> Packages
        {
            get
            {
                if (m_packageReadOnlyList == null)
                {
                    m_packageReadOnlyList = new CrmEntityDictionary<CrmPluginPackage>(m_packageList);
                }

                return m_packageReadOnlyList;
            }
        }

        [XmlIgnore]
        public CrmEntityDictionary<CrmPlugin> Plugins
        {
            get
            {
                if (m_pluginReadOnlyList == null)
                {
                    m_pluginReadOnlyList = new CrmEntityDictionary<CrmPlugin>(m_pluginList);
                }

                return m_pluginReadOnlyList;
            }
        }

        [XmlIgnore]
        public CrmPlugin ProfilerPlugin { get; set; }

        [XmlIgnore]
        public bool SecureConfigurationPermissionDenied { get; set; }

        [XmlIgnore]
        public Version ServerBuild
        {
            get
            {
                return m_serverBuild;
            }

            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException("value");
                }
                else
                {
                    m_serverBuild = value;
                }
            }
        }

        [XmlIgnore]
        public CrmEntityDictionary<CrmServiceEndpoint> ServiceEndpoints
        {
            get
            {
                if (m_serviceEndpointReadOnlyList == null)
                {
                    m_serviceEndpointReadOnlyList = new CrmEntityDictionary<CrmServiceEndpoint>(m_serviceEndpointList);
                }

                return m_serviceEndpointReadOnlyList;
            }
        }

        [XmlIgnore]
        public CrmEntityDictionary<CrmPluginStep> Steps
        {
            get
            {
                if (m_stepReadOnlyList == null)
                {
                    m_stepReadOnlyList = new CrmEntityDictionary<CrmPluginStep>(m_stepList);
                }

                return m_stepReadOnlyList;
            }
        }

        [XmlIgnore]
        public Dictionary<Guid, CrmUser> Users
        {
            get
            {
                return m_userList;
            }
        }

        [XmlIgnore]
        public string WebApplicationUrl { get; set; }

        [XmlIgnore]
        public CrmEntityDictionary<CrmServiceEndpoint> Webhooks
        {
            get
            {
                if (m_webhookReadOnlyList == null)
                {
                    m_webhookReadOnlyList = new CrmEntityDictionary<CrmServiceEndpoint>(m_webhookList);
                }

                return m_webhookReadOnlyList;
            }
        }

        #endregion Public Properties

        #region Internal Properties

        [XmlIgnore]
        internal IOrganizationService OrganizationService
        {
            get
            {
                lock (this)
                {
                    if (null == m_organizationService)
                    {
                        InitializeOrganizationService();
                    }

                    return m_organizationService;
                }
            }
            private set
            {
                m_organizationService = value;
            }
        }

        #endregion Internal Properties

        #region Public Indexers

        [XmlIgnore]
        public CrmPluginAssembly this[Guid assemblyId]
        {
            get
            {
                if (Assemblies != null)
                {
                    return Assemblies[assemblyId];
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion Public Indexers

        #region Public Methods

        public void AddAssembly(CrmPluginPackage package, CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            else if (!m_assemblyList.ContainsKey(assembly.AssemblyId))
            {
                ValidateEntity(assembly);

                if (!package.Assemblies.ContainsKey(assembly.AssemblyId))
                {
                    package.AddPluginAssembly(assembly);
                }

                m_assemblyList.Add(assembly.AssemblyId, assembly);
                var assemblies = m_assemblyList.Values.Where(a => a.Name == assembly.Name);
                if (assemblies.Any(a => a.AssemblyId != assembly.AssemblyId)) //multiple versions
                {
                    foreach (var c_Assembly in assemblies)
                    {
                        if (c_Assembly.MultipleVersions) continue;
                        c_Assembly.MultipleVersions = true;
                        foreach (var plugin in c_Assembly.Plugins.Values)
                        {
                            plugin.AssemblyVersion = c_Assembly.Version;
                        }
                    }
                }
            }
        }

        public void AddAssembly(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            else if (!m_assemblyList.ContainsKey(assembly.AssemblyId))
            {
                ValidateEntity(assembly);

                m_assemblyList.Add(assembly.AssemblyId, assembly);
                var assemblies = m_assemblyList.Values.Where(a => a.Name == assembly.Name);
                if (assemblies.Any(a => a.AssemblyId != assembly.AssemblyId)) //multiple versions
                {
                    foreach (var c_Assembly in assemblies)
                    {
                        if (c_Assembly.MultipleVersions) continue;
                        c_Assembly.MultipleVersions = true;
                        foreach (var plugin in c_Assembly.Plugins.Values)
                        {
                            plugin.AssemblyVersion = c_Assembly.Version;
                        }
                    }
                }
            }
        }

        public void AddImage(CrmPluginStep step, CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }
            else if (step == null)
            {
                throw new ArgumentNullException("step");
            }
            else if (!step.Images.ContainsKey(image.ImageId))
            {
                step.AddImage(image);
                return;
            }
            else
            {
                ValidateEntity(image);
            }

            m_imageList.Add(image.ImageId, image);
        }

        public void AddMessage(CrmMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            else if (m_messageList.ContainsKey(message.MessageId))
            {
                throw new ArgumentException("Message is already in the list");
            }
            else
            {
                ValidateEntity(message);
            }

            m_messageList.Add(message.MessageId, message);
        }

        public void AddMessageEntity(CrmMessage message, CrmMessageEntity entity)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            else if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            else if (!message.MessageEntities.ContainsKey(entity.MessageEntityId))
            {
                message.AddMessageEntity(entity);
                return;
            }
            else
            {
                ValidateEntity(entity);
            }

            m_messageEntityList.Add(entity.EntityId, entity);
        }

        public void AddPackage(CrmPluginPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }
            else if (m_packageList.ContainsKey(package.PackageId))
            {
                throw new ArgumentException("Package is already in the list");
            }
            else
            {
                ValidateEntity(package);
            }

            m_packageList.Add(package.PackageId, package);
        }

        public void AddPlugin(CrmPluginAssembly assembly, CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }
            else if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            else if (!assembly.Plugins.ContainsKey(plugin.PluginId))
            {
                assembly.AddPlugin(plugin);
                return;
            }
            else
            {
                ValidateEntity(plugin);
            }

            m_pluginList.Add(plugin.PluginId, plugin);
        }

        public void AddServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }
            if (m_serviceEndpointList.ContainsKey(serviceEndpoint.ServiceEndpointId))
            {
                throw new ArgumentException("ServiceEndpoint is already in the list");
            }

            m_serviceEndpointList.Add(serviceEndpoint.ServiceEndpointId, serviceEndpoint);
        }

        public void AddStep(ICrmEntity plugin, CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (plugin is CrmPlugin crmPlugin)
            {
                if (!crmPlugin.Steps.ContainsKey(step.StepId))
                {
                    crmPlugin.AddStep(step);
                    return;
                }
            }

            //if (plugin is CrmServiceEndpoint crmServiceEndpoint)
            //{
            //    if (!crmServiceEndpoint.Steps.ContainsKey(step.StepId))
            //    {
            //        crmServiceEndpoint.AddStep(step);
            //        return;
            //    }
            //}

            ValidateEntity(step);

            m_stepList.Add(step.StepId, step);
        }

        public void AddWebhook(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }
            if (m_webhookList.ContainsKey(serviceEndpoint.ServiceEndpointId))
            {
                throw new ArgumentException("ServiceEndpoint is already in the list");
            }

            m_webhookList.Add(serviceEndpoint.ServiceEndpointId, serviceEndpoint);
        }

        /// <summary>
        /// Clears all saved entity attributes
        /// </summary>
        public void ClearAllEntityAttributes()
        {
            m_attributeList.Clear();
            m_attributeException = null;
        }

        public void ClearAssemblies()
        {
            m_imageList.Clear();
            m_stepList.Clear();
            m_pluginList.Clear();
            m_assemblyList.Clear();
        }

        public void ClearImages()
        {
            ClearImages(Guid.Empty);
        }

        public void ClearImages(Guid stepId)
        {
            CrmPluginImage[] imageList;
            if (stepId == Guid.Empty)
            {
                //Copy the list of images. Can't use the enumerator because we are changing the underlying data
                imageList = new CrmPluginImage[m_imageList.Count];
                m_imageList.Values.CopyTo(imageList, 0);
            }
            else if (m_stepList.ContainsKey(stepId))
            {
                //Copy the list of images. Can't use the enumerator because we are changing the underlying data
                imageList = m_stepList[stepId].Images.ToArray();
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "stepId");
            }

            //Loop through the image id's
            foreach (CrmPluginImage image in imageList)
            {
                RemoveImage(image.StepId, image.ImageId);
            }
        }

        public void ClearMessageEntities()
        {
            ClearMessageEntities(Guid.Empty);
        }

        public void ClearMessageEntities(Guid messageId)
        {
            CrmMessageEntity[] entityList;
            if (messageId == Guid.Empty)
            {
                //Copy the list of entities. Can't use the enumerator because we are changing the underlying data
                entityList = new CrmMessageEntity[m_messageEntityList.Count];
                m_messageEntityList.Values.CopyTo(entityList, 0);
            }
            else if (m_messageList.ContainsKey(messageId))
            {
                //Copy the list of entities. Can't use the enumerator because we are changing the underlying data
                entityList = m_messageList[messageId].MessageEntities.ToArray();
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "messageId");
            }

            //Loop through the entity id's
            foreach (CrmMessageEntity message in entityList)
            {
                RemoveMessageEntity(message.MessageId, message.MessageEntityId);
            }
        }

        public void ClearMessages()
        {
            m_messageEntityList.Clear();
            m_messageList.Clear();
        }

        public void ClearPackages()
        {
            m_imageList.Clear();
            m_stepList.Clear();
            m_pluginList.Clear();
            m_assemblyList.Clear();
            m_packageList.Clear();
        }

        public void ClearPlugins()
        {
            ClearPlugins(Guid.Empty);
        }

        public void ClearPlugins(Guid assemblyId)
        {
            CrmPlugin[] pluginList;
            if (assemblyId == Guid.Empty)
            {
                m_imageList.Clear();
                m_stepList.Clear();

                //Copy the list of plugins. Can't use the enumerator because we are changing the underlying data
                pluginList = new CrmPlugin[m_pluginList.Count];
                m_pluginList.Values.CopyTo(pluginList, 0);
            }
            else if (m_assemblyList.ContainsKey(assemblyId))
            {
                //Copy the list of plugins. Can't use the enumerator because we are changing the underlying data
                pluginList = m_assemblyList[assemblyId].Plugins.ToArray();
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "assemblyId");
            }

            //Loop through the plugin id's
            foreach (CrmPlugin plugin in pluginList)
            {
                RemovePlugin(plugin.AssemblyId, plugin.PluginId);
            }
        }

        public void ClearServiceEndpoints()
        {
            m_serviceEndpointList.Clear();
        }

        public void ClearSteps()
        {
            ClearSteps(Guid.Empty);
        }

        public void ClearSteps(Guid pluginId)
        {
            CrmPluginStep[] stepList;
            if (pluginId == Guid.Empty)
            {
                m_imageList.Clear();

                //Copy the list of steps. Can't use the enumerator because we are changing the underlying data
                stepList = new CrmPluginStep[m_stepList.Count];
                m_stepList.Values.CopyTo(stepList, 0);
            }
            else if (m_pluginList.ContainsKey(pluginId))
            {
                //Copy the list of steps. Can't use the enumerator because we are changing the underlying data
                stepList = m_pluginList[pluginId].Steps.ToArray();
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "pluginId");
            }

            //Loop through the step id's
            foreach (CrmPluginStep step in stepList)
            {
                RemoveStep(step.PluginId, step.StepId);
            }
        }

        public void ClearWebhooks()
        {
            m_webhookList.Clear();
        }

        /// <summary>
        /// Locate the message based on the name of the message
        /// </summary>
        /// <param name="messageName">Message to be found</param>
        /// <returns>CrmMessage if found, null if not</returns>
        public CrmMessage FindMessage(string messageName)
        {
            if (messageName == null)
            {
                throw new ArgumentNullException("message");
            }

            foreach (CrmMessage msg in m_messageList.Values)
            {
                if (messageName.Equals(msg.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return msg;
                }
            }

            return null;
        }

        /// <summary>
        /// Method that initializes needed variables. This can be done in a thread
        /// </summary>
        public void Initialize()
        {
            lock (this)
            {
                if (m_organizationService == null)
                {
                    InitializeOrganizationService();
                }
            }
        }

        /// <summary>
        /// Determines if the attributes for a specific entity have been loaded
        /// </summary>
        public bool IsEntityAttributesLoaded(string entityName)
        {
            return m_attributeList.ContainsKey(entityName);
        }

        public void RemoveAssembly(Guid assemblyId)
        {
            if (m_assemblyList.ContainsKey(assemblyId))
            {
                CrmPluginAssembly assembly = m_assemblyList[assemblyId];

                //Copy the list of plugins. Can't use the enumerator because we are changing the underlying data
                Guid[] pluginIdList = new Guid[assembly.Plugins.Count];
                assembly.Plugins.Keys.CopyTo(pluginIdList, 0);

                //Loop through the plugin id's
                foreach (Guid pluginId in pluginIdList)
                {
                    RemovePlugin(assembly, pluginId);
                }

                m_assemblyList.Remove(assemblyId);
            }
            else
            {
                throw new ArgumentException("Invalid Assembly Id", "assemblyId");
            }
        }

        public void RemoveImage(Guid stepId, Guid imageId)
        {
            if (m_stepList.ContainsKey(stepId))
            {
                RemoveImage(m_stepList[stepId], imageId);
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "stepId");
            }
        }

        public void RemoveImage(CrmPluginStep step, Guid imageId)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }
            else if (step.Images.ContainsKey(imageId))
            {
                step.RemoveImage(imageId);
            }
            else if (m_imageList.ContainsKey(imageId))
            {
                m_imageList.Remove(imageId);
            }
            else
            {
                throw new ArgumentException("Image is not in list", "step");
            }
        }

        public void RemoveMessage(Guid messageId)
        {
            if (m_messageList.ContainsKey(messageId))
            {
                CrmMessage message = m_messageList[messageId];

                //Copy the list of MessageEntities. Can't use the enumerator because we are changing the underlying data
                Guid[] entityIdList = new Guid[message.MessageEntities.Count];
                message.MessageEntities.Keys.CopyTo(entityIdList, 0);

                //Loop through the MessageEntity id's
                foreach (Guid messageEntityId in entityIdList)
                {
                    RemoveMessageEntity(message, messageEntityId);
                }

                m_messageList.Remove(messageId);
            }
            else
            {
                throw new ArgumentException("Invalid Message Id", "messageId");
            }
        }

        public void RemoveMessageEntity(Guid messageId, Guid messageEntityId)
        {
            if (m_messageList.ContainsKey(messageId))
            {
                RemoveMessageEntity(m_messageList[messageId], messageEntityId);
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "messageId");
            }
        }

        public void RemoveMessageEntity(CrmMessage message, Guid messageEntityId)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            else if (message.MessageEntities.ContainsKey(messageEntityId))
            {
                message.RemoveMessageEntity(messageEntityId);
            }
            else if (m_messageEntityList.ContainsKey(messageEntityId))
            {
                m_messageEntityList.Remove(messageEntityId);
            }
            else
            {
                throw new ArgumentException("MessageEntity is not in list", "messageEntityId");
            }
        }

        public void RemovePackage(Guid packageId)
        {
            if (m_assemblyList.ContainsKey(packageId))
            {
                CrmPluginPackage package = m_packageList[packageId];

                //Copy the list of plugins. Can't use the enumerator because we are changing the underlying data
                Guid[] assemblyIdList = new Guid[package.Assemblies.Count];
                package.Assemblies.Keys.CopyTo(assemblyIdList, 0);

                //Loop through the plugin id's
                foreach (Guid assemblyId in assemblyIdList)
                {
                    RemoveAssembly(assemblyId);
                }

                m_assemblyList.Remove(packageId);
            }
            else
            {
                throw new ArgumentException("Invalid Package Id", "packageId");
            }
        }

        public void RemovePlugin(Guid assemblyId, Guid pluginId)
        {
            if (m_assemblyList.ContainsKey(assemblyId))
            {
                RemovePlugin(m_assemblyList[assemblyId], pluginId);
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "assemblyId");
            }
        }

        public void RemovePlugin(CrmPluginAssembly assembly, Guid pluginId)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            else if (assembly.Plugins.ContainsKey(pluginId))
            {
                assembly.RemovePlugin(pluginId);
            }
            else if (m_pluginList.ContainsKey(pluginId))
            {
                CrmPlugin plugin = m_pluginList[pluginId];

                //Copy the list of steps. Can't use the enumerator because we are changing the underlying data
                Guid[] stepIdList = new Guid[plugin.Steps.Count];
                plugin.Steps.Keys.CopyTo(stepIdList, 0);

                //Loop through the step id's
                foreach (Guid stepId in stepIdList)
                {
                    RemoveStep(plugin, stepId);
                }

                m_pluginList.Remove(pluginId);
            }
            else
            {
                throw new ArgumentException("Plugin is not in list", "pluginId");
            }
        }

        public void RemoveServiceEndpoint(Guid serviceEndpointId)
        {
            if (m_serviceEndpointList.ContainsKey(serviceEndpointId))
            {
                m_serviceEndpointList.Remove(serviceEndpointId);
            }
            else
            {
                throw new ArgumentException("Invalid ServiceEndpoint Id", "serviceEndpointId");
            }
        }

        public void RemoveStep(Guid pluginId, Guid stepId)
        {
            if (m_pluginList.ContainsKey(pluginId))
            {
                RemoveStep(m_pluginList[pluginId], stepId);
            }
            else
            {
                throw new ArgumentException("Invalid Entity Id", "pluginId");
            }
        }

        public void RemoveStep(CrmPlugin plugin, Guid stepId)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }
            else if (plugin.Steps.ContainsKey(stepId))
            {
                plugin.RemoveStep(stepId);
            }
            else if (m_stepList.ContainsKey(stepId))
            {
                CrmPluginStep step = m_stepList[stepId];

                //Copy the list of images. Can't use the enumerator because we are changing the underlying data
                Guid[] imageIdList = new Guid[step.Images.Count];
                step.Images.Keys.CopyTo(imageIdList, 0);

                //Loop through the image id's
                foreach (Guid imageId in imageIdList)
                {
                    RemoveImage(step, imageId);
                }

                m_stepList.Remove(stepId);
            }
            else
            {
                throw new ArgumentException("Step is not in list", "stepId");
            }
        }

        /// <summary>
        /// Retrieves the entity attributes for the specified entity
        /// </summary>
        public CrmAttribute[] RetrieveEntityAttributes(string entityName)
        {
            if (m_attributeList.ContainsKey(entityName))
            {
                return m_attributeList[entityName];
            }
            else
            {
                throw new ArgumentException("Entity is not loaded", "entityName");
            }
        }

        public void SaveEntityAttributes(string entityName, Exception loadException)
        {
            if (loadException == null)
            {
                throw new ArgumentNullException("loadException");
            }
            else if (entityName == null)
            {
                throw new ArgumentNullException("entityName");
            }
            else if (m_attributeList.ContainsKey(entityName))
            {
                m_attributeList.Remove(entityName);
            }

            m_attributeException = loadException;
        }

        /// <summary>
        /// Save a specific list of attributes for a specific entity
        /// </summary>
        public void SaveEntityAttributes(string entityName, CrmAttribute[] attributes)
        {
            if (entityName == null)
            {
                throw new ArgumentNullException("entityName");
            }
            else if (attributes == null)
            {
                throw new ArgumentNullException("attributes");
            }

            if (m_attributeList.ContainsKey(entityName))
            {
                m_attributeList[entityName] = attributes;
            }
            else
            {
                m_attributeList.Add(entityName, attributes);
            }

            m_attributeException = null;
        }

        public override string ToString()
        {
            return NodeText;
        }

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Initialize the CrmService
        /// </summary>
        private void InitializeOrganizationService()
        {
            lock (this)
            {
                //var crmServiceClient = ConnectionDetail.ServiceClient;//GetCrmServiceClient();
                m_organizationService = ConnectionDetail.ServiceClient;//(crmServiceClient.OrganizationWebProxyClient != null)? (IOrganizationService)crmServiceClient.OrganizationWebProxyClient: crmServiceClient.OrganizationServiceProxy;
            }
        }

        private void ValidateEntity(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_assemblyList.ContainsKey(assembly.AssemblyId))
            {
                throw new ArgumentException("Assembly is already in the list");
            }
            else if (assembly.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (assembly.AssemblyId == Guid.Empty)
            {
                throw new ArgumentException("AssemblyId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmPluginPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_packageList.ContainsKey(package.PackageId))
            {
                throw new ArgumentException("Package is already in the list");
            }
            else if (package.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (package.PackageId == Guid.Empty)
            {
                throw new ArgumentException("PackageId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_pluginList.ContainsKey(plugin.PluginId))
            {
                throw new ArgumentException("Plugin is already in the list");
            }
            else if (plugin.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (plugin.AssemblyId == Guid.Empty)
            {
                throw new ArgumentException("AssemblyId must be set to valid Guid");
            }
            else if (plugin.PluginId == Guid.Empty)
            {
                throw new ArgumentException("PluginId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_stepList.ContainsKey(step.StepId))
            {
                throw new ArgumentException("Step is already in the list");
            }
            else if (step.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (step.AssemblyId == Guid.Empty)
            {
                throw new ArgumentException("AssemblyId must be set to valid Guid");
            }
            else if (step.PluginId == Guid.Empty)
            {
                throw new ArgumentException("PluginId must be set to valid Guid");
            }
            else if (step.StepId == Guid.Empty)
            {
                throw new ArgumentException("StepId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_imageList.ContainsKey(image.ImageId))
            {
                throw new ArgumentException("Image is already in the list");
            }
            else if (image.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (image.AssemblyId == Guid.Empty)
            {
                throw new ArgumentException("AssemblyId must be set to valid Guid");
            }
            else if (image.PluginId == Guid.Empty)
            {
                throw new ArgumentException("PluginId must be set to valid Guid");
            }
            else if (image.StepId == Guid.Empty)
            {
                throw new ArgumentException("StepId must be set to valid Guid");
            }
            else if (image.ImageId == Guid.Empty)
            {
                throw new ArgumentException("ImageId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_messageList.ContainsKey(message.MessageId))
            {
                throw new ArgumentException("Message is already in the list");
            }
            else if (message.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (message.MessageId == Guid.Empty)
            {
                throw new ArgumentException("MessageId must be set to valid Guid");
            }
        }

        private void ValidateEntity(CrmMessageEntity messageEntity)
        {
            if (messageEntity == null)
            {
                throw new ArgumentNullException();
            }
            else if (m_messageEntityList.ContainsKey(messageEntity.MessageEntityId))
            {
                throw new ArgumentException("MessageEntity is already in the list");
            }
            else if (messageEntity.Organization != this)
            {
                throw new ArgumentException("Organization must match");
            }
            else if (messageEntity.MessageId == Guid.Empty)
            {
                throw new ArgumentException("MessageId must be set to valid Guid");
            }
            else if (messageEntity.MessageEntityId == Guid.Empty)
            {
                throw new ArgumentException("MessageEntityId must be set to valid Guid");
            }
        }

        #endregion Private Methods
    }
}
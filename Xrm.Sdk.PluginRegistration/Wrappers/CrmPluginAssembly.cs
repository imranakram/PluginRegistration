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
    using Helpers;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;

    public enum CrmAssemblyIsolationMode
    {
        [Description("None")]
        None = 1,

        [Description("Sandbox")]
        Sandbox = 2
    }

    public enum CrmAssemblySourceType
    {
        [Description("Database")]
        Database = 0,

        [Description("Disk")]
        Disk = 1,

        [Description("GAC")]
        GAC = 2,

        [Description("Package")]
        Package = 4
    }

    [Serializable]
    public sealed class CrmPluginAssembly : ICrmEntity, ICrmTreeNode, ICloneable
    {
        #region Public Fields

        public const string RelationshipAssemblyToType = "pluginassembly_plugintype";

        #endregion Public Fields

        #region Private Fields

        private static CrmEntityColumn[] m_entityColumns = null;
        private Guid m_assemblyId = Guid.Empty;
        private int m_customizationLevel;
        private string m_Name = null;
        private CrmOrganization m_org = null;
        private Guid m_packageId = Guid.Empty;
        private Dictionary<Guid, CrmPlugin> m_pluginList = new Dictionary<Guid, CrmPlugin>();
        private CrmEntityDictionary<CrmPlugin> m_pluginReadOnlyList = null;

        #endregion Private Fields

        #region Public Constructors

        public CrmPluginAssembly(CrmOrganization org)
        {
            m_org = org;
            CustomizationLevel = 1;
        }

        public CrmPluginAssembly(CrmOrganization org, Guid id, string name, DateTime? createdOn, DateTime? modifiedOn,
            CrmAssemblySourceType type, CrmAssemblyIsolationMode isolationMode, string path, string version,
            string publicKeyToken, string culture, int customizationLevel, bool enabled)
            : this(org)
        {
            AssemblyId = id;
            Name = name;
            SourceType = type;
            ServerFileName = path;
            Version = version;
            PublicKeyToken = publicKeyToken;
            Culture = culture;
            CustomizationLevel = customizationLevel;
            Enabled = enabled;
            UpdateDates(createdOn, modifiedOn);
        }

        public CrmPluginAssembly(CrmOrganization org, PluginAssembly assembly)
            : this(org)
        {
            RefreshFromPluginAssembly(assembly);
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
                        new CrmEntityColumn("Name", "Name", typeof(string)),
                        new CrmEntityColumn("Description", "Description", typeof(string)),
                        new CrmEntityColumn("ModifiedOn", "Modified On", typeof(string)),
                        new CrmEntityColumn("SourceType", "Source", typeof(string)),
                        new CrmEntityColumn("Version", "Version", typeof(string)),
                        new CrmEntityColumn("Path", "Path", typeof(string)),
                        new CrmEntityColumn("PublicKeyToken", "Public Key Token", typeof(string)),
                        new CrmEntityColumn("Culture", "Culture", typeof(string)),
                        new CrmEntityColumn("IsolationMode", "Isolation Mode", typeof(string)),
                        new CrmEntityColumn("Enabled", "Enabled", typeof(bool)),
                        new CrmEntityColumn("Id", "AssemblyId", typeof(Guid)),
                        new CrmEntityColumn("PackageId", "PackageId", typeof(Guid)),
                        };
                }

                return m_entityColumns;
            }
        }

        [Category("Information"), Browsable(true), Description("Unique identifier of Assembly"), ReadOnly(true)]
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

                if (m_pluginList != null)
                {
                    foreach (CrmPlugin plugin in m_pluginList.Values)
                    {
                        plugin.AssemblyId = value;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the Created On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? CreatedOn { get; private set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Culture { get; set; }

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

        [Category("Editable"), Browsable(true), Description("Description of the Assembly"), ReadOnly(false)]
        public string Description { get; set; }

        [Browsable(false)]
        public bool Enabled { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public Guid EntityId
        {
            get
            {
                return AssemblyId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string EntityType
        {
            get
            {
                return PluginAssembly.EntityLogicalName;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmAssemblyIsolationMode IsolationMode { get; set; }

        [Browsable(false)]
        public bool IsProfilerAssembly { get; set; }

        [XmlIgnore]
        public bool IsSystemCrmEntity
        {
            get
            {
                return CustomizationLevel == 0;
            }
        }

        /// <summary>
        /// Retrieves the Modified On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? ModifiedOn { get; private set; }

        [XmlIgnore]
        [Browsable(false)]
        public bool MultipleVersions { get; set; }

        [Category("Information"), Browsable(true), Description("Name of the Assembly"), ReadOnly(true)]
        public string Name
        {
            get
            {
                return m_Name;
            }
            set
            {
                if (value == m_Name)
                {
                    return;
                }

                m_Name = value;

                if (m_pluginList != null)
                {
                    foreach (CrmPlugin plugin in m_pluginList.Values)
                    {
                        plugin.AssemblyName = value;
                    }
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                if (m_pluginList == null || m_pluginList.Count == 0)
                {
                    //return new CrmPlugin[0].ToList<ICrmTreeNode>();
                    return new List<ICrmTreeNode> { };
                }
                else
                {
                    var children = new CrmPlugin[m_pluginList.Count];
                    m_pluginList.Values.CopyTo(children, 0);
                    return children.ToList<ICrmTreeNode>();
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid NodeId
        {
            get
            {
                return AssemblyId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                return CrmTreeNodeImageType.Assembly;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                return CrmTreeNodeImageType.AssemblySelected;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeText
        {
            get
            {
                return string.Format("({0}) {1}{2}", NodeTypeLabel, Name, MultipleVersions ? $" [{Version}]" : null);
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                return CrmTreeNodeType.Assembly;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                return "Assembly";
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
                    foreach (CrmPlugin plugin in m_pluginList.Values)
                    {
                        if (plugin.Organization == null)
                        {
                            plugin.Organization = m_org;
                        }
                        if (MultipleVersions)
                        {
                            plugin.AssemblyVersion = Version;
                        }
                        m_org.AddPlugin(this, plugin);
                    }
                }
                else
                {
                    throw new NotSupportedException("Cannot change the Organization once it has been set");
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid PackageId
        {
            get
            {
                return m_packageId;
            }
            set
            {
                if (value == m_packageId)
                {
                    return;
                }

                m_packageId = value;
            }
        }

        [Browsable(false)]
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

        /// <summary>
        /// Managed identity linked to this assembly. Only shown for online organizations.
        /// </summary>
        [Category("Managed Identity"), Browsable(true), ReadOnly(true), OnlineOnly(true), DisplayName("Managed Identity")]
        public string ManagedIdentityName { get; private set; }

        [Category("Managed Identity"), Browsable(true), ReadOnly(true), OnlineOnly(true), DisplayName("Managed Identity Id")]
        public Guid? ManagedIdentityId { get; private set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string PublicKeyToken { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string ServerFileName { get; set; }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmAssemblySourceType SourceType { get; set; }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                var valueList = new Dictionary<string, object>
                {
                    { "Id", AssemblyId },
                    { "Description", String.IsNullOrEmpty(Description) ? string.Empty : Description },
                    { "Name", Name },
                    {
                        "ModifiedOn",
                        (ModifiedOn.HasValue ? ModifiedOn.ToString() : "")
                    },
                    { "SourceType", SourceType.ToString() },
                    { "Version", ConvertNullStringToEmpty(Version) },
                    { "Path", ConvertNullStringToEmpty(ServerFileName) },
                    { "PublicKeyToken", ConvertNullStringToEmpty(PublicKeyToken) },
                    { "Culture", ConvertNullStringToEmpty(Culture) }
                };

                if (CrmAssemblyIsolationMode.Sandbox == IsolationMode)
                {
                    valueList.Add("IsolationMode", "Sandbox");
                }
                else
                {
                    valueList.Add("IsolationMode", "None");
                }

                valueList.Add("Enabled", Enabled);

                return valueList;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Version { get; set; }

        #endregion Public Properties

        #region Public Indexers

        [Browsable(false)]
        public CrmPlugin this[Guid pluginId]
        {
            get
            {
                return m_pluginList[pluginId];
            }
        }

        #endregion Public Indexers

        #region Public Methods

        public void AddPlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (MultipleVersions)
            {
                plugin.AssemblyVersion = Version;
            }

            m_pluginList.Add(plugin.PluginId, plugin);

            if (m_org != null)
            {
                Organization.AddPlugin(this, plugin);
            }
        }

        public void ClearPlugins()
        {
            m_pluginList.Clear();

            if (m_org != null)
            {
                Organization.ClearPlugins(AssemblyId);
            }
        }

        public object Clone()
        {
            return Clone(true);
        }

        public CrmPluginAssembly Clone(bool includeOrganization)
        {
            CrmPluginAssembly newAssembly;
            if (includeOrganization)
            {
                newAssembly = new CrmPluginAssembly(m_org);
            }
            else
            {
                newAssembly = new CrmPluginAssembly(null);
            }

            newAssembly.m_assemblyId = m_assemblyId;
            newAssembly.m_Name = m_Name;
            newAssembly.CreatedOn = CreatedOn;
            newAssembly.Culture = Culture;
            newAssembly.CustomizationLevel = m_customizationLevel;
            newAssembly.Enabled = Enabled;
            newAssembly.IsolationMode = IsolationMode;
            newAssembly.ModifiedOn = ModifiedOn;
            newAssembly.ServerFileName = ServerFileName;
            newAssembly.PublicKeyToken = PublicKeyToken;
            newAssembly.SourceType = SourceType;
            newAssembly.Version = Version;
            newAssembly.ManagedIdentityId = ManagedIdentityId;
            newAssembly.ManagedIdentityName = ManagedIdentityName;

            //Create a new plugin list
            var newPluginList = new Dictionary<Guid, CrmPlugin>();
            foreach (CrmPlugin plugin in m_pluginList.Values)
            {
                //Clone the plugin
                var clonedPlugin = plugin.Clone(includeOrganization);

                //Add the plugin to the new list
                newPluginList.Add(clonedPlugin.PluginId, clonedPlugin);
            }

            //Assign the list to the new object
            newAssembly.m_pluginList = newPluginList;

            return newAssembly;
        }

        public Dictionary<string, object> GenerateCrmEntities()
        {
            var entityList = new Dictionary<string, object>();

            PluginAssembly assembly = new PluginAssembly();
            if (AssemblyId != Guid.Empty)
            {
                assembly.PluginAssemblyId = new Guid?();
                assembly["pluginassemblyid"] = AssemblyId;
            }

            assembly.SourceType = new OptionSetValue
            {
                Value = (int)SourceType
            };

            assembly.IsolationMode = new OptionSetValue
            {
                Value = (int)IsolationMode
            };

            assembly.Culture = Culture;
            assembly.PublicKeyToken = PublicKeyToken;
            assembly.Version = Version;
            assembly.Name = Name;
            assembly.Description = Description;

            if (AssemblyId != Guid.Empty)
            {
                assembly.PluginAssemblyId = new Guid?();
                assembly["pluginassemblyid"] = AssemblyId;
            }

            switch (SourceType)
            {
                case CrmAssemblySourceType.Disk:
                    assembly.Path = ServerFileName;
                    break;

                case CrmAssemblySourceType.Database:
                    //Do nothing in this method
                    break;

                case CrmAssemblySourceType.GAC:
                    //Do nothing
                    break;

                default:
                    throw new NotImplementedException("SourceType = " + SourceType.ToString());
            }

            entityList.Add(PluginAssembly.EntityLogicalName, assembly);

            return entityList;
        }

        public void RefreshFromPluginAssembly(PluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (assembly.Name != null)
            {
                Name = assembly.Name;
            }

            if (assembly.PluginAssemblyId != null)
            {
                AssemblyId = assembly.PluginAssemblyId.Value;
            }

            if (assembly.PackageId != null)
            {
                PackageId = assembly.PackageId?.Id ?? Guid.Empty;
            }

            if (assembly.CreatedOn != null && (assembly.CreatedOn.HasValue))
            {
                CreatedOn = assembly.CreatedOn.Value;
            }

            if (assembly.ModifiedOn != null && (assembly.ModifiedOn.HasValue))
            {
                ModifiedOn = assembly.ModifiedOn.Value;
            }

            if (assembly.SourceType != null)
            {
                SourceType = (CrmAssemblySourceType)Enum.ToObject(typeof(CrmAssemblySourceType), assembly.SourceType.Value);
            }

            if (assembly.IsolationMode != null)
            {
                IsolationMode = (CrmAssemblyIsolationMode)Enum.ToObject(typeof(CrmAssemblyIsolationMode),
                    assembly.IsolationMode.Value);
            }

            if (assembly.Path != null)
            {
                ServerFileName = assembly.Path;
            }

            if (assembly.Version != null)
            {
                Version = assembly.Version;
            }

            if (assembly.PublicKeyToken != null)
            {
                PublicKeyToken = assembly.PublicKeyToken;
            }

            if (assembly.Culture != null)
            {
                Culture = assembly.Culture;
            }

            if (assembly.CustomizationLevel != null)
            {
                CustomizationLevel = assembly.CustomizationLevel.Value;
            }

            Description = assembly.Description;

            if (m_org != null && m_org.ManagedIdentitySupported)
            {
                var identityId = ManagedIdentityHelper.ReadLookup(assembly, out var identityName);
                SetManagedIdentity(identityId, identityName);
            }
        }

        public void SetManagedIdentity(Guid? identityId, string identityName)
        {
            ManagedIdentityId = identityId;
            ManagedIdentityName = identityId.HasValue ? identityName : null;
        }

        public void RemovePlugin(Guid pluginId)
        {
            if (m_pluginList.ContainsKey(pluginId))
            {
                m_pluginList.Remove(pluginId);

                if (m_org != null)
                {
                    Organization.RemovePlugin(this, pluginId);
                }
            }
            else
            {
                throw new ArgumentException("Invalid Plugin Id", "pluginId");
            }
        }

        public override string ToString()
        {
            return NodeText;
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
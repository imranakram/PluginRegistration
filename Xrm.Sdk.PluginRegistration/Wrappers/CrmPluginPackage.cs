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
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Xml.Serialization;

    [Serializable]
    public sealed class CrmPluginPackage : ICrmEntity, ICrmTreeNode, ICloneable
    {
        #region Public Fields

        public const string RelationshipAssemblyToType = "pluginassembly_plugintype";

        #endregion Public Fields

        #region Private Fields

        private static CrmEntityColumn[] m_entityColumns = null;
        private Dictionary<Guid, CrmPluginAssembly> m_assemblyList = new Dictionary<Guid, CrmPluginAssembly>();
        private CrmEntityDictionary<CrmPluginAssembly> m_assemblyReadOnlyList = null;
        private int m_customizationLevel;
        private string m_Name = null;
        private CrmOrganization m_org = null;
        private Guid m_packageId = Guid.Empty;

        #endregion Private Fields

        #region Public Constructors

        public CrmPluginPackage(CrmOrganization org)
        {
            m_org = org;
        }

        public CrmPluginPackage(CrmOrganization org, Guid id, string name, DateTime? createdOn, DateTime? modifiedOn,
            string version, Guid solutionId)
            : this(org)
        {
            PackageId = id;
            Name = name;
            Version = version;
            SolutionId = solutionId;
            UpdateDates(createdOn, modifiedOn);
        }

        public CrmPluginPackage(CrmOrganization org, PluginPackage package)
            : this(org)
        {
            RefreshFromPluginPackage(package);
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
                        new CrmEntityColumn("ModifiedOn", "Modified On", typeof(string)),
                        new CrmEntityColumn("Version", "Version", typeof(string)),
                        new CrmEntityColumn("Path", "Path", typeof(string)),
                        new CrmEntityColumn("Id", "PackageId", typeof(Guid)),
                        new CrmEntityColumn("SolutionId", "PackageId", typeof(Guid)),
                        };
                }

                return m_entityColumns;
            }
        }

        [Browsable(false)]
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

        /// <summary>
        /// Retrieves the Created On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? CreatedOn { get; private set; }

        [XmlIgnore]
        [Browsable(false)]
        public Guid EntityId
        {
            get
            {
                return PackageId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string EntityType
        {
            get
            {
                return PluginPackage.EntityLogicalName;
            }
        }

        public bool IsSystemCrmEntity => false;

        /// <summary>
        /// Retrieves the Modified On date of the entity. To update, see UpdateDates.
        /// </summary>
        [Category("Information"), Browsable(true), ReadOnly(true)]
        public DateTime? ModifiedOn { get; private set; }

        [Category("Information"), Browsable(true), Description("Name of the Package"), ReadOnly(true)]
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
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                if (m_assemblyList == null || m_assemblyList.Count == 0)
                {
                    return new List<ICrmTreeNode> { };
                }
                else
                {
                    var children = new CrmPluginAssembly[m_assemblyList.Count];
                    m_assemblyList.Values.CopyTo(children, 0);
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
                return PackageId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                return CrmTreeNodeImageType.Package;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                return CrmTreeNodeImageType.PackageSelected;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeText
        {
            get
            {
                return string.Format("({0}) {1}", NodeTypeLabel, Name);
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                return CrmTreeNodeType.Package;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                return "Package";
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
                    foreach (CrmPluginAssembly assembly in m_assemblyList.Values)
                    {
                        if (assembly.Organization == null)
                        {
                            assembly.Organization = m_org;
                        }

                        m_org.AddAssembly(this, assembly);
                    }
                }
                else
                {
                    throw new NotSupportedException("Cannot change the Organization once it has been set");
                }
            }
        }

        [Category("Information"), Browsable(true), Description("Unique identifier of Package"), ReadOnly(true)]
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

                if (m_assemblyList != null)
                {
                    foreach (CrmPluginAssembly assembly in m_assemblyList.Values)
                    {
                        assembly.PackageId = value;
                    }
                }
            }
        }

        /// <summary>
        /// Managed identity linked to this package. Only shown for online organizations.
        /// </summary>
        [Category("Managed Identity"), Browsable(true), ReadOnly(true), OnlineOnly(true), DisplayName("Managed Identity")]
        public string ManagedIdentityName { get; private set; }

        [Category("Managed Identity"), Browsable(true), ReadOnly(true), OnlineOnly(true), DisplayName("Managed Identity Id")]
        public Guid? ManagedIdentityId { get; private set; }

        public Guid SolutionId { get; internal set; }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                var valueList = new Dictionary<string, object>
                {
                    { "Id", PackageId },
                    { "Name", Name },
                    {
                        "ModifiedOn",
                        (ModifiedOn.HasValue ? ModifiedOn.ToString() : "")
                    },
                    { "Version", ConvertNullStringToEmpty(Version) },
                    { "SolutionId", SolutionId },
                };

                return valueList;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string Version { get; set; }

        #endregion Public Properties

        #region Public Indexers

        [Browsable(false)]
        public CrmPluginAssembly this[Guid assemblyId]
        {
            get
            {
                return m_assemblyList[assemblyId];
            }
        }

        #endregion Public Indexers

        #region Public Methods

        public void AddPluginAssembly(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            m_assemblyList.Add(assembly.AssemblyId, assembly);

            if (m_org != null)
            {
                Organization.AddAssembly(this, assembly);
            }
        }

        public void ClearAssemblies()
        {
            m_assemblyList.Clear();

            if (m_org != null)
            {
                Organization.ClearAssemblies();
            }
        }

        public object Clone()
        {
            return Clone(true);
        }

        public CrmPluginPackage Clone(bool includeOrganization)
        {
            CrmPluginPackage newPackage;
            if (includeOrganization)
            {
                newPackage = new CrmPluginPackage(m_org);
            }
            else
            {
                newPackage = new CrmPluginPackage(null);
            }

            newPackage.m_packageId = m_packageId;
            newPackage.m_Name = m_Name;
            newPackage.CreatedOn = CreatedOn;
            newPackage.ModifiedOn = ModifiedOn;
            newPackage.Version = Version;
            newPackage.SolutionId = SolutionId;
            newPackage.ManagedIdentityId = ManagedIdentityId;
            newPackage.ManagedIdentityName = ManagedIdentityName;

            //Create a new plugin list
            var newAssemblyList = new Dictionary<Guid, CrmPluginAssembly>();
            foreach (CrmPluginAssembly assembly in m_assemblyList.Values)
            {
                //Clone the plugin
                var clonedAssembly = assembly.Clone(includeOrganization);

                //Add the plugin to the new list
                newAssemblyList.Add(clonedAssembly.AssemblyId, clonedAssembly);
            }

            //Assign the list to the new object
            newPackage.m_assemblyList = newAssemblyList;

            return newPackage;
        }

        public Dictionary<string, object> GenerateCrmEntities()
        {
            var entityList = new Dictionary<string, object>();

            PluginPackage package = new PluginPackage();
            if (PackageId != Guid.Empty)
            {
                package.PluginPackageId = new Guid?();
                package["pluginpackageid"] = PackageId;
            }

            package.Version = Version;
            package.Name = Name;
            package.SolutionId = SolutionId;

            entityList.Add(PluginPackage.EntityLogicalName, package);

            return entityList;
        }

        public void RefreshFromPluginPackage(PluginPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (package.Name != null)
            {
                Name = package.Name;
            }

            if (package.PluginPackageId != null)
            {
                PackageId = package.PluginPackageId.Value;
            }

            if (package.CreatedOn != null && (package.CreatedOn.HasValue))
            {
                CreatedOn = package.CreatedOn.Value;
            }

            if (package.ModifiedOn != null && (package.ModifiedOn.HasValue))
            {
                ModifiedOn = package.ModifiedOn.Value;
            }

            if (package.Version != null)
            {
                Version = package.Version;
            }

            if (package.SolutionId != null)
            {
                SolutionId = package.SolutionId ?? Guid.Empty;
            }

            if (m_org != null && m_org.ManagedIdentitySupported)
            {
                var identityId = ManagedIdentityHelper.ReadLookup(package, out var identityName);
                SetManagedIdentity(identityId, identityName);
            }
        }

        public void SetManagedIdentity(Guid? identityId, string identityName)
        {
            ManagedIdentityId = identityId;
            ManagedIdentityName = identityId.HasValue ? identityName : null;
        }

        public void RemoveAssembly(Guid assemblyId)
        {
            if (m_assemblyList.ContainsKey(assemblyId))
            {
                m_assemblyList.Remove(assemblyId);

                if (m_org != null)
                {
                    Organization.RemoveAssembly(assemblyId);
                }
            }
            else
            {
                throw new ArgumentException("Invalid Assembly Id", "assemblyId");
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
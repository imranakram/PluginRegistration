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

    public enum CrmPluginIsolatable
    {
        Yes,
        No,
        Unknown
    }

    public enum CrmPluginType
    {
        [Description("Plugin")]
        Plugin,

        [Description("Workflow Activity")]
        WorkflowActivity
    }

    [Serializable]
    public sealed class CrmPlugin : ICrmEntity, ICrmTreeNode, ICloneable
    {
        #region Public Fields

        public const string RelationshipTypeToStep = "plugintype_sdkmessageprocessingstep";

        #endregion Public Fields

        #region Private Fields

        private const string V3CalloutProxyTypeName = "Microsoft.Crm.Extensibility.V3CalloutProxyPlugin";
        private static CrmEntityColumn[] m_entityColumns = null;
        private string m_assemblyName = null;
        private DateTime? m_createdOn = null;
        private int m_customizationLevel = 1;
        private string m_friendlyName;
        private bool m_friendlyNameIgnore;
        private CrmPluginIsolatable m_isolatable = CrmPluginIsolatable.Unknown;
        private DateTime? m_modifiedOn = null;
        private string m_name = null;
        private string m_AssemblyVersion = null;
        private CrmOrganization m_org;
        private Guid m_pluginAssemblyId = Guid.Empty;
        private Guid m_pluginId = Guid.Empty;
        private CrmPluginType m_plugType = CrmPluginType.Plugin;
        private Dictionary<Guid, CrmPluginStep> m_stepList = new Dictionary<Guid, CrmPluginStep>();
        private CrmEntityDictionary<CrmPluginStep> m_stepReadOnlyList = null;
        private string m_typeName = null;
        private string m_workflowActivityGroupName = null;

        #endregion Private Fields

        #region Public Constructors

        public CrmPlugin(CrmOrganization org)
        {
            m_org = org;
            FriendlyName = Guid.NewGuid().ToString();
        }

        public CrmPlugin(CrmOrganization org, Guid pluginId, Guid assemblyId, string assemblyName, string friendlyName,
            string typeName, CrmPluginType type, CrmPluginIsolatable isolatable, DateTime? createdOn, DateTime? modifiedOn)
            : this(org)
        {
            PluginId = pluginId;
            AssemblyId = assemblyId;
            AssemblyName = assemblyName;
            TypeName = typeName;
            Name = typeName;
            FriendlyName = friendlyName;
            PluginType = type;
            Isolatable = isolatable;
            UpdateDates(createdOn, modifiedOn);
        }

        public CrmPlugin(CrmOrganization org, PluginType type)
            : this(org)
        {
            RefreshFromPluginType(type);
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
                        new CrmEntityColumn("Assembly", "Assembly", typeof(string)),
                        new CrmEntityColumn("ModifiedOn", "Modified On", typeof(string)),
                        new CrmEntityColumn("FriendlyName", "Friendly Name", typeof(string)),
                        new CrmEntityColumn("Name", "Name", typeof(string)),
                        new CrmEntityColumn("TypeName", "Type Name", typeof(string)),
                        new CrmEntityColumn("WorkflowActivityGroupName", "WorkflowActivityGroupName", typeof(string)),
                        new CrmEntityColumn("Description", "Description", typeof(string)),
                        new CrmEntityColumn("Isolatable", "Isolatable", typeof(string)),
                        new CrmEntityColumn("Id", "PluginId", typeof(Guid)) };
                }

                return m_entityColumns;
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public Guid AssemblyId
        {
            get
            {
                return m_pluginAssemblyId;
            }
            set
            {
                if (value == m_pluginAssemblyId)
                {
                    return;
                }

                m_pluginAssemblyId = value;

                foreach (CrmPluginStep step in m_stepList.Values)
                {
                    step.AssemblyId = value;
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string AssemblyName
        {
            get
            {
                return m_assemblyName;
            }

            set
            {
                m_assemblyName = value;
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

        [Category("Editable"), Browsable(true), ReadOnly(false)]
        public string Description
        {
            get;
            set;
        }

        [XmlIgnore]
        [Browsable(false)]
        public Guid EntityId
        {
            get
            {
                return m_pluginId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string EntityType
        {
            get
            {
                return Entities.PluginType.EntityLogicalName;
            }
        }

        [Category("Editable"), Browsable(true), ReadOnly(false)]
        public string FriendlyName
        {
            get
            {
                return m_friendlyName;
            }
            set
            {
                m_friendlyName = GenerateFriendlyName(value, out m_friendlyNameIgnore);
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginIsolatable Isolatable
        {
            get
            {
                return m_isolatable;
            }

            set
            {
                m_isolatable = value;
            }
        }

        [Browsable(false)]
        public bool IsProfilerPlugin { get; set; }

        [XmlIgnore]
        [Category("Information"), Browsable(true), ReadOnly(true)]
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
        public DateTime? ModifiedOn
        {
            get
            {
                return m_modifiedOn;
            }
        }

        [Category("Editable"), Browsable(true), ReadOnly(false)]
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public List<ICrmTreeNode> NodeChildren
        {
            get
            {
                if (m_stepList == null || m_stepList.Count == 0)
                {
                    return new List<ICrmTreeNode> { };
                }
                else
                {
                    var children = new List<CrmPluginStep>(m_stepList.Count);
                    children.AddRange(m_stepList.Values);
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
                return m_pluginId;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeImageType
        {
            get
            {
                if (IsProfilerPlugin)
                {
                    return CrmTreeNodeImageType.PluginProfiler;
                }
                else
                {
                    switch (m_plugType)
                    {
                        case CrmPluginType.Plugin:
                            return CrmTreeNodeImageType.Plugin;

                        case CrmPluginType.WorkflowActivity:
                            return CrmTreeNodeImageType.WorkflowActivity;

                        default:
                            throw new NotImplementedException("PluginType = " + m_plugType);
                    }
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeImageType NodeSelectedImageType
        {
            get
            {
                if (IsProfilerPlugin)
                {
                    return CrmTreeNodeImageType.PluginProfilerSelected;
                }
                else
                {
                    switch (m_plugType)
                    {
                        case CrmPluginType.Plugin:
                            return CrmTreeNodeImageType.PluginSelected;

                        case CrmPluginType.WorkflowActivity:
                            return CrmTreeNodeImageType.WorkflowActivitySelected;

                        default:
                            throw new NotImplementedException("PluginType = " + m_plugType);
                    }
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeText
        {
            get
            {
                if (IsProfilerPlugin)
                {
                    return "Plug-in Profiler";
                }

                string format;
                if (CrmPluginIsolatable.Yes == Isolatable)
                {
                    format = "({0}) {2}{3} - Isolatable";
                }
                else if (string.IsNullOrWhiteSpace(WorkflowActivityGroupName) ||
                    WorkflowActivityGroupName.StartsWith(AssemblyName + " (", StringComparison.Ordinal))
                {
                    format = "({0}) {2}{3}";
                }
                else
                {
                    format = "({0}) {1}: {2}{3}";
                }

                return string.Format(format, NodeTypeLabel, WorkflowActivityGroupName, string.IsNullOrWhiteSpace(Name) ? Description : Name, string.IsNullOrEmpty(AssemblyVersion)?"":$" [{AssemblyVersion}]");
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public CrmTreeNodeType NodeType
        {
            get
            {
                switch (m_plugType)
                {
                    case CrmPluginType.Plugin:
                        return CrmTreeNodeType.Plugin;

                    case CrmPluginType.WorkflowActivity:
                        return CrmTreeNodeType.WorkflowActivity;

                    default:
                        throw new NotImplementedException("PluginType = " + m_plugType);
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string NodeTypeLabel
        {
            get
            {
                switch (PluginType)
                {
                    case CrmPluginType.Plugin:
                        return "Plugin";

                    case CrmPluginType.WorkflowActivity:
                        return "Workflow Activity";

                    default:
                        throw new NotImplementedException("PluginType = " + PluginType.ToString());
                }
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public string AssemblyVersion
        {
            get
            {
                return m_AssemblyVersion;
            }
            set
            {
                m_AssemblyVersion = value;
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

                foreach (CrmPluginStep step in m_stepList.Values)
                {
                    step.PluginId = value;
                }
            }
        }

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public CrmPluginType PluginType
        {
            get
            {
                return m_plugType;
            }
            set
            {
                m_plugType = value;
            }
        }

        [Browsable(false)]
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

        [Category("Information"), Browsable(true), ReadOnly(true)]
        public string TypeName
        {
            get
            {
                return m_typeName;
            }

            set
            {
                m_typeName = value;
            }
        }

        [XmlIgnore]
        [Browsable(false)]
        public Dictionary<string, object> Values
        {
            get
            {
                Dictionary<string, object> valueList = new Dictionary<string, object>
                {
                    { "Id", PluginId },
                    { "Assembly", AssemblyName },
                    { "ModifiedOn", ModifiedOn },
                    { "FriendlyName", FriendlyName },
                    { "Name", Name },
                    { "TypeName", TypeName },
                    { "WorkflowActivityGroupName", WorkflowActivityGroupName },
                    { "Isolatable", Isolatable.ToString() },
                    { "Description", Description }
                };
                return valueList;
            }
        }

        [Category("Editable"), Browsable(true), ReadOnly(false)]
        public string WorkflowActivityGroupName
        {
            get
            {
                return m_workflowActivityGroupName;
            }
            set
            {
                m_workflowActivityGroupName = value;
            }
        }

        /// <summary>
        /// A workflow activity's arguments as read from a build, to compare with the registered ones before
        /// an update. Null when not read.
        /// </summary>
        [Browsable(false), XmlIgnore]
        public List<Helpers.ActivityArgument> ActivityArguments { get; set; }

        #endregion Public Properties

        #region Public Indexers

        [Browsable(false)]
        public CrmPluginStep this[Guid stepId]
        {
            get
            {
                return m_stepList[stepId];
            }
        }

        #endregion Public Indexers

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

        public void ClearSteps()
        {
            m_stepList.Clear();

            if (Organization != null)
            {
                Organization.ClearSteps(PluginId);
            }
        }

        public object Clone()
        {
            return Clone(true);
        }

        public CrmPlugin Clone(bool includeOrganization)
        {
            CrmPlugin newPlugin;
            if (includeOrganization)
            {
                newPlugin = new CrmPlugin(m_org);
            }
            else
            {
                newPlugin = new CrmPlugin(null);
            }

            newPlugin.m_assemblyName = m_assemblyName;
            newPlugin.m_createdOn = m_createdOn;
            newPlugin.m_customizationLevel = m_customizationLevel;
            newPlugin.m_friendlyName = m_friendlyName;
            newPlugin.m_friendlyNameIgnore = m_friendlyNameIgnore;
            newPlugin.m_isolatable = m_isolatable;
            newPlugin.m_modifiedOn = m_modifiedOn;
            newPlugin.m_pluginAssemblyId = m_pluginAssemblyId;
            newPlugin.m_pluginId = m_pluginId;
            newPlugin.m_plugType = m_plugType;
            newPlugin.m_typeName = m_typeName;

            //Create a new step list
            Dictionary<Guid, CrmPluginStep> newStepList = new Dictionary<Guid, CrmPluginStep>();
            foreach (CrmPluginStep step in m_stepList.Values)
            {
                //Clone the step
                CrmPluginStep clonedStep = step.Clone(includeOrganization);

                //Add the step to the new list
                newStepList.Add(clonedStep.StepId, clonedStep);
            }

            //Assign the list to the new object
            newPlugin.m_stepList = newStepList;

            return newPlugin;
        }

        public Dictionary<string, object> GenerateCrmEntities()
        {
            PluginType plugin = new PluginType();
            if (PluginId != Guid.Empty)
            {
                plugin["plugintypeid"] = new Guid?();
                plugin["plugintypeid"] = PluginId;
            }

            if (AssemblyId != Guid.Empty)
            {
                plugin.PluginAssemblyId = new EntityReference();
                plugin.PluginAssemblyId.LogicalName = PluginAssembly.EntityLogicalName;
                plugin.PluginAssemblyId.Id = AssemblyId;
            }
            else
            {
                throw new ArgumentException("PluginAssembly has not been set", "plugin");
            }

            plugin.TypeName = TypeName;
            plugin.FriendlyName = m_friendlyName;

            plugin.Name = Name;
            plugin.Description = Description;
            plugin.WorkflowActivityGroupName = WorkflowActivityGroupName;

            Dictionary<string, object> entityList = new Dictionary<string, object>();
            entityList.Add(Entities.PluginType.EntityLogicalName, plugin);

            return entityList;
        }

        public string GenerateNodeText(string newFriendlyName)
        {
            bool ignoreFriendlyName;
            string newText = GenerateFriendlyName(newFriendlyName, out ignoreFriendlyName);

            if (ignoreFriendlyName)
            {
                return TypeName;
            }
            else
            {
                return newText;
            }
        }

        public void RefreshFromPluginType(PluginType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type.AssemblyName != null)
            {
                AssemblyName = type.AssemblyName;
            }

            if (type.TypeName != null)
            {
                TypeName = type.TypeName;
            }

            if (type.FriendlyName != null)
            {
                FriendlyName = type.FriendlyName;
            }

            if (type.Name != null)
            {
                Name = type.Name;
            }

            Description = type.Description;

            if (type.PluginTypeId != null)
            {
                PluginId = type.PluginTypeId.Value;
            }

            if (type.PluginAssemblyId != null)
            {
                AssemblyId = type.PluginAssemblyId.Id;
            }

            if (type.CustomizationLevel != null)
            {
                CustomizationLevel = type.CustomizationLevel.Value;
            }

            if (type.CreatedOn != null && (type.CreatedOn.HasValue))
            {
                m_createdOn = type.CreatedOn.Value;
            }

            if (type.ModifiedOn != null && (type.ModifiedOn.HasValue))
            {
                m_modifiedOn = type.ModifiedOn.Value;
            }

            if (type.IsWorkflowActivity != null && (type.IsWorkflowActivity.HasValue))
            {
                if (type.IsWorkflowActivity.Value)
                {
                    PluginType = CrmPluginType.WorkflowActivity;
                }
                else
                {
                    PluginType = CrmPluginType.Plugin;
                }
            }

            WorkflowActivityGroupName = type.WorkflowActivityGroupName;
        }

        public void RemoveStep(Guid stepId)
        {
            if (m_stepList.ContainsKey(stepId))
            {
                m_stepList.Remove(stepId);

                if (Organization != null)
                {
                    Organization.RemoveStep(this, stepId);
                }
            }
            else
            {
                throw new ArgumentException("Invalid Step Id", "stepId");
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

        private string GenerateFriendlyName(string newName, out bool ignoreFriendlyName)
        {
            if (string.IsNullOrEmpty(newName) ||
                string.Equals(m_typeName, newName, StringComparison.InvariantCultureIgnoreCase))
            {
                ignoreFriendlyName = true;
                if (m_friendlyNameIgnore)
                {
                    return m_friendlyName;
                }
                else
                {
                    return Guid.NewGuid().ToString();
                }
            }
            else
            {
                // Checking if name should be ignored
                var guidOutput = Guid.Empty;
                ignoreFriendlyName = Guid.TryParse(newName, out guidOutput);

                return newName;
            }
        }

        #endregion Private Methods
    }
}
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
    using Controls;
    using Entities;
    using Helpers;
    using Microsoft.Xrm.Sdk;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Wrappers;

    public partial class PluginRegistrationForm : Form
    {
        #region Private Fields

        private bool m_assemblyLoaded = false;
        private CrmPluginAssembly m_currentAssembly;
        private string m_lastAssemblyFileName;
        private PluginAssemblyFileMapping m_mapping;
        private List<PluginAssemblyFileMapping> m_mappings;
        private CrmOrganization m_org;
        private MainControl m_orgControl;
        private ProgressIndicator m_progRegistration;
        private List<CrmPlugin> m_registeredPluginList;

        #endregion Private Fields

        #region Public Constructors

        public PluginRegistrationForm(CrmOrganization org, MainControl orgControl, CrmPluginAssembly assembly, List<PluginAssemblyFileMapping> mappings)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (orgControl == null)
            {
                throw new ArgumentNullException("orgControl");
            }

            InitializeComponent();

            m_org = org;
            m_orgControl = orgControl;
            m_progRegistration = new ProgressIndicator(ProgressIndicatorInit, ProgressIndicatorComplete,
                ProgressIndicatorAddText, ProgressIndicatorSetText,
                ProgressIndicatorIncrement, null);
            m_currentAssembly = assembly;
            m_mappings = mappings;

            radIsolationSandbox.Checked = org.ConnectionDetail.UseOnline;

            trvPlugins.CrmTreeNodeSorter = orgControl.CrmTreeNodeSorter;

            if (org.ConnectionDetail.UseOnline)
            {
                radIsolationNone.Enabled = false;
                radDisk.Enabled = false;
                radDB.Enabled = false;
            }
            else
            {
                radIsolationNone.Enabled = true;
            }
            //Check if this is a known assembly
            if (null == assembly)
            {
                //If this is a known assembly, check for the default isolation mode for each authentication type
                if (null != org.ConnectionDetail)
                {
                    // TODO: Come back
                    //switch (org.ConnectionDetail.GetDiscoveryService().ServiceConfiguration.AuthenticationType)
                    //{
                    //    case Microsoft.Xrm.Sdk.Client.AuthenticationProviderType.ActiveDirectory:
                    //        radIsolationNone.Checked = true;
                    //        break;
                    //    default:
                    //        radIsolationSandbox.Checked = true;
                    //        break;
                    //}
                }
            }
            else
            {
                m_registeredPluginList = new List<CrmPlugin>();

                LoadAssembly(assembly, false);

                switch (assembly.IsolationMode)
                {
                    case CrmAssemblyIsolationMode.Sandbox:
                        radIsolationSandbox.Checked = true;
                        break;

                    case CrmAssemblyIsolationMode.None:
                        radIsolationNone.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException($"IsolationMode = {assembly.IsolationMode.ToString()}");
                }

                switch (assembly.SourceType)
                {
                    case CrmAssemblySourceType.Database:
                        radDB.Checked = true;
                        break;

                    case CrmAssemblySourceType.Disk:
                        radDisk.Checked = true;
                        break;

                    case CrmAssemblySourceType.GAC:
                        radGAC.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException($"SourceType = {assembly.SourceType.ToString()}");
                }

                txtServerFileName.Text = assembly.ServerFileName;

                m_mapping = mappings.FirstOrDefault(m => m.PluginAssemblyName == assembly.Name && m.ConnectionId == org.ConnectionDetail.ConnectionId.Value);
                if (m_mapping == null)
                {
                    m_mapping = mappings.FirstOrDefault(m => m.PluginAssemblyName == assembly.Name);
                    if (m_mapping != null)
                    {
                        mappings.Remove(m_mapping);
                        m_mapping = new PluginAssemblyFileMapping
                        {
                            ConnectionId = org.ConnectionDetail.ConnectionId.Value,
                            PluginAssemblyName = m_mapping.PluginAssemblyName,
                            FilePath = m_mapping.FilePath
                        };
                        m_mappings.Add(m_mapping);
                    }
                }

                if (m_mapping != null && !string.IsNullOrEmpty(m_mapping.FilePath) && File.Exists(m_mapping.FilePath))
                {
                    AssemblyPathControl.FileName = m_mapping.FilePath;
                    AssemblyPathControl_PathChanged(AssemblyPathControl, new EventArgs());
                    btnLoadAssembly_Click(btnLoadAssembly, new EventArgs());
                }

                Text = $"Update Assembly: {assembly.Name}";
                btnRegister.Text = "Update Selected Plugins";
            }

            EnableRegistrationControls();
        }

        #endregion Public Constructors

        #region Private Methods

        private void AssemblyPathControl_BrowseCompleted(object sender, EventArgs e)
        {
            btnLoadAssembly.PerformClick();
        }

        private void AssemblyPathControl_PathChanged(object sender, EventArgs e)
        {
            btnLoadAssembly.Enabled = AssemblyPathControl.HasFileName;
            EnableRegistrationControls();

            if (AssemblyPathControl.FileExists)
            {
                string fileName = Path.GetFileName(AssemblyPathControl.FileName);

                //Only want to change the server file name if it is the same as the assembly's file name
                //If it isn't, then the user changed the file name
                if (txtServerFileName.TextLength == 0 || string.Equals(txtServerFileName.Text, m_lastAssemblyFileName))
                {
                    txtServerFileName.Text = fileName;
                }

                m_lastAssemblyFileName = fileName;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnLoadAssembly_Click(object sender, EventArgs e)
        {
            if (!AssemblyPathControl.FileExists)
            {
                MessageBox.Show("Error: Unable to locate the specified file. Please ensure that it exists",
                    "Plugin Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Load the assembly
            CrmPluginAssembly assembly;
            try
            {
                assembly = RegistrationHelper.RetrievePluginsFromAssembly(AssemblyPathControl.FileName);
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to load the specified Plugin Assembly", "Plugins", ex);
                return;
            }

            LoadAssembly(assembly, true);

            //Mark the assembly as having been loaded
            m_assemblyLoaded = true;
            chkUpdateAssembly.Checked = true;
            if (null != m_currentAssembly)
            {
                chkUpdateAssembly.Visible = true;
            }

            //Enable the controls
            EnableRegistrationControls();
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            const string ERROR_CAPTION = "Registration Error";
            string ERROR_MESSAGE;
            if (m_currentAssembly == null)
            {
                ERROR_MESSAGE = "There was an error while registering the selected plugins.";
            }
            else
            {
                ERROR_MESSAGE = "There was an error while updating the selected plugins.";
            }

            #region Extract Plugin Registration Information

            m_progRegistration.Complete(true); //Just in case it has incorrect information

            //Determine the source type. If we are talking about an assembly on disk, verify that it exists
            if (GetAssemblySourceType() == CrmAssemblySourceType.Disk)
            {
                if (string.IsNullOrEmpty(txtServerFileName.Text.Trim()))
                {
                    MessageBox.Show(
                        "If the Registration Location is Disk, the \"File Name on Server\" must be specified",
                        "Missing Information",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    m_progRegistration.Complete(false);

                    return;
                }
            }

            // Create a list of currently selected plugins
            var assemblyCanBeIsolated = true;
            var checkedPluginList = new Dictionary<string, CrmPlugin>();

            foreach (ICrmTreeNode node in trvPlugins?.CheckedNodes)
            {
                if (node.NodeType == CrmTreeNodeType.Plugin || node.NodeType == CrmTreeNodeType.WorkflowActivity)
                {
                    var plugin = (CrmPlugin)node;
                    if (CrmPluginIsolatable.No == plugin.Isolatable)
                    {
                        assemblyCanBeIsolated = false;
                    }

                    checkedPluginList.Add(plugin.TypeName, plugin);
                }
            }

            //Check if there are any plugins selected
            if (checkedPluginList.Count == 0)
            {
                MessageBox.Show(
                    "No plugins have been selected from the list. Please select at least one and try again.",
                    "No Plugins Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Verify that a valid isolation mode has been selected
            if (radIsolationSandbox.Checked && !assemblyCanBeIsolated)
            {
                MessageBox.Show(
                    "Since some of the plug-ins cannot be isolated, the assembly cannot be marked as Isolated.",
                    "Isolation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            //Reload the assembly
            var assemblyPath = AssemblyPathControl.FileName;
            CrmPluginAssembly assembly;
            if (string.IsNullOrEmpty(assemblyPath))
            {
                // Clone the existing assembly
                assembly = m_currentAssembly.Clone(false);
            }
            else
            {
                assembly = RegistrationHelper.RetrievePluginsFromAssembly(assemblyPath);

                //Retrieve the source type and determine if the
                assembly.SourceType = GetAssemblySourceType();

                if (CrmAssemblySourceType.Disk != assembly.SourceType)
                {
                    assembly.ServerFileName = null;
                }
                else
                {
                    assembly.ServerFileName = txtServerFileName.Text;
                }
            }

            if (m_currentAssembly != null)
            {
                var oldVersion = new Version(m_currentAssembly.Version);
                var newVersion = new Version(assembly.Version);

                if (oldVersion.Major != newVersion.Major)
                {
                    MessageBox.Show(
                        $"Assembly's major version has changed from {oldVersion} to {newVersion}.\n\nSuch an update is not supported, please register another instance of this assembly instead!",
                        "Assembly's version missmatch",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (oldVersion.Minor != newVersion.Minor)
                {
                    MessageBox.Show(
                        $"Assembly's minor version has changed from {oldVersion} to {newVersion}.\n\nSuch an update is not supported, please register another instance of this assembly instead!",
                        "Assembly's version missmatch",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }
            }

            // Ensure the checked items were all found in the assembly
            var registerPluginList = new List<CrmPlugin>();
            var pluginList = new List<CrmPlugin>();
            var removedPluginList = new List<CrmPlugin>();
            var missingPluginList = new List<CrmPlugin>();

            try
            {
                // Sequential: the lists below are plain List<T>, which isn't safe to add to from several threads
                foreach (var currentPlugin in assembly.Plugins.Values)
                {
                    var foundPlugin = m_registeredPluginList?.Where(x => x.TypeName.ToLowerInvariant() == currentPlugin.TypeName.ToLowerInvariant()).FirstOrDefault();
                    var alreadyExisted = (m_registeredPluginList != null && foundPlugin != null);

                    if (alreadyExisted)
                    {
                        currentPlugin.AssemblyId = m_currentAssembly.AssemblyId;
                        currentPlugin.PluginId = foundPlugin.PluginId;
                    }

                    if (checkedPluginList.ContainsKey(currentPlugin.TypeName))
                    {
                        registerPluginList.Add(currentPlugin);

                        if (currentPlugin.PluginType == CrmPluginType.Plugin)
                        {
                            pluginList.Add(currentPlugin);
                        }
                    }
                    else if (alreadyExisted)
                    {
                        removedPluginList.Add(currentPlugin);
                    }
                }

                if (m_registeredPluginList != null)
                {
                    foreach (var currentRecord in m_registeredPluginList)
                    {
                        if (!assembly.Plugins.Values.ToList().Any(x => x.TypeName.ToLowerInvariant() == currentRecord.TypeName.ToLowerInvariant()))
                        {
                            missingPluginList.Add(currentRecord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to load the specified Plugin Assembly.", "Plugins", ex);
                return;
            }

            // Update the assembly with the information specified by the user
            assembly.IsolationMode = GetIsolationMode();

            if (missingPluginList.Count != 0)
            {
                var list = missingPluginList.Select(x => x.TypeName).Aggregate((name01, name02) => name01 + "\n" + name02);

                MessageBox.Show(
                    $"Following plugin are missing in the assembly:\n\n{list}\n\nRegistration cannot continue!",
                    "Plugins are missing",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // An assembly with plugins must be strongly signed
            if (pluginList.Count != 0)
            {
                if (string.IsNullOrEmpty(assembly.PublicKeyToken))
                {
                    MessageBox.Show(
                        "Assemblies containing Plugins must be strongly signed. Sign the Assembly using a KeyFile.",
                        "Strong Names Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }
            }

            // Check if there are any plugins selected that were in the assembly.
            if (registerPluginList.Count == 0)
            {
                MessageBox.Show(
                    "No plugins have been selected from the list. Please select at least one and try again.",
                    "No Plugins Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }
            else
            {
                assembly.ClearPlugins();
            }

            // If we are doing an Update, do some special processing
            if (m_currentAssembly != null)
            {
                assembly.AssemblyId = m_currentAssembly.AssemblyId;
            }

            #endregion Extract Plugin Registration Information

            #region Register Plugin

            m_progRegistration.Initialize(registerPluginList.Count + removedPluginList.Count, "Preparing Registration");

            int registeredAssemblies = 0;
            int ignoredAssemblies = 0;
            int updatedAssemblies = 0;
            bool createAssembly;

            //Check whether the plugin exists. If it exists, should we use the existing one?
            var retrieveDateList = new List<ICrmEntity>();
            try
            {
                Guid pluginAssemblyId = Guid.Empty;
                if (m_currentAssembly != null)
                {
                    if (chkUpdateAssembly.Checked)
                    {
                        var originalGroupName = RegistrationHelper.GenerateDefaultGroupName(m_currentAssembly.Name, new Version(m_currentAssembly.Version));
                        var newGroupName = RegistrationHelper.GenerateDefaultGroupName(assembly.Name, new Version(assembly.Version));

                        //The workflow activities go with the update, so Dataverse reads their arguments again (#16)
                        var activityTypes = RegistrationHelper.WorkflowActivityTypesForUpdate(m_currentAssembly.Plugins, originalGroupName, newGroupName);

                        //Do the actual update to the assembly
                        RegistrationHelper.UpdateAssembly(m_org, assemblyPath, assembly, activityTypes);

                        m_currentAssembly.Name = assembly.Name;
                        m_currentAssembly.Culture = assembly.Culture;
                        m_currentAssembly.CustomizationLevel = assembly.CustomizationLevel;
                        m_currentAssembly.PublicKeyToken = assembly.PublicKeyToken;
                        m_currentAssembly.ServerFileName = assembly.ServerFileName;
                        m_currentAssembly.SourceType = assembly.SourceType;
                        m_currentAssembly.Version = assembly.Version;
                        m_currentAssembly.IsolationMode = assembly.IsolationMode;

                        retrieveDateList.Add(m_currentAssembly);

                        foreach (var type in activityTypes.Where(t => t.WorkflowActivityGroupName != null))
                        {
                            var plugin = m_currentAssembly.Plugins[type.Id];

                            plugin.WorkflowActivityGroupName = type.WorkflowActivityGroupName;
                            retrieveDateList.Add(plugin);
                        }

                        updatedAssemblies++;
                    }
                    else if (!chkUpdateAssembly.Visible && assembly.IsolationMode != m_currentAssembly.IsolationMode)
                    {
                        var updateAssembly = new PluginAssembly()
                        {
                            Id = assembly.AssemblyId,
                            IsolationMode = new OptionSetValue((int)assembly.IsolationMode)
                        };

                        m_org.OrganizationService.Update(updateAssembly);

                        m_currentAssembly.ServerFileName = assembly.ServerFileName;
                        m_currentAssembly.SourceType = assembly.SourceType;
                        m_currentAssembly.IsolationMode = assembly.IsolationMode;

                        retrieveDateList.Add(m_currentAssembly);

                        updatedAssemblies++;
                    }

                    assembly = m_currentAssembly;

                    if (m_mapping == null)
                    {
                        m_mapping = new PluginAssemblyFileMapping
                        {
                            PluginAssemblyName = assembly.Name,
                            FilePath = AssemblyPathControl.FileName,
                            ConnectionId = m_org.ConnectionDetail.ConnectionId.Value
                        };
                        m_mappings.Add(m_mapping);
                    }
                    else
                    {
                        m_mapping.FilePath = AssemblyPathControl.FileName;
                    }

                    createAssembly = false;
                    m_progRegistration.Increment();

                    m_orgControl.RefreshAssembly(m_currentAssembly, false);
                }
                else
                {
                    createAssembly = true;
                    m_progRegistration.Increment();
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    ERROR_MESSAGE = ex.Message;
                }

                m_progRegistration.Increment($"ERROR: {ERROR_MESSAGE}");

                ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                m_progRegistration.Complete(false);
                return;
            }

            //Register the assembly (if needed)
            if (createAssembly)
            {
                try
                {
                    assembly.AssemblyId = RegistrationHelper.RegisterAssembly(m_org, assemblyPath, assembly);
                    assembly.Organization = m_org;

                    retrieveDateList.Add(assembly);
                }
                catch (Exception ex)
                {
                    var chunks = ex.Message.Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);

                    if (chunks.Length > 1)
                    {
                        // Replacing generic error message extracted from exception to the title of the window
                        ERROR_MESSAGE = chunks[chunks.Length - 1];
                    }

                    m_progRegistration.Increment($"ERROR: {ERROR_MESSAGE}");

                    ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                    m_progRegistration.Complete(false);
                    return;
                }

                registeredAssemblies++;
                m_progRegistration.Increment("SUCCESS: Plugin Assembly was registered");
            }
            else if (m_currentAssembly == null)
            {
                ignoredAssemblies++;
                m_progRegistration.Increment("INFORMATION: Assembly was not registered");
            }
            else
            {
                if (chkUpdateAssembly.Checked)
                {
                    m_progRegistration.Increment("SUCCESS: Assembly was updated");
                }
                else
                {
                    m_progRegistration.Increment("INFORMATION: Assembly was not updated");
                }
            }

            //Check to see if the assembly needs to be added to the list
            if (!m_org.Assemblies.ContainsKey(assembly.AssemblyId))
            {
                m_org.AddAssembly(assembly);

                //Update the Main Form
                try
                {
                    m_orgControl.AddAssembly(assembly);
                    m_progRegistration.Increment();
                }
                catch (Exception ex)
                {
                    m_progRegistration.Increment("ERROR: Error occurred while updating the Main form for the assembly");

                    ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                    m_progRegistration.Complete(false);
                    return;
                }
            }
            else
            {
                m_progRegistration.Increment();
            }

            // Register the Plugin
            bool createPlugin;
            int registeredPlugins = 0;
            int ignoredPlugins = 0;
            int errorsPlugins = 0;

            foreach (var currentPlugin in registerPluginList)
            {
                if (currentPlugin == null) continue;

                currentPlugin.AssemblyId = assembly.AssemblyId;

                //Check if the plugin exists
                bool pluginUpdate = m_registeredPluginList != null && m_registeredPluginList.Any(x => x.TypeName.ToLowerInvariant() == currentPlugin.TypeName.ToLowerInvariant());
                try
                {
                    Guid pluginTypeId = Guid.Empty;

                    if (pluginUpdate || (!createAssembly && RegistrationHelper.PluginExists(m_org, currentPlugin.TypeName, assembly.AssemblyId, out pluginTypeId)))
                    {
                        if (pluginUpdate)
                        {
                            createPlugin = false;
                        }
                        else
                        {
                            m_progRegistration.AppendText($"INFORMATION: Plugin Type Name is already being used by PluginType {pluginTypeId}.");

                            switch (MessageBox.Show($"The specified name \"{currentPlugin.TypeName}\" is already registered. Skip the registration of this plugin?\n\nPlease note the plugins may not be the same.",
                                "Plugin Already Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                            {
                                case DialogResult.Yes:
                                    createPlugin = false;

                                    currentPlugin.PluginId = pluginTypeId;
                                    currentPlugin.Organization = assembly.Organization;
                                    break;

                                case DialogResult.No:
                                    createPlugin = true;
                                    break;

                                case DialogResult.Cancel:
                                    m_progRegistration.AppendText("ABORTED: Plugin Registration has been aborted by the user.");
                                    m_progRegistration.Complete(false);
                                    return;

                                default:
                                    throw new NotImplementedException();
                            }
                        }

                        m_progRegistration.Increment();
                    }
                    else
                    {
                        createPlugin = true;
                        m_progRegistration.Increment();
                    }
                }
                catch (Exception ex)
                {
                    m_progRegistration.Increment($"ERROR: Occurred while checking if {currentPlugin.TypeName} is already registered.");

                    ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                    m_progRegistration.Complete(false);
                    return;
                }

                //Create the plugin (if necessary)
                if (createPlugin)
                {
                    try
                    {
                        Guid pluginId = currentPlugin.PluginId;
                        currentPlugin.PluginId = RegistrationHelper.RegisterPlugin(m_org, currentPlugin);
                        currentPlugin.Organization = m_org;

                        if (pluginId != currentPlugin.PluginId && assembly.Plugins.ContainsKey(pluginId))
                        {
                            assembly.RemovePlugin(pluginId);
                        }

                        retrieveDateList.Add(currentPlugin);

                        m_progRegistration.Increment($"SUCCESS: Plugin {currentPlugin.TypeName} was registered.");

                        registeredPlugins++;
                    }
                    catch (Exception ex)
                    {
                        m_progRegistration.Increment(2, $"ERROR: Occurred while registering {currentPlugin.TypeName}.");

                        ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                        errorsPlugins++;
                        continue;
                    }
                }
                else
                {
                    if (!pluginUpdate)
                    {
                        ignoredPlugins++;
                    }

                    m_progRegistration.Increment();
                }

                //Check if the plugin needs to be added to the list
                if (!assembly.Plugins.ContainsKey(currentPlugin.PluginId))
                {
                    assembly.AddPlugin(currentPlugin);

                    //Update the main form
                    try
                    {
                        m_orgControl.AddPlugin(currentPlugin);
                        m_progRegistration.Increment();
                    }
                    catch (Exception ex)
                    {
                        m_progRegistration.Increment($"ERROR: Occurred while updating the Main form for {currentPlugin.TypeName}.");

                        ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                        m_progRegistration.Complete(false);
                        return;
                    }
                }
                else
                {
                    m_progRegistration.Increment();
                }
            }

            // Unregister plugins that were unchecked
            int updatedPlugins = 0;
            foreach (var currectPlugin in removedPluginList)
            {
                //Check if the plugin exists
                try
                {
                    RegistrationHelper.Unregister(m_org, m_progRegistration, currectPlugin);
                    m_progRegistration.Increment(3, $"SUCCESS: Plugin {currectPlugin.TypeName} was unregistered.");
                    m_orgControl.RemovePlugin(currectPlugin.PluginId);

                    updatedPlugins++;
                }
                catch (Exception ex)
                {
                    m_progRegistration.Increment(3, $"ERROR: Occurred while unregistering {currectPlugin.TypeName}.");

                    ErrorMessageForm.ShowErrorMessageBox(this, ERROR_MESSAGE, ERROR_CAPTION, ex);

                    errorsPlugins++;
                }
            }

            //Update the entities whose Created On / Modified On dates changed
            try
            {
                OrganizationHelper.UpdateDates(m_org, retrieveDateList);
                m_progRegistration.Increment("SUCCESS: Created On / Modified On dates updated");
            }
            catch (Exception ex)
            {
                m_progRegistration.Increment("ERROR: Unable to update Created On / Modified On dates");

                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to update Created On / Modified On dates", "Update Error", ex);
            }

            #endregion Register Plugin

            m_progRegistration.AppendText("SUCCESS: Selected Plugins have been registered");
            m_progRegistration.Complete(false);

            MessageBox.Show(string.Format("The selected Plugins have been registered.\n{0} Assembly Registered\n{1} Assembly Ignored\n{2} Assembly Updated\n{3} Plugin(s) Registered\n{4} Plugin(s) Ignored\n{5} Plugin(s) Encountered Errors\n{6} Plugin(s) Removed",
                registeredAssemblies, ignoredAssemblies, updatedAssemblies, registeredPlugins, ignoredPlugins, errorsPlugins, updatedPlugins),
                "Registered Plugins", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (errorsPlugins == 0)
            {
                Close();
            }
        }

        private void chkSelectAll_Click(object sender, EventArgs e)
        {
            trvPlugins.CheckAllNodes(chkSelectAll.Checked);
        }

        private void EnableRegistrationControls()
        {
            //Check if there are any nodes in the tree. If there are, the assembly must have been loaded or it is an update
            if (m_assemblyLoaded)
            {
                btnRegister.Enabled = true;
                grpRegLoc.Enabled = true;
            }
            else if (null != m_currentAssembly)
            {
                btnRegister.Enabled = true;
                grpRegLoc.Enabled = false;
            }
            else
            {
                btnRegister.Enabled = false;
                grpRegLoc.Enabled = true;
            }
        }

        private CrmAssemblySourceType GetAssemblySourceType()
        {
            if (radDB.Checked)
            {
                return CrmAssemblySourceType.Database;
            }
            else if (radDisk.Checked)
            {
                return CrmAssemblySourceType.Disk;
            }
            else if (radGAC.Checked)
            {
                return CrmAssemblySourceType.GAC;
            }
            else
            {
                throw new NotImplementedException("Unknown Source Type");
            }
        }

        private CrmAssemblyIsolationMode GetIsolationMode()
        {
            if (radIsolationNone.Checked)
            {
                return CrmAssemblyIsolationMode.None;
            }
            else if (radIsolationSandbox.Checked)
            {
                return CrmAssemblyIsolationMode.Sandbox;
            }
            else
            {
                throw new NotImplementedException("Unknown Isolation Mode");
            }
        }

        private void LoadAssembly(CrmPluginAssembly assembly, bool checkExisting)
        {
            bool loadTypeId = (m_currentAssembly != null && !checkExisting && m_registeredPluginList != null);
            bool checkRecord = (checkExisting && m_currentAssembly != null && m_registeredPluginList != null);

            //Loop through and add the data to the form
            trvPlugins.LoadNodes(new List<ICrmTreeNode> { assembly });

            foreach (var currentPlugin in assembly.Plugins.Values)
            {
                if (loadTypeId && !m_registeredPluginList.Any(x => x.TypeName.ToLowerInvariant() == currentPlugin.TypeName.ToLowerInvariant()))
                {
                    m_registeredPluginList.Add(currentPlugin);
                }

                if (!checkRecord || m_registeredPluginList.Any(x => x.TypeName.ToLowerInvariant() == currentPlugin.TypeName.ToLowerInvariant()))
                {
                    trvPlugins.CheckNode(currentPlugin.PluginId, true);
                }
                else
                {
                    trvPlugins.CheckNode(currentPlugin.PluginId, false);
                    chkSelectAll.Checked = false;
                }
            }
        }

        private void ProgressIndicatorAddText(string message)
        {
            if (txtProgress.TextLength == 0)
            {
                txtProgress.Text = message;
            }
            else
            {
                txtProgress.AppendText(string.Format("\r\n{0}", message));
            }

            txtProgress.SelectionStart = txtProgress.TextLength;
            txtProgress.SelectionLength = 0;
        }

        private void ProgressIndicatorComplete()
        {
            grpPath.Enabled = true;
            grpPlugins.Enabled = true;
            grpIsolationMode.Enabled = true;
            grpRegLoc.Enabled = true;
            EnableRegistrationControls();
            btnCancel.Enabled = true;

            barRegistration.Value = barRegistration.Minimum;
        }

        private void ProgressIndicatorIncrement(int value)
        {
            barRegistration.Value += value;
        }

        private void ProgressIndicatorInit(int min, int pluginCount, int initialValue)
        {
            const int STEPS_REGISTRATION_SETUP = 0;
            const int STEPS_REGISTRATION_ASSEMBLY = 3;
            const int STEPS_REGISTRATION_PLUGIN = 3;
            const int STEPS_REGISTRATION_FINAL = 1;

            //Total number of plugin steps:
            //- Initial Steps
            int totalSteps = STEPS_REGISTRATION_SETUP +

                //- Check Each Assembly Exist
                //- Register Each Assembly
                //- Update Main Form
                STEPS_REGISTRATION_ASSEMBLY +

                //- Check Each Plugin Exist
                //- Register Each Plugin (or Skip)
                //- Update Main Form
                (STEPS_REGISTRATION_PLUGIN * pluginCount) +

                //- Final Steps
                STEPS_REGISTRATION_FINAL;

            if (totalSteps <= 0)
            {
                m_progRegistration.ClearText();
                return;
            }

            //Update the progress bar
            barRegistration.Minimum = min;
            barRegistration.Value = initialValue;
            barRegistration.Maximum = totalSteps;
            txtProgress.Clear();

            grpPath.Enabled = false;
            grpPlugins.Enabled = false;
            grpIsolationMode.Enabled = false;
            grpRegLoc.Enabled = false;
            btnRegister.Enabled = false;
            btnCancel.Enabled = false;
        }

        private void ProgressIndicatorSetText(string message)
        {
            txtProgress.Text = message;
        }

        private void radDisk_CheckedChanged(object sender, EventArgs e)
        {
            lblServerFileName.Enabled = radDisk.Checked;
            txtServerFileName.Enabled = radDisk.Checked;
        }

        private void trvPlugins_CheckStateChanged(object sender, CrmTreeNodeEventArgs e)
        {
            chkSelectAll.Checked = trvPlugins.AllNodesChecked;
        }

        #endregion Private Methods
    }
}
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

using Microsoft.Crm.Sdk.Messages;

namespace Xrm.Sdk.PluginRegistration
{
    using Controls;
    using CsvHelper;
    using Forms;
    using Helpers;
    using McTools.Xrm.Connection;
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using OfficeOpenXml;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using Wrappers;
    using Xrm.Sdk.PluginRegistration.Extensions;
    using Xrm.Sdk.PluginRegistration.Models;
    using XrmToolBox.Extensibility;
    using XrmToolBox.Extensibility.Args;
    using XrmToolBox.Extensibility.Interfaces;

    public partial class MainControl : PluginControlBase, IStatusBarMessenger, IGitHubPlugin, IShortcutReceiver, IAboutPlugin
    {
        #region Private Fields

        private const string SYSTEM_ERROR_CAPTION = "Microsoft Dynamics CRM";
        private const string SYSTEM_ERROR_MESSAGE = "The selected item is required for the Microsoft Dynamics CRM system to work correctly.";
        private const string EPPLUS_LICENSE_ORG_NAME = "Plugin Registration Tool in XrmToolBox";

        private const string ENABLE_ALL_STEPS_CONFIRMATION = "Are you sure you want to enable all steps for the selected item?";
        private const string DISABLE_ALL_STEPS_CONFIRMATION = "Are you sure you want to disable all steps for the selected item?";
        private const string ENABLE_ALL_STEPS_CAPTION = "Enable All Steps";
        private const string DISABLE_ALL_STEPS_CAPTION = "Disable All Steps";

        private static CrmEntitySorter m_entitySorter;
        private ConnectionDetail m_con;
        private CrmViewType m_currentView;
        private Guid m_dataRowId = Guid.Empty;
        private int m_dataRowIndex = -1;
        private CrmOrganization m_org;
        private ProgressIndicator m_progressIndicator = null;
        private Dictionary<string, CrmTreeNode> m_rootNodeList = null;
        private Settings m_settings;
        private Dictionary<Guid, Guid> m_stepEntityMap = new Dictionary<Guid, Guid>();
        private Dictionary<Guid, Guid> m_stepParentList = null;
        private Dictionary<Guid, Guid> m_viewNodeList = null;

        #endregion Private Fields

        #region Public Constructors

        public MainControl()
        {
            InitializeComponent();

            SetFilterStatusOnToolbar(!string.IsNullOrEmpty(m_settings?.ExcludedAssemblies) || (m_settings?.ExcludeManagedAssemblies ?? false));

            #region Load the Images & Icons from the Resource File

            Dictionary<CrmTreeNodeImageType, Image> nodeImageList = null;
            try
            {
                nodeImageList = CrmResources.LoadImage(
                    CrmTreeNodeImageType.Assembly, CrmTreeNodeImageType.Image,
                    CrmTreeNodeImageType.Message, CrmTreeNodeImageType.MessageEntity,
                    CrmTreeNodeImageType.StepDisabled, CrmTreeNodeImageType.StepEnabled,
                    CrmTreeNodeImageType.ServiceEndpoint, CrmTreeNodeImageType.Package);

                toolServiceEndpointRegister.Image = nodeImageList[CrmTreeNodeImageType.ServiceEndpoint];

                toolPackageRegister.Image = nodeImageList[CrmTreeNodeImageType.Package];

                toolAssemblyRegister.Image = nodeImageList[CrmTreeNodeImageType.Assembly];
                mnuContextNodeAssemblyRegister.Image = toolAssemblyRegister.Image;
                mnuContextGeneralAssemblyRegister.Image = toolAssemblyRegister.Image;

                toolStepRegister.Image = nodeImageList[CrmTreeNodeImageType.StepEnabled];
                mnuContextNodeStepRegister.Image = toolStepRegister.Image;
                mnuContextGeneralStepRegister.Image = toolStepRegister.Image;

                toolImageRegister.Image = nodeImageList[CrmTreeNodeImageType.Image];
                mnuContextNodeImageRegister.Image = toolImageRegister.Image;
                mnuContextGeneralImageRegister.Image = toolImageRegister.Image;

                toolViewAssembly.Image = toolAssemblyRegister.Image;
                toolViewEntity.Image = nodeImageList[CrmTreeNodeImageType.MessageEntity];
                toolViewMessage.Image = nodeImageList[CrmTreeNodeImageType.Message];
                toolViewPackage.Image = nodeImageList[CrmTreeNodeImageType.Package];

                toolWebHookRegister.Image = nodeImageList[CrmTreeNodeImageType.ServiceEndpoint];

                imlEnableImages.Images.Add("enableStep", nodeImageList[CrmTreeNodeImageType.StepEnabled]);
                imlEnableImages.Images.Add("disableStep", nodeImageList[CrmTreeNodeImageType.StepDisabled]);

                UpdateEnableButton(true);

                var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainControl));
                //var resources = new ResourceManager("Images", this.GetType().Assembly);
                this.PluginIcon = ((System.Drawing.Icon)(resources.GetObject("TabIcon")));
            }
            catch (Exception)
            {
                if (nodeImageList != null)
                {
                    var imgList = nodeImageList.Values.ToList();
                    for (int i = 0; i < imgList.Count; i++)
                    {
                        imgList[i]?.Dispose();
                    }
                }

                throw;
            }

            Dictionary<string, Image> imageList = null;
            try
            {
                imageList = CrmResources.LoadImage(
                    "ImportExport",
                    "EditLabel",
                    "Update",
                    "Register",
                    "Refresh",
                    "Delete",
                    "Import",
                    "Export",
                    "View",
                    "Search",
                    "Errors",
                    "InstallProfiler",
                    "EnableProfiler",
                    "DisableProfiler",
                    "UninstallProfiler",
                    "Debug",
                    "Close",
                    "Save",
                    "Filter",
                    "Solutions",
                    "layers");

                toolRegister.Image = imageList["Register"];
                toolView.Image = imageList["View"];

                toolUpdate.Image = imageList["Update"];
                mnuContextNodeUpdate.Image = toolUpdate.Image;

                toolUnregister.Image = imageList["Delete"];
                mnuContextNodeUnregister.Image = toolUnregister.Image;

                toolAddToSolution.Image = imageList["Solutions"];
                tsmiAddToSolution.Image = toolAddToSolution.Image;

                tsmiCheckActiveLayer.Image = imageList["layers"];

                toolSearch.Image = imageList["Search"];
                mnuContextNodeSearch.Image = toolSearch.Image;
                mnuContextGeneralSearch.Image = toolSearch.Image;

                toolRefresh.Image = imageList["Refresh"];
                mnuContextNodeRefresh.Image = toolRefresh.Image;
                mnuContextGeneralRefresh.Image = toolRefresh.Image;

                toolClose.Image = imageList["Close"];
                toolExport.Image = imageList["Save"];

                imlEnableImages.Images.Add("installProfiler", imageList["InstallProfiler"]);
                imlEnableImages.Images.Add("enableProfiler", imageList["EnableProfiler"]);
                imlEnableImages.Images.Add("disableProfiler", imageList["DisableProfiler"]);
                imlEnableImages.Images.Add("uninstallProfiler", imageList["UninstallProfiler"]);
            }
            catch (Exception)
            {
                var imgList = imageList.Values.ToList();
                for (int i = 0; i < imgList.Count; i++)
                {
                    imgList[i]?.Dispose();
                }
                throw;
            }

            #endregion Load the Images & Icons from the Resource File

            //Set the view types on the menu items. The Designer's auto code generation keeps overwriting this
            toolViewAssembly.Tag = CrmViewType.Assembly;
            toolViewEntity.Tag = CrmViewType.Entity;
            toolViewMessage.Tag = CrmViewType.Message;
            toolViewPackage.Tag = CrmViewType.Package;

            if (m_entitySorter == null)
            {
                m_entitySorter = new CrmEntitySorter();
            }
            trvPlugins.CrmTreeNodeSorter = m_entitySorter;

            //this.RefreshProfilerGeneralMenu();

            //Update the shortcut keys
            mnuContextNodeAssemblyRegister.ShortcutKeys = toolAssemblyRegister.ShortcutKeys;
            mnuContextGeneralAssemblyRegister.ShortcutKeys = toolAssemblyRegister.ShortcutKeys;

            mnuContextNodeStepRegister.ShortcutKeys = toolStepRegister.ShortcutKeys;
            mnuContextGeneralStepRegister.ShortcutKeys = toolStepRegister.ShortcutKeys;

            mnuContextNodeImageRegister.ShortcutKeys = toolImageRegister.ShortcutKeys;
            mnuContextGeneralImageRegister.ShortcutKeys = toolImageRegister.ShortcutKeys;

            mnuContextNodeRefresh.ShortcutKeys = mnuContextGeneralRefresh.ShortcutKeys;

            mnuContextNodeSearch.ShortcutKeys = mnuContextGeneralSearch.ShortcutKeys;

            mnuContextNodeSearch.ShortcutKeys = mnuContextGeneralSearch.ShortcutKeys;

            //Setup splitter panel 2 min distance because form designer keeps putting properties in incorrect order
            splitterDisplay.Panel2MinSize = 230;

            ConnectionUpdated += OrganizationControl_ConnectionUpdated;

            m_progressIndicator = new ProgressIndicator(new Action<StatusBarMessageEventArgs>((message) =>
            {
                if (SendMessageToStatusBar != null)
                {
                    SendMessageToStatusBar(this, message);
                }
            }));
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler<StatusBarMessageEventArgs> SendMessageToStatusBar;

        #endregion Public Events

        #region Private Enums

        private enum CrmViewType
        {
            Assembly,
            Message,
            Entity,
            Package
        }

        #endregion Private Enums

        #region Public Properties

        public ConnectionDetail Connection
        {
            get
            {
                return m_con;
            }
        }

        public IComparer<ICrmTreeNode> CrmTreeNodeSorter
        {
            get
            {
                return m_entitySorter;
            }
        }

        public bool IsAutoExpanded
        {
            get
            {
                return trvPlugins.AutoExpand;
            }
        }

        public CrmOrganization Organization
        {
            get
            {
                return m_org;
            }
        }

        public string RepositoryName => "PluginRegistration";

        public string UserName => "Innofactor";

        #endregion Public Properties

        #region Public Methods

        public void AddAssembly(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(Guid.Empty, assembly);
            }
        }

        public void AddImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            trvPlugins.AddNode(image.StepId, image);
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == image.StepId)
            {
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void AddPackage(CrmPluginPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (m_currentView == CrmViewType.Package)
            {
                trvPlugins.AddNode(Guid.Empty, package);
            }
        }

        public void AddPlugin(CrmPlugin reg)
        {
            if (reg == null)
            {
                throw new ArgumentNullException("reg");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(reg.AssemblyId, reg);
                if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == reg.AssemblyId)
                {
                    SelectItem(trvPlugins.SelectedNode);
                }
            }
        }

        public void AddServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.AddNode(serviceEndpoint.ServiceEndpointId, serviceEndpoint);
            }
        }

        public void AddStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            Guid parentId;
            switch (m_currentView)
            {
                case CrmViewType.Assembly:
                    if (step.ServiceBusConfigurationId == Guid.Empty)
                    {
                        parentId = step.PluginId;
                    }
                    else
                    {
                        parentId = step.ServiceBusConfigurationId;
                    }

                    break;

                case CrmViewType.Package:
                    parentId = step.PluginId;

                    break;

                case CrmViewType.Entity:
                case CrmViewType.Message:
                    parentId = CreateCrmTreeNodes(m_currentView, step.MessageId, step.MessageEntityId, true).NodeId;

                    //Add to the step parent id list
                    if (!m_stepParentList.ContainsKey(step.StepId))
                    {
                        m_stepParentList.Add(step.StepId, parentId);
                    }

                    //Add to the CrmTreeNode
                    ((CrmTreeNode)trvPlugins[parentId]).AddChild(step);
                    break;

                default:
                    throw new NotImplementedException($"View = {m_currentView.ToString()}");
            }

            trvPlugins.AddNode(parentId, step);
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeId == parentId)
            {
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void Init(CrmOrganization org)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (org.ConnectionDetail == null)
            {
                throw new ArgumentNullException("org.ConnectionDetail");
            }

            toolWebHookRegister.Visible = new Version(org.ConnectionDetail.OrganizationVersion) >= new Version(9, 0);

            m_org = org;
            m_con = org.ConnectionDetail;
            m_currentView = CrmViewType.Assembly;
            LoadNodes();
        }

        public bool IsNodeSystemItem(ICrmTreeNode node)
        {
            var internalNode = node as CrmTreeNode;
            if (null != internalNode)
            {
                return true;
            }

            var entity = node as ICrmEntity;
            if (null != entity)
            {
                return entity.IsSystemCrmEntity;
            }
            else
            {
                return false;
            }
        }

        public void ReceiveKeyDownShortcut(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && mnuContextNodeUnregister.Enabled && !propGridEntity.ContainsFocus)
            {
                mnuContextNodeUnregister.PerformClick();
            }
            else if (e.KeyCode == Keys.F5 && mnuContextGeneralRefresh.Enabled)
            {
                mnuContextGeneralRefresh.PerformClick();
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.F && mnuContextGeneralSearch.Enabled)
            {
                mnuContextGeneralSearch.PerformClick();
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.A && toolAssemblyRegister.Enabled)
            {
                toolAssemblyRegister.PerformClick();
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.T && toolStepRegister.Enabled)
            {
                toolStepRegister.PerformClick();
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.I && toolImageRegister.Enabled)
            {
                toolImageRegister.PerformClick();
            }
            else if (e.Control && !e.Shift && e.KeyCode == Keys.E && toolServiceEndpointRegister.Enabled)
            {
                toolServiceEndpointRegister.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.P && toolViewPackage.Available && toolViewPackage.Enabled)
            {
                toolViewPackage.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.A && toolViewAssembly.Enabled)
            {
                toolViewAssembly.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E && toolViewEntity.Enabled)
            {
                toolViewEntity.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.M && toolViewMessage.Enabled)
            {
                toolViewMessage.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E && toolViewEntity.Enabled)
            {
                toolViewEntity.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E && toolViewEntity.Enabled)
            {
                toolViewEntity.PerformClick();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.E && toolViewEntity.Enabled)
            {
                toolViewEntity.PerformClick();
            }
        }

        public void ReceiveKeyPressShortcut(KeyPressEventArgs e)
        {
        }

        public void ReceiveKeyUpShortcut(KeyEventArgs e)
        {
        }

        public void ReceivePreviewKeyDownShortcut(PreviewKeyDownEventArgs e)
        {
        }

        public void RefreshAssembly(CrmPluginAssembly assembly, bool reloadChildren)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RefreshNode(assembly.NodeId, reloadChildren);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RefreshFullTreeView()
        {
            var instruction = new WorkAsyncInfo()
            {
                Message = "Refreshing assemblies information...",
                Work = (worker, argument) =>
                {
                    try
                    {
                        // Clear the static message cache before reloading messages
                        OrganizationHelper.ClearMessageCache();
                        OrganizationHelper.RefreshConnection(m_org, m_settings, OrganizationHelper.LoadMessages(m_org), m_progressIndicator);
                        Invoke(new Action(() =>
                        {
                            propGridEntity.SelectedObject = null;
                            LoadNodes();
                        }));
                    }
                    catch (Exception ex)
                    {
                        Invoke(new Action(() =>
                        {
                            ErrorMessageForm.ShowErrorMessageBox(this, "Unable to refresh the organization. Connection must close.", "Connection Error", ex);
                        }));
                    }
                }
            };
            WorkAsync(instruction);
        }

        public void RefreshImage(CrmPluginImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            trvPlugins.RefreshNode(image.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RefreshPackage(CrmPluginPackage package, bool reloadChildren)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (m_currentView == CrmViewType.Package)
            {
                trvPlugins.RefreshNode(package.NodeId, reloadChildren);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RefreshPlugin(CrmPlugin plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RefreshNode(plugin.NodeId, true);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RefreshServiceEndpoint(CrmServiceEndpoint serviceEndpoint)
        {
            if (serviceEndpoint == null)
            {
                throw new ArgumentNullException("serviceEndpoint");
            }

            trvPlugins.RefreshNode(serviceEndpoint.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RefreshStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }

            trvPlugins.RefreshNode(step.NodeId, true);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RemoveAssembly(Guid assemblyId)
        {
            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RemoveNode(assemblyId);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RemoveImage(Guid imageId)
        {
            trvPlugins.RemoveNode(imageId);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void RemovePlugin(Guid pluginId)
        {
            if (m_currentView == CrmViewType.Assembly)
            {
                trvPlugins.RemoveNode(pluginId);
                SelectItem(trvPlugins.SelectedNode);
            }
        }

        public void RemoveStep(Guid stepId)
        {
            var step = (CrmPluginStep)trvPlugins[stepId];

            trvPlugins.RemoveNode(stepId);
            SelectItem(trvPlugins.SelectedNode);
        }

        public void SelectNode(Guid nodeId)
        {
            if (!trvPlugins.HasNode(nodeId))
            {
                throw new ArgumentException("Node is not in the tree", "nodeId");
            }

            trvPlugins.SelectedNode = trvPlugins[nodeId];
        }

        public void ShowAboutDialog()
        {
            try
            {
                var about = new About();
                //StartPosition = FormStartPosition.CenterParent

                about.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void ShowSystemItemError(string text)
        {
            if (text == null)
            {
                MessageBox.Show(SYSTEM_ERROR_MESSAGE, SYSTEM_ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(string.Format("{0}\n{1}", SYSTEM_ERROR_MESSAGE, text),
                    SYSTEM_ERROR_CAPTION, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateAutoExpand(bool newValue)
        {
            trvPlugins.AutoExpand = newValue;
        }

        #endregion Public Methods

        #region Private Methods

        private static bool DeleteExistingFile(FileInfo fileInfo)
        {
            try
            {
                fileInfo.Delete();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static CsvWriter InitializeCsvWriter(string filePath)
        {
            var writer = new StreamWriter(filePath, false, Encoding.Default);
            var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

            csv.WriteHeader(typeof(ExportModel));
            csv.NextRecord();
            return csv;
        }

        private static void OpenExportedFile(string filePath)
        {
            if (MessageBox.Show("Would you like to open the saved file?", "File saved successfully", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Process.Start(filePath);
            }
        }

        /// <summary>
        /// Get full path of the file specified in the SaveFileDialog, in case of cancel it returns null
        /// </summary>
        /// <returns></returns>
        private static string ShowSaveFileDialog()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Comma Separated Values File|*.csv|Excel File Format|*.xlsx",
                FilterIndex = 2,
                RestoreDirectory = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                return Path.GetFullPath(saveFileDialog.FileName);
            }
            else
            {
                return null;
            }
        }

        private static bool VerifySelectedNode(ICrmTreeNode node)
        {
            return (node == null || node.NodeType == CrmTreeNodeType.Step
                || node.NodeType == CrmTreeNodeType.Image || node.NodeType == CrmTreeNodeType.ServiceEndpoint
                || node.NodeType == CrmTreeNodeType.Message || node.NodeType == CrmTreeNodeType.MessageEntity
                || node.NodeType == CrmTreeNodeType.Connection || node.NodeType == CrmTreeNodeType.None);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (trvPlugins != null && trvPlugins.SelectedNode != null)
                {
                    switch (trvPlugins.SelectedNode.NodeType)
                    {
                        case CrmTreeNodeType.Assembly:
                            CrmPluginAssembly assembly = (CrmPluginAssembly)trvPlugins.SelectedNode;
                            RegistrationHelper.UpdateAssembly(m_org, assembly.Description, assembly.AssemblyId);
                            trvPlugins.RefreshNode(assembly.AssemblyId, false, false);
                            MessageBox.Show("Assembly has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;

                        case CrmTreeNodeType.Plugin:
                        case CrmTreeNodeType.WorkflowActivity:
                            CrmPlugin plugin = (CrmPlugin)trvPlugins.SelectedNode;
                            RegistrationHelper.UpdatePlugin(m_org, plugin);
                            trvPlugins.RefreshNode(plugin.PluginId, false, false);
                            MessageBox.Show("Plug-in has been updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;

                        default:
                            MessageBox.Show("A valid object has not been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("A valid object has not been selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to Update the Assembly /Plugin due to an error.", "Update", ex);
            }
        }

        private CrmTreeNode CreateCrmTreeNodes(CrmViewType view, Guid messageId, Guid messageEntityId, bool addToTree)
        {
            CrmTreeNode rootNode = null, childNode = null;

            if (Guid.Empty == messageId)
            {
                throw new ArgumentException("Invalid Guid", "messageId");
            }

            switch (view)
            {
                case CrmViewType.Message:
                    {
                        if (!m_org.Messages.ContainsKey(messageId))
                        {
                            return null;
                        }
                        rootNode = new CrmTreeNode(m_org.Messages[messageId]);
                        if (Guid.Empty == messageEntityId)
                        {
                            childNode = new CrmTreeNode(new CrmMessageEntity(Organization, messageId, Guid.Empty,
                                "none", "none", CrmPluginStepDeployment.Both, null, null));
                        }
                        else
                        {
                            if (!m_org.MessageEntities.ContainsKey(messageEntityId))
                            {
                                return null;
                            }
                            childNode = new CrmTreeNode(m_org.MessageEntities[messageEntityId]);
                        }
                    }
                    break;

                case CrmViewType.Entity:
                    {
                        if (Guid.Empty == messageEntityId)
                        {
                            rootNode = new CrmTreeNode(new CrmMessageEntity(Organization, messageId, Guid.Empty, "none", "none",
                                CrmPluginStepDeployment.Both, null, null));
                        }
                        else
                        {
                            if (!m_org.MessageEntities.ContainsKey(messageEntityId))
                            {
                                return null;
                            }
                            rootNode = new CrmTreeNode(m_org.MessageEntities[messageEntityId]);
                        }
                        if (!m_org.Messages.ContainsKey(messageId))
                        {
                            return null;
                        }
                        childNode = new CrmTreeNode(m_org.Messages[messageId]);
                    }
                    break;

                default:
                    throw new NotImplementedException($"View = {view.ToString()}");
            }

            Guid rootNodeId = rootNode.NodeId;
            Guid childNodeId = childNode.NodeId;

            if (m_rootNodeList.ContainsKey(rootNode.NodeText))
            {
                rootNode = m_rootNodeList[rootNode.NodeText];
            }
            else
            {
                rootNode.NodeId = Guid.NewGuid();
                m_rootNodeList.Add(rootNode.NodeText, rootNode);

                if (addToTree)
                {
                    trvPlugins.AddNode(Guid.Empty, rootNode);
                }
            }

            if (rootNode.HasChild(childNode.NodeText))
            {
                childNode = rootNode[childNode.NodeText];
            }
            else
            {
                childNode.NodeId = Guid.NewGuid();
                rootNode.AddChild(childNode);

                if (addToTree)
                {
                    trvPlugins.AddNode(rootNode.NodeId, childNode);
                }
            }

            if (!m_viewNodeList.ContainsKey(rootNodeId))
            {
                m_viewNodeList.Add(rootNodeId, rootNode.NodeId);
            }

            if (!m_viewNodeList.ContainsKey(childNodeId))
            {
                m_viewNodeList.Add(childNodeId, childNode.NodeId);
            }

            return childNode;
        }

        private void ExportSelected()
        {
            var node = trvPlugins.SelectedNode;

            if (VerifySelectedNode(node))
            {
                MessageBox.Show("Please select an assembly, plugin or workflow activity", "Invalid selection", MessageBoxButtons.OK);
                return;
            }

            var filePath = ShowSaveFileDialog();
            var fileInfo = new FileInfo(filePath);
            var model = new List<ExportModel>();
            if (string.IsNullOrEmpty(filePath))
            {
                //user cancelled on SaveFileDialog so exit and do nothing.
                return;
            }
            switch (node.NodeType)
            {
                case CrmTreeNodeType.Assembly:
                    {
                        model = ForEachAssemblyExport((CrmPluginAssembly)node);
                    }
                    break;

                case CrmTreeNodeType.Plugin:
                case CrmTreeNodeType.WorkflowActivity:
                    {
                        model = ForEachPluginExport((CrmPlugin)node);
                    }
                    break;
                case CrmTreeNodeType.WebHook:
                    {
                        model = ForEachWebhookExport((CrmServiceEndpoint)node);
                    }
                    break;
                default:
                    throw new NotImplementedException($"NodeType = {node.NodeType.ToString()}");
            }
            if (string.Equals(fileInfo.Extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ExcelPackage.License.SetNonCommercialOrganization(EPPLUS_LICENSE_ORG_NAME); 

                using (var xlPackage = new ExcelPackage(fileInfo))
                {
                    xlPackage.Workbook.Worksheets.Add($"{Organization.OrganizationFriendlyName}");
                    var worksheet = xlPackage.Workbook.Worksheets[0];

                    worksheet.Cells.LoadFromCollection(model, true, OfficeOpenXml.Table.TableStyles.Light8);
                    xlPackage.Save();
                }
            }
            else if (string.Equals(fileInfo.Extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                using (var csv = InitializeCsvWriter(filePath))
                {
                    csv.WriteRecords(model);
                }
            }

            OpenExportedFile(filePath);
        }

        private void ExportTool()
        {
            var filePath = ShowSaveFileDialog();
            if (string.IsNullOrEmpty(filePath))
            {
                //user cancelled on SaveFileDialog so exit and do nothing.
                return;
            }
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
            {
                if (!DeleteExistingFile(fileInfo))
                {
                    MessageBox.Show("Unable to overwrite existing file. Please try again with a unique name.");
                    return;
                }
            }

            var model = new List<ExportModel>();

            foreach (CrmPluginAssembly assembly in Organization.Assemblies.Where(x => ((CrmServiceEndpoint.ServiceBusPluginAssemblyName != x.Name
                                                                                        || 0 != x.CustomizationLevel) && !x.IsProfilerAssembly)).OrderBy(x => x.Name))
            {
                model.AddRange(ForEachAssemblyExport(assembly));
            }

            // Add webhooks export
            foreach (CrmServiceEndpoint webhook in Organization.Webhooks.OrderBy(x => x.Name))
            {
                model.AddRange(ForEachWebhookExport(webhook));
            }

            if (string.Equals(fileInfo.Extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ExcelPackage.License.SetNonCommercialOrganization(EPPLUS_LICENSE_ORG_NAME);

                using (var xlPackage = new ExcelPackage(fileInfo))
                {
                    xlPackage.Workbook.Worksheets.Add($"{Organization.OrganizationFriendlyName}");
                    var worksheet = xlPackage.Workbook.Worksheets[0];

                    worksheet.Cells.LoadFromCollection(model, true, OfficeOpenXml.Table.TableStyles.Light8);
                    xlPackage.Save();
                }
            }
            else if (string.Equals(fileInfo.Extension, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                using (var csv = InitializeCsvWriter(filePath))
                {
                    csv.WriteRecords(model);
                }
            }
            else
            {
            }
            OpenExportedFile(filePath);
        }

        private List<ExportModel> ForEachAssemblyExport(CrmPluginAssembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }
            var model = new List<ExportModel>();

            var assemblyInfo = new ExportModel
            {
                AssemblyName = assembly.Name,
                TypeName = string.Empty,
                PluginType = "Assembly",
                Description = assembly.Description,
                IsolationMode = assembly.IsolationMode.GetDescription(),
                SourceType = assembly.SourceType.GetDescription()
            };
            model.Add(assemblyInfo);
            foreach (var node in assembly.NodeChildren)
            {
                model.AddRange(ForEachPluginExport(node));
            }
            return model;
        }

        private List<ExportModel> ForEachPluginExport(ICrmTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            var csvModel = new List<ExportModel>();
            switch (node.NodeType)
            {
                case CrmTreeNodeType.Plugin:
                    var plugin = (CrmPlugin)node;
                    var pluginInfo = new ExportModel
                    {
                        AssemblyName = plugin.AssemblyName,
                        TypeName = plugin.Name,
                        PluginType = plugin.PluginType.GetDescription(),
                        Description = plugin.Description
                    };
                    csvModel.Add(pluginInfo);

                    foreach (CrmPluginStep step in plugin.Steps)
                    {
                        var stepInfo = GetInfoForStep(step);
                        if (stepInfo == null)
                        {
                            continue;
                        }
                        stepInfo.AssemblyName = plugin.AssemblyName;
                        stepInfo.TypeName = plugin.Name;
                        csvModel.Add(stepInfo);
                        foreach (var image in step.Images)
                        {
                            var imageInfo = GetInfoForImages(image);
                            imageInfo.TypeName= plugin.Name;
                            
                            csvModel.Add(imageInfo);
                        }
                    }
                    break;

                case CrmTreeNodeType.WorkflowActivity:
                    {
                        var workflow = (CrmPlugin)node;
                        var workflowInfo = new ExportModel
                        {
                            TypeName = workflow.Name,
                            AssemblyName = workflow.AssemblyName,
                            PluginType = workflow.PluginType.GetDescription(),
                            Description = workflow.Description
                        };
                        csvModel.Add(workflowInfo);
                    }
                    break;
            }
            return csvModel;
        }

        private List<ExportModel> ForEachWebhookExport(CrmServiceEndpoint webhook)
        {
            if (webhook == null)
            {
                throw new ArgumentNullException("webhook");
            }
            var model = new List<ExportModel>();

            // Webhook metadata row
            var webhookInfo = new ExportModel
            {
                AssemblyName = webhook.Name, // Use AssemblyName for webhook name for consistency
                TypeName = string.Empty,
                PluginType = "Webhook",
                Description = webhook.Description,
                IsolationMode = null, // Webhooks may not have isolation mode
                SourceType = "Webhook"
            };
            model.Add(webhookInfo);

            // Steps under webhook
            foreach (var step in webhook.Steps.Values)
            {
                var stepInfo = GetInfoForStep(step);
                if (stepInfo == null)
                {
                    continue;
                }
                stepInfo.AssemblyName = webhook.Name;
                stepInfo.TypeName = string.Empty;
                model.Add(stepInfo);
                // Images under step (if any)
                foreach (var image in step.Images)
                {
                    var imageInfo = GetInfoForImages(image);
                    imageInfo.TypeName = string.Empty;
                    model.Add(imageInfo);
                }
            }
            return model;
        }

        private ExportModel GetInfoForStep(CrmPluginStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException("step");
            }
            else
            {
                if (!Organization.Messages.ContainsKey(step.MessageId))
                {
                    return null;
                }
                var messageName = Organization.Messages[step.MessageId].Name;
                var message = m_org.FindMessage(messageName);
                var primaryEntity = "none";
                var secondaryEntity = "none";

                if (Organization.MessageEntities.ContainsKey(step.MessageEntityId))
                {
                    CrmMessageEntity msgEntity = message[step.MessageEntityId];

                    primaryEntity = msgEntity.PrimaryEntity;
                    secondaryEntity = msgEntity.SecondaryEntity;
                }
                var userContext = "Calling User";
                if(step.ImpersonatingUserId != Guid.Empty && Organization.Users.TryGetValue(step.ImpersonatingUserId, out CrmUser user))
                {
                    userContext = $"{user.Name}({user.UserId})";
                }
                
                var record = new ExportModel
                {
                    Stage = step.Stage.GetDescription(),
                    ExecutionMode = step.Mode.GetDescription(),
                    ExecutionOrder = step.Rank.ToString(),
                    Message = messageName,
                    FilteringAttributes = step.FilteringAttributes,
                    Deployment = step.Deployment.GetDescription(),
                    PrimaryEntity = primaryEntity,
                    SecondaryEntity = secondaryEntity,
                    Description = step.Description,
                    PluginType = step.NodeType.GetDescription(),
                    IsEnabled = step.Enabled.ToString(),
                    UserContext = userContext
                };

                return record;
            }
        }
        private ExportModel GetInfoForImages(CrmPluginImage image)
        {
            var record = new ExportModel
            {
                PluginType = "Image",
                ImageName = image.Name,
                ImageType = image.ImageType.GetDescription(),
                ImageAttributes = !String.IsNullOrWhiteSpace(image.Attributes)? image.Attributes: "All Attributes",
                EntityAlias = image.EntityAlias
            };

            return record;
        }

        private void grvData_DoubleClick(object sender, EventArgs e)
        {
            if (trvPlugins.HasNode(m_dataRowId))
            {
                trvPlugins.SelectedNode = trvPlugins[m_dataRowId];
            }
        }

        private void grvData_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == m_dataRowIndex)
            {
                return;
            }
            m_dataRowIndex = e.RowIndex;

            Guid rowId = (Guid)grvData.Rows[m_dataRowIndex].Cells["Id"].Value;
            m_dataRowId = rowId;

            //Check for special cases
            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Message:
                case CrmTreeNodeType.MessageEntity:

                    //The Id presented in the DataGrid is not the id of the node in the tree
                    if (m_viewNodeList.ContainsKey(rowId))
                    {
                        m_dataRowId = m_viewNodeList[rowId];
                    }
                    break;
            }
        }

        private void LoadNodes()
        {
            LoadNodes(m_currentView);
        }

        private void LoadNodes(CrmViewType view)
        {
            ToolStripItem currentCheckedItem = null;
            ToolStripItem newCheckedItem = null;

            foreach (ToolStripItem item in toolView.DropDownItems)
            {
                if (item.Tag != null && item.Tag.GetType() == typeof(CrmViewType))
                {
                    var checkedProp = item.GetType().GetProperty("Checked", typeof(bool));
                    if (checkedProp != null)
                    {
                        if ((bool)checkedProp.GetValue(item, null))
                        {
                            currentCheckedItem = item;
                        }
                        else if ((CrmViewType)item.Tag == view)
                        {
                            newCheckedItem = item;
                        }
                    }
                }

                if (currentCheckedItem != null && newCheckedItem != null)
                {
                    break;
                }
            }

            // Create the new view
            Enabled = false;
            try
            {
                switch (view)
                {
                    case CrmViewType.Package:
                        m_rootNodeList = null;
                        m_stepParentList = null;
                        m_viewNodeList = null;

                        var pNodes = new List<ICrmTreeNode>();
                        foreach (CrmPluginPackage package in Organization.Packages)
                        {
                            pNodes.Add(package);
                        }

                        trvPlugins.LoadNodes(pNodes);
                        break;

                    case CrmViewType.Assembly:
                        m_rootNodeList = null;
                        m_stepParentList = null;
                        m_viewNodeList = null;

                        var nodes = new List<ICrmTreeNode>();
                        foreach (CrmPluginAssembly assembly in Organization.Assemblies)
                        {
                            // If the same assembly name used for any other custom plugin assembly then that need to be added
                            if ((CrmServiceEndpoint.ServiceBusPluginAssemblyName != assembly.Name || 0 != assembly.CustomizationLevel) &&
                                !assembly.IsProfilerAssembly)
                            {
                                nodes.Add(assembly);
                            }
                        }

                        nodes.AddRange(Organization.ServiceEndpoints.ToArray());
                        nodes.AddRange(Organization.Webhooks.ToArray());

                        if (null != Organization.ProfilerPlugin)
                        {
                            nodes.Add(Organization.ProfilerPlugin);
                        }

                        trvPlugins.LoadNodes(nodes);
                        break;

                    case CrmViewType.Entity:
                    case CrmViewType.Message:
                        {
                            //Create the Root Node List
                            if (null == m_rootNodeList)
                            {
                                m_rootNodeList = new Dictionary<string, CrmTreeNode>();
                            }
                            else
                            {
                                m_rootNodeList.Clear();
                            }

                            //Create the Step Parent Node List
                            if (null == m_stepParentList)
                            {
                                m_stepParentList = new Dictionary<Guid, Guid>();
                            }
                            else
                            {
                                m_stepParentList.Clear();
                            }

                            //Create the View Node List
                            if (null == m_viewNodeList)
                            {
                                m_viewNodeList = new Dictionary<Guid, Guid>();
                            }
                            else
                            {
                                m_viewNodeList.Clear();
                            }

                            //Retrieve the of steps
                            foreach (var step in Organization.Steps)
                            {
                                if (step.MessageId != Guid.Empty)
                                {
                                    CrmTreeNode parentNode = CreateCrmTreeNodes(view, step.MessageId, step.MessageEntityId, false);
                                    if (parentNode == null)
                                    {
                                        continue;
                                    }
                                    parentNode.AddChild(step);
                                    m_stepParentList.Add(step.StepId, parentNode.NodeId);
                                }
                            }

                            var nodeList = new List<CrmTreeNode>();
                            nodeList.AddRange(m_rootNodeList.Values);

                            trvPlugins.LoadNodes(nodeList.ToList<ICrmTreeNode>());
                        }
                        break;

                    default:
                        throw new NotImplementedException($"View = {view.ToString()}");
                }

                m_currentView = view;

                if (currentCheckedItem != null)
                {
                    currentCheckedItem.GetType().GetProperty("Checked",
                        typeof(bool)).SetValue(currentCheckedItem, false, null);
                }

                if (newCheckedItem != null)
                {
                    newCheckedItem.GetType().GetProperty("Checked",
                        typeof(bool)).SetValue(newCheckedItem, true, null);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to change the view", "View Error", ex);
            }
            finally
            {
                Enabled = true;
            }
        }

        private void OrganizationControl_ConnectionUpdated(object sender, ConnectionUpdatedEventArgs e)
        {
            if (!SettingsManager.Instance.TryLoad(GetType(), out m_settings, e.ConnectionDetail.ConnectionId.Value.ToString()))
            {
                if (SettingsManager.Instance.TryLoad(GetType(), out m_settings))
                {
                    SettingsManager.Instance.Save(GetType(), m_settings, e.ConnectionDetail.ConnectionId.Value.ToString());
                }
                else
                {
                    m_settings = new Settings();
                    SettingsManager.Instance.Save(GetType(), m_settings, e.ConnectionDetail.ConnectionId.Value.ToString());
                }
            }

            var instruction = new WorkAsyncInfo()
            {
                Message = "Loading assemblies information...",
                Work = (worker, argument) =>
                {
                    argument.Result = new CrmOrganization(ConnectionDetail, m_settings, m_progressIndicator, Service);
                },
                PostWorkCallBack = (argument) =>
                {
                    Init((CrmOrganization)argument.Result);

                    var isValidForPackage = e.ConnectionDetail.OrganizationMajorVersion == 9 && e.ConnectionDetail.OrganizationMinorVersion >= 2 || e.ConnectionDetail.OrganizationMajorVersion > 9;

                    toolPackageRegister.Visible = isValidForPackage;
                    toolViewPackage.Visible = isValidForPackage;
                }
            };

            WorkAsync(instruction);
        }

        /// <summary>
        /// Removes the given CrmTreeNode. If its parent does not have any other children, it will also be removed.
        /// </summary>
        /// <param name="nodeId">Node to be removed</param>
        private void RemoveCrmTreeNodesCascadeUp(CrmTreeNode node)
        {
            if (null == node)
            {
                throw new ArgumentNullException("node");
            }

            //Check if we are going to be removing this node
            while (null != node && 0 == node.ChildCount)
            {
                //Retrieve the parent node
                CrmTreeNode parentNode;
                if (Guid.Empty == node.ParentNodeId)
                {
                    parentNode = null;

                    //This is a root element, remove it from the list
                    if (m_rootNodeList.ContainsKey(node.NodeText))
                    {
                        m_rootNodeList.Remove(node.NodeText);
                    }
                }
                else
                {
                    parentNode = (CrmTreeNode)trvPlugins[node.ParentNodeId];
                    parentNode.RemoveChild(node.NodeText);
                }

                //Remove the node from the tree
                trvPlugins.RemoveNode(node.NodeId);

                //Set the current node to be the parent node
                node = parentNode;
            }
        }

        private void SelectItem(ICrmTreeNode node)
        {
            //Reset the visibility and enabled properties because we don't what is enabled
            toolUpdate.Visible = false;
            mnuContextNodeUpdate.Visible = false;
            toolAddToSolution.Visible = false;
            tsmiAddToSolution.Visible = false;
            tsmiCheckActiveLayer.Visible = false;
            toolStripSeparator3.Visible = false;

            toolUpdate.Enabled = false;
            mnuContextNodeUpdate.Enabled = false;

            mnuContextNodeEnableAllSteps.Visible = false;
            mnuContextNodeDisableAllSteps.Visible = false;
            toolEnableAllSteps.Visible = false;
            toolDisableAllSteps.Visible = false;

            toolEnable.Visible = false;
            mnuContextNodeEnable.Visible = false;

            bool isSystemNode = IsNodeSystemItem(trvPlugins.SelectedNode);
            if (node == null)
            {
                mnuContextNodeUnregister.Enabled = false;
                toolUnregister.Enabled = false;
                mnuContextNodeUnregister.Enabled = false;
                return;
            }
            else
            {
                //It should only be possible to unregister non-system components
                mnuContextNodeUnregister.Enabled = !isSystemNode;
                toolUnregister.Enabled = !isSystemNode;
            }

            DataTable gridTable = null;
            switch (node.NodeType)
            {
                case CrmTreeNodeType.ServiceEndpoint:
                    {
                        CrmServiceEndpoint serviceEndpoint = (CrmServiceEndpoint)node;

                        toolUpdate.Visible = true;
                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Visible = true;
                        mnuContextNodeUpdate.Enabled = true;
                        btnSave.Enabled = false;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns, serviceEndpoint.Steps);
                    }
                    break;

                case CrmTreeNodeType.Assembly:
                    if (!isSystemNode)
                    {
                        CrmPluginAssembly assembly = (CrmPluginAssembly)node;

                        toolUpdate.Visible = true;
                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Visible = true;
                        mnuContextNodeUpdate.Enabled = true;
                        tsmiAddToSolution.Visible = true;
                        tsmiCheckActiveLayer.Visible = true;
                        toolStripSeparator3.Visible = true;
                        toolAddToSolution.Visible = true;
                        btnSave.Enabled = true;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPlugin>(CrmPlugin.Columns, assembly.Plugins);

                        mnuContextNodeEnableAllSteps.Visible = true;
                        mnuContextNodeDisableAllSteps.Visible = true;
                    }
                    break;

                case CrmTreeNodeType.Package:
                    if (!isSystemNode)
                    {
                        CrmPluginPackage package = (CrmPluginPackage)node;

                        toolUpdate.Visible = true;
                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Visible = true;
                        mnuContextNodeUpdate.Enabled = true;
                        tsmiAddToSolution.Visible = true;
                        tsmiCheckActiveLayer.Visible = true;
                        toolStripSeparator3.Visible = true;
                        toolAddToSolution.Visible = true;
                        btnSave.Enabled = true;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginAssembly>(CrmPluginAssembly.Columns, package.Assemblies);

                        mnuContextNodeEnableAllSteps.Visible = true;
                        mnuContextNodeDisableAllSteps.Visible = true;
                    }
                    break;

                case CrmTreeNodeType.Plugin:
                case CrmTreeNodeType.WorkflowActivity:
                    {
                        CrmPlugin plugin = (CrmPlugin)node;
                        btnSave.Enabled = true;
                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns, plugin.Steps);

                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Enabled = true;

                        mnuContextNodeEnableAllSteps.Visible = true;
                        mnuContextNodeDisableAllSteps.Visible = true;
                    }
                    break;

                case CrmTreeNodeType.Step:
                    {
                        CrmPluginStep step = (CrmPluginStep)node;
                        btnSave.Enabled = false;
                        toolAddToSolution.Visible = true;
                        tsmiAddToSolution.Visible = true;
                        tsmiCheckActiveLayer.Visible = true;
                        UpdateEnableButton(step.Enabled);

                        if (true/*!OrganizationHelper.IsProfilerSupported*/ ||
                            !(step.IsProfiled || step.Organization.Plugins[step.PluginId].IsProfilerPlugin))
                        {
                            toolEnable.Visible = true;
                            mnuContextNodeEnable.Visible = true;
                        }

                        toolUpdate.Visible = true;
                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Visible = true;
                        mnuContextNodeUpdate.Enabled = true;

                        //Load the data table and display information
                        gridTable = OrganizationHelper.CreateDataTable<CrmPluginImage>(CrmPluginImage.Columns, step.Images);
                    }
                    break;

                case CrmTreeNodeType.Image:
                    {
                        toolUpdate.Visible = true;
                        toolUpdate.Enabled = true;
                        mnuContextNodeUpdate.Visible = true;
                        mnuContextNodeUpdate.Enabled = true;
                        CrmPluginImage image = (CrmPluginImage)node;
                        btnSave.Enabled = false;
                        //Load the data table and display information
                        gridTable = null;
                    }
                    break;

                case CrmTreeNodeType.Message:
                case CrmTreeNodeType.MessageEntity:
                    {
                        toolUnregister.Enabled = false;
                        mnuContextNodeUnregister.Enabled = false;
                        btnSave.Enabled = false;
                        CrmTreeNode treeNode = (CrmTreeNode)node;

                         // Allow enable/disable all steps when a user selects an Entity/Message grouping node
                         // (this is what "view by entity/message" uses in the tree)
                         if (treeNode.ChildNodeType == CrmTreeNodeType.Step
                             || treeNode.ChildNodeType == CrmTreeNodeType.Message
                             || treeNode.ChildNodeType == CrmTreeNodeType.MessageEntity)
                         {
                             mnuContextNodeEnableAllSteps.Visible = true;
                             mnuContextNodeDisableAllSteps.Visible = true;

                            // Toolbar buttons should behave the same way
                            toolEnableAllSteps.Visible = true;
                            toolDisableAllSteps.Visible = true;
                         }

                         switch (treeNode.ChildNodeType)
                         {
                             case CrmTreeNodeType.Message:
                                 {
                                     gridTable = OrganizationHelper.CreateDataTable<CrmMessage>(CrmMessage.Columns,
                                         (CrmMessage[])treeNode.ToEntityArray(CrmTreeNodeType.Message));
                                 }
                                 break;

                             case CrmTreeNodeType.MessageEntity:
                                 {
                                     gridTable = OrganizationHelper.CreateDataTable<CrmMessageEntity>(CrmMessageEntity.Columns,
                                         (CrmMessageEntity[])treeNode.ToEntityArray(CrmTreeNodeType.MessageEntity));
                                 }
                                 break;

                             case CrmTreeNodeType.Step:
                                 {
                                     gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns,
                                         (CrmPluginStep[])treeNode.ToEntityArray(CrmTreeNodeType.Step));
                                 }
                                 break;

                             default:
                                 gridTable = null;
                                 break;
                         }
                     }
                     break;

                case CrmTreeNodeType.WebHook:
                    //todo: webhook logic
                    CrmServiceEndpoint webhook = (CrmServiceEndpoint)node;

                    toolUpdate.Visible = true;
                    toolUpdate.Enabled = true;
                    mnuContextNodeUpdate.Visible = true;
                    mnuContextNodeUpdate.Enabled = true;
                    btnSave.Enabled = false;
                    //Load the data table and display information
                    gridTable = OrganizationHelper.CreateDataTable<CrmPluginStep>(CrmPluginStep.Columns, webhook.Steps);
                    break;

                default:

                    throw new NotImplementedException($"NodeType = {node.NodeType.ToString()}");
            }

            //Update the properties grid
            {
                var treeNode = node as CrmTreeNode;
                if (null == treeNode)
                {
                    propGridEntity.SelectedObject = node;
                }
                else
                {
                    propGridEntity.SelectedObject = treeNode.Entity;
                }
            }

            m_dataRowIndex = -1;
            m_dataRowId = Guid.Empty;
            if (gridTable == null)
            {
                grpGrid.Visible = false;
                splitterDisplay.Height = grpGrid.Bottom - splitterDisplay.Top;
            }
            else
            {
                grpGrid.Visible = true;
                splitterDisplay.Height = grpGrid.Top - grpPlugins.Margin.Bottom - grpGrid.Margin.Top - splitterDisplay.Top;

                //Create the list of values

                //Create the new DataSet
                var set = new DataSet("Grid");
                set.Tables.Add(gridTable);

                grvData.DataSource = set.DefaultViewManager;
                grvData.DataMember = gridTable.TableName;
                grvData.Columns["Id"].Visible = false;
            }
        }

        private void SetFilterStatusOnToolbar(bool showAsActive)
        {
            if (showAsActive)
            {
                tsbFilterAssemblies.Text = "Filter Assemblies";
                tsbFilterAssemblies.ToolTipText = m_settings.ExcludedAssemblies;
                tsbFilterAssemblies.Image = Resources.FilterActive;
            }
            else
            {
                tsbFilterAssemblies.Text = "Filter Assemblies";
                tsbFilterAssemblies.ToolTipText = "No filter(s) set";
                tsbFilterAssemblies.Image = Resources.FilterInactive;
            }
        }

        private void toolAssemblyRegister_Click(object sender, EventArgs e)
        {
            var regForm = new PluginRegistrationForm(Organization, this, null, m_settings.Mappings);
            regForm.ShowDialog(ParentForm);

            SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
        }

        private void toolClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void toolEnable_Click(object sender, EventArgs e)
        {
            if (trvPlugins.SelectedNode.NodeType != CrmTreeNodeType.Step)
            {
                return;
            }
            else if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("The step cannot be enabled or disabled.");
                return;
            }

            CrmPluginStep step = (CrmPluginStep)trvPlugins.SelectedNode;
            string captionItem, messageItem;
            if (step.Enabled)
            {
                captionItem = "Disable";
                messageItem = "disable";
            }
            else
            {
                captionItem = "Enable";
                messageItem = "enable";
            }

            if (MessageBox.Show($"Are you sure you want to {messageItem} this step?", $"{captionItem} Step",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                return;
            }

            Enabled = false;
            try
            {
                RegistrationHelper.UpdateStepStatus(m_org, step.StepId, !step.Enabled);
                step.Enabled = !step.Enabled;
                UpdateEnableButton(step.Enabled);
                trvPlugins.RefreshNode(trvPlugins.SelectedNode.NodeId);

                MessageBox.Show($"Step {messageItem}d successfully.", $"{captionItem} Step", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, $"Unable to {messageItem} this item at ths time. An error occurred.", $"{captionItem} Step", ex);
            }
            finally
            {
                Enabled = true;
            }
        }

        private void toolExport_Click(object sender, EventArgs e)
        {
            var exportForm = new ExportTypeSelectionForm();

            switch (exportForm.ShowDialog())
            {
                case DialogResult.Yes:
                    ExportTool();
                    break;

                case DialogResult.No:
                    ExportSelected();
                    break;

                default:
                    break;
            }
        }

        private void toolImageRegister_Click(object sender, EventArgs e)
        {
            Guid nodeId = Guid.Empty;
            if (trvPlugins.SelectedNode != null && trvPlugins.SelectedNode.NodeType != CrmTreeNodeType.Image)
            {
                nodeId = trvPlugins.SelectedNode.NodeId;
            }

            // TODO : Ajith
            // Do Validations if the Image is valid on the Step -Message and then Launch the Wizard

            var regForm = new ImageRegistrationForm(m_org, this, trvPlugins.RootNodes, null, nodeId);
            regForm.ShowDialog();
        }

        private void toolPackageRegister_Click(object sender, EventArgs e)
        {
            var regForm = new PackageRegistrationForm(Organization, this, m_settings);
            SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
            regForm.ShowDialog(this);
        }

        private void toolRefresh_Click(object sender, EventArgs e)
        {
            RefreshFullTreeView();
        }

        private void toolSearch_Click(object sender, EventArgs e)
        {
            var searchForm = new SearchForm(Organization, this, trvPlugins.RootNodes, trvPlugins.SelectedNode)
            {
                StartPosition = FormStartPosition.CenterParent
            };
            searchForm.ShowDialog(this);
        }

        private void toolStepRegister_Click(object sender, EventArgs e)
        {
            //Check if we can extract a plugin from the Tree
            CrmPlugin plugin = null;
            CrmServiceEndpoint serviceEndpoint = null;

            if (trvPlugins.SelectedNode != null)
            {
                Guid pluginId;
                Guid serviceEndpointId = Guid.Empty;
                switch (trvPlugins.SelectedNode.NodeType)
                {
                    case CrmTreeNodeType.Assembly:
                    case CrmTreeNodeType.Message:
                    case CrmTreeNodeType.MessageEntity:

                        pluginId = Guid.Empty;
                        break;

                    case CrmTreeNodeType.Plugin:
                    case CrmTreeNodeType.WorkflowActivity:

                        pluginId = trvPlugins.SelectedNode.NodeId;
                        break;

                    case CrmTreeNodeType.Step:

                        pluginId = ((CrmPluginStep)trvPlugins.SelectedNode).PluginId;
                        break;

                    case CrmTreeNodeType.Image:

                        pluginId = ((CrmPluginImage)trvPlugins.SelectedNode).PluginId;
                        break;

                    case CrmTreeNodeType.ServiceEndpoint:

                        pluginId = ((CrmServiceEndpoint)trvPlugins.SelectedNode).PluginId;
                        serviceEndpointId = ((CrmServiceEndpoint)trvPlugins.SelectedNode).NodeId;
                        break;

                    case CrmTreeNodeType.WebHook:
                        pluginId = Guid.Empty;
                        serviceEndpointId = ((CrmServiceEndpoint)trvPlugins.SelectedNode).NodeId;
                        break;

                    default:
                        throw new NotImplementedException($"NodeType = {trvPlugins.SelectedNode.NodeType.ToString()}");
                }

                if (Guid.Empty != pluginId)
                {
                    plugin = m_org.Plugins[pluginId];
                }
                if (Guid.Empty != serviceEndpointId)
                {
                    serviceEndpoint = pluginId != Guid.Empty ? m_org.ServiceEndpoints[serviceEndpointId] : m_org.Webhooks[serviceEndpointId];
                }
            }

            var regForm = new StepRegistrationForm(Organization, this, plugin, null, serviceEndpoint);
            regForm.ShowDialog();
        }

        private void toolUnregister_Click(object sender, EventArgs e)
        {
            if (trvPlugins.SelectedNode == null)
            {
                return;
            }
            else if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("It cannot be unregistered.");
                return;
            }

            var nodeId = trvPlugins.SelectedNode.NodeId;
            if (MessageBox.Show("Are you sure you want to unregister this item?", "Unregister",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                Enabled = false;
                // this.MainForm.EnableToolBar(false);

                try
                {
                    var builder = new StringBuilder();
                    foreach (KeyValuePair<string, int> stat in RegistrationHelper.Unregister(m_org, m_progressIndicator, trvPlugins))
                    {
                        builder.AppendLine($"{stat.Value} {stat.Key} Unregistered Successfully");
                    }

                    MessageBox.Show(builder.ToString(), "Unregister", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    ErrorMessageForm.ShowErrorMessageBox(this, "Unable to unregister this item an error occurred.", "Unregister Error", ex);
                }
                finally
                {
                    if (trvPlugins.HasNode(nodeId))
                    {
                        trvPlugins.RefreshNode(nodeId, true);
                        SelectItem(trvPlugins.SelectedNode);
                    }

                    Enabled = true;
                    // this.MainForm.EnableToolBar(true);
                }

                trvPlugins.Focus();
            }
            else
            {
                trvPlugins.Focus();
                return;
            }
        }

        private void toolUpdate_Click(object sender, EventArgs e)
        {
            if (trvPlugins.SelectedNode == null)
            {
                return;
            }

            if (IsNodeSystemItem(trvPlugins.SelectedNode))
            {
                ShowSystemItemError("The assembly cannot be updated.");
                return;
            }

            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Assembly:
                    {
                        var assembly = (CrmPluginAssembly)trvPlugins.SelectedNode;
                        if (assembly.SourceType == CrmAssemblySourceType.Package)
                        {
                            MessageBox.Show("This assembly is part of a package. Please change to Display by Package to update this assembly.",
                                "Plugin Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        var regForm = new PluginRegistrationForm(Organization, this, (CrmPluginAssembly)trvPlugins.SelectedNode, m_settings.Mappings);
                        regForm.ShowDialog(ParentForm);
                        SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
                    }
                    break;

                case CrmTreeNodeType.Package:
                    {
                        var regForm = new PackageRegistrationForm(Organization, this, (CrmPluginPackage)trvPlugins.SelectedNode, m_settings);
                        regForm.ShowDialog(ParentForm);
                        SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
                    }
                    break;

                case CrmTreeNodeType.Step:
                    {
                        var step = (CrmPluginStep)trvPlugins.SelectedNode;

                        CrmPlugin plugin = null;
                        CrmServiceEndpoint serviceEndpoint = null;

                        switch (step.EventHandler.LogicalName)
                        {
                            case "serviceendpoint":
                                serviceEndpoint = m_org.Webhooks[step.EventHandler.Id];
                                break;

                            default:
                                plugin = m_org[step.AssemblyId][step.PluginId];

                                if (step.ServiceBusConfigurationId != Guid.Empty)
                                {
                                    serviceEndpoint = m_org.ServiceEndpoints[step.ServiceBusConfigurationId];
                                }
                                break;
                        }

                        var regForm = new StepRegistrationForm(Organization, this, plugin, step, serviceEndpoint);
                        regForm.ShowDialog();
                    }
                    break;

                case CrmTreeNodeType.Image:
                    {
                        var regForm = new ImageRegistrationForm(m_org, this,
                            trvPlugins.RootNodes, (CrmPluginImage)trvPlugins.SelectedNode, trvPlugins.SelectedNode.NodeId);
                        regForm.ShowDialog();
                    }
                    break;

                case CrmTreeNodeType.WebHook:
                    {
                        var webhookRegForm = new WebHookRegistrationForm(m_org, this, (CrmServiceEndpoint)trvPlugins.SelectedNode);
                        webhookRegForm.ShowDialog();
                    }
                    break;

                default:
                    throw new NotImplementedException($"NodeType = {trvPlugins.SelectedNode.NodeType.ToString()}");
            }

            ICrmTreeNode node = trvPlugins.SelectedNode;
            if (node != null)
            {
                trvPlugins_SelectionChanged(sender, new CrmTreeNodeTreeEventArgs(node, TreeViewAction.Unknown));
            }
        }

        private void toolView_Click(object sender, EventArgs e)
        {
            if (sender != null && sender.GetType().IsSubclassOf(typeof(ToolStripItem)))
            {
                ToolStripItem item = (ToolStripItem)sender;
                if (item.Tag != null && item.Tag.GetType() == typeof(CrmViewType))
                {
                    propGridEntity.SelectedObject = null;
                    LoadNodes((CrmViewType)item.Tag);
                }
            }
        }

        private void toolWebHookRegister_Click(object sender, EventArgs e)
        {
            var webhookRegForm = new WebHookRegistrationForm(m_org, this, null);
            webhookRegForm.ShowDialog();
        }

        private void trvPlugins_DoubleClick(object sender, CrmTreeNodeEventArgs e)
        {
            if (toolUpdate.Visible && toolUpdate.Enabled)
            {
                trvPlugins.SelectedNode = e.Node;
                if (toolUpdate.Visible && toolUpdate.Enabled)
                {
                    toolUpdate_Click(sender, e);
                }
            }
            else
            {
                //If the selected step is the Plug-in Profiler plug-in, the PluginRegistrationForm should be displayed.
                //This will allow the consumer the ability to change the isolation mode of the Plug-in Profiler.
                var plugin = trvPlugins.SelectedNode as CrmPlugin;
                if (null != plugin && plugin.IsProfilerPlugin)
                {
                    //Display the Plug-in Registration form
                    using (var form = new PluginRegistrationForm(Organization, this,
                        Organization.Assemblies[plugin.AssemblyId], m_settings.Mappings))
                    {
                        form.ShowDialog(ParentForm);
                        SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
                    }

                    //Update the tree based on the selected node
                    ICrmTreeNode node = trvPlugins.SelectedNode;
                    if (node != null)
                    {
                        trvPlugins_SelectionChanged(sender, new CrmTreeNodeTreeEventArgs(node, TreeViewAction.Unknown));
                    }
                }
            }
        }

        private void trvPlugins_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && toolUpdate.Enabled && toolUpdate.Visible)
            {
                toolUpdate_Click(sender, e);
            }
        }

        private void trvPlugins_NodeRemoved(object sender, CrmTreeNodeEventArgs e)
        {
            switch (e.Node.NodeType)
            {
                case CrmTreeNodeType.Step:
                    {
                        var stepId = e.Node.NodeId;
                        if (null != m_stepParentList && m_stepParentList.ContainsKey(stepId))
                        {
                            CrmTreeNode node = (CrmTreeNode)trvPlugins[m_stepParentList[stepId]];
                            node.RemoveChild(stepId);
                            m_stepParentList.Remove(stepId);

                            RemoveCrmTreeNodesCascadeUp(node);
                        }
                    }
                    break;

                default:
                    return;
            }

            if (null == trvPlugins.SelectedNode)
            {
                propGridEntity.SelectedObject = null;
            }
        }

        private void trvPlugins_SelectionChanged(object sender, CrmTreeNodeTreeEventArgs e)
        {
            SelectItem(e.Node);
        }

        private void tsbFilterAssemblies_Click(object sender, EventArgs e)
        {
            var dialog = new AssembliesFilterForm(m_settings);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (dialog.HasChanged)
                {
                    SetFilterStatusOnToolbar(!string.IsNullOrEmpty(dialog.Filter) || dialog.ExcludeManagedAssemblies);
                    toolRefresh_Click(this, new EventArgs());
                }

                SettingsManager.Instance.Save(GetType(), m_settings, ConnectionDetail.ConnectionId.Value.ToString());
            }
        }

        private void tsmiAddToSolution_Click(object sender, EventArgs e)
        {
            bool isForAssembly = false;
            Guid componentId = Guid.Empty;
            int componentType = -1;
            bool isPackage = false;
            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Assembly:
                    {
                        var assembly = (CrmPluginAssembly)trvPlugins.SelectedNode;
                        isForAssembly = true;
                        componentId = assembly.AssemblyId;
                        componentType = 91;
                    }
                    break;

                case CrmTreeNodeType.Package:
                    {
                        var package = (CrmPluginPackage)trvPlugins.SelectedNode;
                        componentId = package.PackageId;
                        componentType = 10090;
                        isPackage = true;
                    }
                    break;

                case CrmTreeNodeType.Step:
                    {
                        var step = (CrmPluginStep)trvPlugins.SelectedNode;
                        componentId = step.StepId;
                        componentType = 92;
                    }
                    break;
            }

            var dialog = new SolutionPicker(Service);
            if (dialog.ShowDialog(ParentForm) == DialogResult.OK)
            {
                WorkAsync(new WorkAsyncInfo
                {
                    Message = $"Adding {trvPlugins.SelectedNode.NodeType} to solution {dialog.SelectedSolution.GetAttributeValue<string>("friendlyname")}",
                    Work = (bw, evt) =>
                    {
                        if (isPackage)
                        {
                            var scd = Service.RetrieveMultiple(new QueryExpression("solutioncomponentdefinition")
                            {
                                NoLock = true,
                                ColumnSet = new ColumnSet("solutioncomponenttype"),
                                Criteria = new FilterExpression
                                {
                                    Conditions =
                                {
                                    new ConditionExpression("primaryentityname", ConditionOperator.Equal, "pluginpackage")
                                }
                                }
                            }).Entities.FirstOrDefault();

                            if (scd == null)
                            {
                                throw new Exception($"Unable to find the solution component type for table pluginpackage");
                            }

                            componentType = scd.GetAttributeValue<int>("solutioncomponenttype");
                        }

                        Service.Execute(new AddSolutionComponentRequest
                        {
                            AddRequiredComponents = false,
                            ComponentId = componentId,
                            ComponentType = componentType,
                            SolutionUniqueName = dialog.SelectedSolution.GetAttributeValue<string>("uniquename")
                        });
                    },
                    PostWorkCallBack = evt =>
                    {
                        if (evt.Error != null)
                        {
                            MessageBox.Show(ParentForm, $@"An error occured while adding component to solution: {evt.Error.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                });
            }
        }

        private void tsmiCheckActiveLayer_Click(object sender, EventArgs e)
        {
            //TTO
            bool isForAssembly = false;
            Guid componentId = Guid.Empty;
            string componentType = "";

            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Assembly:
                    {
                        var assembly = (CrmPluginAssembly)trvPlugins.SelectedNode;
                        isForAssembly = true;
                        componentId = assembly.AssemblyId;
                        componentType = "PluginAssembly";
                    }
                    break;

                case CrmTreeNodeType.Package:
                    {
                        var package = (CrmPluginPackage)trvPlugins.SelectedNode;
                        componentId = package.PackageId;
                        componentType = "PluginPackage";
                    }
                    break;

                case CrmTreeNodeType.Step:
                    {
                        var step = (CrmPluginStep)trvPlugins.SelectedNode;
                        componentId = step.StepId;
                        componentType = "SdkMessageProcessingStep";
                    }
                    break;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Searching an active layer for {trvPlugins.SelectedNode.NodeType}...",
                Work = (bw, evt) =>
                {
                    var query = new QueryExpression("msdyn_componentlayer")
                    {
                        NoLock = true,
                        ColumnSet = new ColumnSet(true),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                    new ConditionExpression("msdyn_solutioncomponentname",ConditionOperator.Equal,componentType),
                                    new ConditionExpression("msdyn_componentid",ConditionOperator.Equal, componentId),
                                    new ConditionExpression("msdyn_solutionname",ConditionOperator.Equal, "Active"),
                            }
                        }
                    };

                    var result = Service.RetrieveMultiple(query);

                    evt.Result = result;
                },
                PostWorkCallBack = evt =>
                {
                    if (evt.Error != null)
                    {
                        MessageBox.Show(ParentForm, $@"An error occured while searching for an active layer: {evt.Error.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var result = (EntityCollection)evt.Result;

                    if (result.Entities.Count == 0)
                    {
                        MessageBox.Show(this, "No active layer has been found for this component.\n\nYou are all good!", "No Active layer found!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var dr = MessageBox.Show(this, "An active layer has been found!\n\nWould you like to remove it?", "Active layer found!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (dr == DialogResult.Yes)
                    {
                        WorkAsync(new WorkAsyncInfo
                        {
                            Message = $"Removing active layer for {trvPlugins.SelectedNode.NodeType}...",
                            Work = (bw2, evt2) =>
                            {
                                var request = new OrganizationRequest("RemoveActiveCustomizations");
                                request.Parameters["SolutionComponentName"] = result.Entities.First().GetAttributeValue<string>("msdyn_solutioncomponentname");
                                request.Parameters["ComponentId"] = result.Entities.First().GetAttributeValue<Guid>("msdyn_componentid");

                                Service.Execute(request);
                            },
                            PostWorkCallBack = evt2 =>
                            {
                                if (evt.Error != null)
                                {
                                    MessageBox.Show(ParentForm, $@"An error occured while removing the active layer: {evt.Error.Message}", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                                else
                                {
                                    var dr2 = MessageBox.Show(this, "You should refresh display to see the changes\n\nWould you like to refresh display now?", "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                    if (dr2 == DialogResult.Yes)
                                    {
                                        toolRefresh_Click(toolRefresh, new EventArgs());
                                    }
                                }
                            }
                        });
                    }
                }
            });
        }

        private void UpdateEnableButton(bool currentlyEnabled)
        {
            string imageKey;
            if (currentlyEnabled)
            {
                toolEnable.Text = "&Disable";
                imageKey = "disableStep";
            }
            else
            {
                toolEnable.Text = "&Enable";
                imageKey = "enableStep";
            }

            toolEnable.Image = imlEnableImages.Images[imageKey];

            mnuContextNodeEnable.Text = toolEnable.Text;
            mnuContextNodeEnable.Image = toolEnable.Image;
        }

        private void mnuContextNodeShowDependencies_Click(object sender, EventArgs e)
        {
            ShowSelectedAssemblyDependencies();
        }

        private void toolShowDependencies_Click(object sender, EventArgs e)
        {
            ShowSelectedAssemblyDependencies();
        }

        private void ShowSelectedAssemblyDependencies()
        {
            if (trvPlugins.SelectedNode == null)
            {
                MessageBox.Show("Please select a plugin assembly, plugin, or step to view its dependencies.", "Show Dependencies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            switch (trvPlugins.SelectedNode.NodeType)
            {
                case CrmTreeNodeType.Assembly:
                    using (var dlg = new Forms.DependenciesDialog((Wrappers.CrmPluginAssembly)trvPlugins.SelectedNode))
                        dlg.ShowDialog(this);
                    break;
                case CrmTreeNodeType.Plugin:
                case CrmTreeNodeType.WorkflowActivity:
                    using (var dlg = new Forms.DependenciesDialog((Wrappers.CrmPlugin)trvPlugins.SelectedNode))
                        dlg.ShowDialog(this);
                    break;
                case CrmTreeNodeType.Step:
                    using (var dlg = new Forms.DependenciesDialog((Wrappers.CrmPluginStep)trvPlugins.SelectedNode))
                        dlg.ShowDialog(this);
                    break;
                default:
                    MessageBox.Show("Please select a plugin assembly, plugin, or step to view its dependencies.", "Show Dependencies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
            }
        }

        private static bool ConfirmEnableDisableAllSteps(IWin32Window owner, bool enable)
        {
            return MessageBox.Show(owner,
                       enable ? ENABLE_ALL_STEPS_CONFIRMATION : DISABLE_ALL_STEPS_CONFIRMATION,
                       enable ? ENABLE_ALL_STEPS_CAPTION : DISABLE_ALL_STEPS_CAPTION,
                       MessageBoxButtons.YesNo,
                       MessageBoxIcon.Question,
                       MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private void mnuContextNodeEnableAllSteps_Click(object sender, EventArgs e)
        {
            if (!ConfirmEnableDisableAllSteps(this, true))
            {
                return;
            }

            EnableDisableAllSteps(true);
        }

        private void mnuContextNodeDisableAllSteps_Click(object sender, EventArgs e)
        {
            if (!ConfirmEnableDisableAllSteps(this, false))
            {
                return;
            }

            EnableDisableAllSteps(false);
        }

        private void toolEnableAllSteps_Click(object sender, EventArgs e)
        {
            if (!ConfirmEnableDisableAllSteps(this, true))
            {
                return;
            }

            EnableDisableAllSteps(true);
        }

        private void toolDisableAllSteps_Click(object sender, EventArgs e)
        {
            if (!ConfirmEnableDisableAllSteps(this, false))
            {
                return;
            }

            EnableDisableAllSteps(false);
        }
        private void EnableDisableAllSteps(bool enable)
        {
            var node = trvPlugins.SelectedNode;
            if (node == null) return;

            var steps = new List<Wrappers.CrmPluginStep>();

            if (node.NodeType == CrmTreeNodeType.Package)
            {
                var package = (Wrappers.CrmPluginPackage)node;
                foreach (var assembly in package.Assemblies.Values)
                    foreach (var plugin in assembly.Plugins.Values)
                        steps.AddRange(plugin.Steps.Values);
            }
            else if (node.NodeType == CrmTreeNodeType.Assembly)
            {
                var assembly = (Wrappers.CrmPluginAssembly)node;
                foreach (var plugin in assembly.Plugins.Values)
                    steps.AddRange(plugin.Steps.Values);
            }
            else if (node.NodeType == CrmTreeNodeType.Plugin)
            {
                var plugin = (Wrappers.CrmPlugin)node;
                steps.AddRange(plugin.Steps.Values);
            }
            else if (node.NodeType == CrmTreeNodeType.Message || node.NodeType == CrmTreeNodeType.MessageEntity)
            {
                // In "view by entity/message", the tree nodes are grouping nodes (internal CrmTreeNode)
                // and they can directly expose their step children via ToEntityArray(...)
                var treeNode = node as CrmTreeNode;
                if (treeNode != null)
                {
                    AddGroupingNodeSteps(treeNode, steps);
                }
            }
            else
            {
                MessageBox.Show("Please select a package, plugin assembly, class, message, or entity.", "Enable/Disable All Steps", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (steps.Count == 0)
            {
                MessageBox.Show("No steps found.", "Enable/Disable All Steps", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            WorkAsync(new WorkAsyncInfo
            {
                Message = $"{(enable ? "Enabling" : "Disabling")} all steps...",
                Work = (worker, args) =>
                {
                    foreach (var step in steps)
                    {
                        if (step.Enabled != enable)
                        {
                            Helpers.RegistrationHelper.UpdateStepStatus(m_org, step.StepId, enable);
                            step.Enabled = enable;
                        }
                    }
                },
                PostWorkCallBack = evt =>
                {
                    foreach (var step in steps)
                    {
                        trvPlugins.RefreshNode(step.NodeId);
                    }
                    MessageBox.Show($"All steps have been {(enable ? "enabled" : "disabled")}.", "Enable/Disable All Steps", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }

        private static void AddGroupingNodeSteps(CrmTreeNode groupingNode, List<Wrappers.CrmPluginStep> steps)
        {
            if (groupingNode == null)
            {
                throw new ArgumentNullException("groupingNode");
            }
            if (steps == null)
            {
                throw new ArgumentNullException("steps");
            }

            switch (groupingNode.ChildNodeType)
            {
                case CrmTreeNodeType.Step:
                    steps.AddRange(groupingNode.ToEntityArray(CrmTreeNodeType.Step).Cast<CrmPluginStep>());
                    break;

                case CrmTreeNodeType.Message:
                    foreach (CrmTreeNode child in groupingNode.NodeChildren.Cast<CrmTreeNode>())
                    {
                        AddGroupingNodeSteps(child, steps);
                    }
                    break;

                case CrmTreeNodeType.MessageEntity:
                    foreach (CrmTreeNode child in groupingNode.NodeChildren.Cast<CrmTreeNode>())
                    {
                        AddGroupingNodeSteps(child, steps);
                    }
                    break;

                default:
                    break;
            }
        }
        #endregion Private Methods

        #region Private Classes

        private sealed class CrmEntitySorter : IComparer<ICrmTreeNode>
        {
            #region Public Methods

            public int Compare(ICrmTreeNode node1, ICrmTreeNode node2)
            {
                if (node1 == null)
                {
                    return -1;
                }
                else if (node2 == null)
                {
                    return 1;
                }
                else if (node1.NodeType != node2.NodeType)
                {
                    return ((int)node1.NodeType - (int)node2.NodeType);
                }
                else if (node1.NodeType == CrmTreeNodeType.Step)
                {
                    var step1 = (CrmPluginStep)node1;
                    var step2 = (CrmPluginStep)node2;

                    if (step1.Rank == step2.Rank)
                    {
                        if (step1.CreatedOn == null && step2.CreatedOn == null)
                        {
                            return 0;
                        }
                        else if (step1.CreatedOn == null)
                        {
                            return -1;
                        }
                        else if (step2.CreatedOn == null)
                        {
                            return 1;
                        }
                        else
                        {
                            return DateTime.Compare((DateTime)step1.CreatedOn, (DateTime)step2.CreatedOn);
                        }
                    }
                    else if (step1.Rank < step2.Rank)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return string.Compare(GetNodeText(node1), GetNodeText(node2), false);
                }
            }

            #endregion Public Methods

            #region Private Methods

            private string GetNodeText(ICrmTreeNode node)
            {
                if (node == null || node.NodeText == null)
                {
                    return null;
                }
                else
                {
                    return node.NodeText;
                }
            }

            #endregion Private Methods
        }

        private sealed class CrmTreeNode : ICrmTreeNode
        {
            #region Private Fields

            private Dictionary<string, CrmTreeNode> m_childList = null;
            private CrmTreeNodeType m_childType = CrmTreeNodeType.None;
            private ICrmEntity m_entity;
            private CrmTreeNodeImageType m_imageType;
            private Guid m_nodeId;
            private string m_nodeText;
            private Guid m_origNodeId;
            private Guid m_parentNodeId = Guid.Empty;
            private CrmTreeNodeImageType m_selectedImageType;
            private Dictionary<Guid, CrmPluginStep> m_stepList = null;
            private CrmTreeNodeType m_type;
            private string m_typeLabel;

            #endregion Private Fields

            #region Public Constructors

            public CrmTreeNode(CrmMessage message)
            {
                if (message == null)
                {
                    throw new ArgumentNullException("message");
                }

                m_entity = message;
                m_nodeId = message.MessageId;
                m_nodeText = message.Name;
                m_type = CrmTreeNodeType.Message;
                m_typeLabel = "Message";
                m_imageType = CrmTreeNodeImageType.Message;
                m_selectedImageType = CrmTreeNodeImageType.MessageSelected;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            public CrmTreeNode(CrmMessageEntity msgEntity)
            {
                if (msgEntity == null)
                {
                    throw new ArgumentNullException("entity");
                }

                m_entity = msgEntity;
                m_nodeId = msgEntity.MessageEntityId;
                if (string.IsNullOrEmpty(msgEntity.PrimaryEntity))
                {
                    m_nodeText = "No Entity";
                }
                else if (string.IsNullOrEmpty(msgEntity.SecondaryEntity) || msgEntity.SecondaryEntity.Equals("none", StringComparison.CurrentCultureIgnoreCase))
                {
                    m_nodeText = msgEntity.PrimaryEntity;
                }
                else
                {
                    m_nodeText = $"{msgEntity.PrimaryEntity} / {msgEntity.SecondaryEntity}";
                }
                m_type = CrmTreeNodeType.MessageEntity;
                m_typeLabel = "Entity";
                m_imageType = CrmTreeNodeImageType.MessageEntity;
                m_selectedImageType = CrmTreeNodeImageType.MessageEntitySelected;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            public CrmTreeNode(Guid nodeId, string text, CrmTreeNodeType type, CrmTreeNodeImageType imageType, CrmTreeNodeImageType selectedImageType)
            {
                if (text == null)
                {
                    throw new ArgumentNullException("text");
                }

                m_entity = null;
                m_nodeId = nodeId;
                m_nodeText = text;
                m_type = type;
                m_imageType = imageType;
                m_selectedImageType = selectedImageType;

                m_origNodeId = m_nodeId;

                UpdateNodeText();
            }

            #endregion Public Constructors

            #region Public Properties

            public int ChildCount
            {
                get
                {
                    if (null != m_childList)
                    {
                        return m_childList.Count;
                    }
                    else if (null != m_stepList)
                    {
                        return m_stepList.Count;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            public CrmTreeNodeType ChildNodeType
            {
                get
                {
                    return m_childType;
                }
            }

            public ICrmEntity Entity
            {
                get
                {
                    return m_entity;
                }
            }

            public List<ICrmTreeNode> NodeChildren
            {
                get
                {
                    if (m_stepList != null)
                    {
                        var nodeList = new List<CrmPluginStep>(m_stepList.Count);
                        //m_stepList.Values.CopyTo(nodeList.ToArray(), 0);
                        nodeList.AddRange(m_stepList.Values);
                        return nodeList.ToList<ICrmTreeNode>();
                    }
                    else if (m_childList != null)
                    {
                        var nodeList = new List<CrmTreeNode>(m_childList.Count);
                        //m_childList.Values.CopyTo(nodeList.ToArray(), 0);
                        nodeList.AddRange(m_childList.Values);
                        return nodeList.ToList<ICrmTreeNode>();
                    }
                    else
                    {
                        return new ICrmTreeNode[0].ToList<ICrmTreeNode>();
                    }
                }
            }

            public Guid NodeId
            {
                get
                {
                    return m_nodeId;
                }
                set
                {
                    m_nodeId = value;

                    if (null != m_childList)
                    {
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childNode.m_parentNodeId = value;
                        }
                    }
                }
            }

            public CrmTreeNodeImageType NodeImageType
            {
                get
                {
                    return m_imageType;
                }
            }

            public CrmTreeNodeImageType NodeSelectedImageType
            {
                get
                {
                    return m_selectedImageType;
                }
            }

            public string NodeText
            {
                get
                {
                    return m_nodeText;
                }
            }

            public CrmTreeNodeType NodeType
            {
                get
                {
                    return m_type;
                }
            }

            public string NodeTypeLabel
            {
                get
                {
                    return m_typeLabel;
                }
            }

            public Guid OriginalNodeId
            {
                get
                {
                    return m_origNodeId;
                }
            }

            public Guid ParentNodeId
            {
                get
                {
                    return m_parentNodeId;
                }
            }

            #endregion Public Properties

            #region Public Indexers

            public CrmPluginStep this[Guid id]
            {
                get
                {
                    return m_stepList[id];
                }
            }

            public CrmTreeNode this[string text]
            {
                get
                {
                    return m_childList[text];
                }
            }

            #endregion Public Indexers

            #region Public Methods

            public void AddChild(CrmPluginStep node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                else if (m_childType != CrmTreeNodeType.None && m_childType != node.NodeType)
                {
                    throw new ArgumentException("Child Node Type has already been determined");
                }
                else if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = node.NodeType;
                    m_stepList = new Dictionary<Guid, CrmPluginStep>();
                }

                m_stepList.Add(node.NodeId, node);
            }

            public void AddChild(CrmTreeNode node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("node");
                }
                else if (m_childType != CrmTreeNodeType.None && m_childType != node.NodeType)
                {
                    throw new ArgumentException("Child Node Type has already been determined");
                }
                else if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = node.NodeType;
                    m_childList = new Dictionary<string, CrmTreeNode>();
                }

                node.m_parentNodeId = m_nodeId;
                m_childList.Add(node.NodeText, node);
            }

            public bool HasChild(Guid id)
            {
                if (m_stepList != null)
                {
                    return m_stepList.ContainsKey(id);
                }
                else
                {
                    return false;
                }
            }

            public bool HasChild(string nodeText)
            {
                if (m_childList != null)
                {
                    return m_childList.ContainsKey(nodeText);
                }
                else
                {
                    return false;
                }
            }

            public void RemoveChild(Guid key)
            {
                if (m_stepList == null)
                {
                    throw new ArgumentException("Id is not in the list");
                }
                else
                {
                    m_stepList.Remove(key);
                }
            }

            public void RemoveChild(string key)
            {
                if (m_childList == null)
                {
                    throw new ArgumentException("Id is not in the list");
                }
                else
                {
                    m_childList.Remove(key);
                }
            }

            public ICrmEntity[] ToEntityArray(CrmTreeNodeType type)
            {
                if (m_childType == CrmTreeNodeType.None)
                {
                    m_childType = type;
                }
                else if (m_childType != type)
                {
                    throw new ArgumentNullException("Child type already determined");
                }

                ICrmEntity[] childList;
                int childIndex = 0;
                switch (type)
                {
                    case CrmTreeNodeType.Message:
                        childList = new CrmMessage[m_childList.Count];
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childList[childIndex++] = (CrmMessage)childNode.Entity;
                        }
                        break;

                    case CrmTreeNodeType.MessageEntity:
                        childList = new CrmMessageEntity[m_childList.Count];
                        foreach (CrmTreeNode childNode in m_childList.Values)
                        {
                            childList[childIndex++] = (CrmMessageEntity)childNode.Entity;
                        }
                        break;

                    case CrmTreeNodeType.Step:
                        childList = new CrmPluginStep[m_stepList.Count];
                        m_stepList.Values.CopyTo((CrmPluginStep[])childList, 0);
                        break;

                    default:
                        throw new NotImplementedException($"Type = {type.ToString()}");
                }

                return childList;
            }

            #endregion Public Methods

            #region Private Methods

            private void UpdateNodeText()
            {
                string prefix;
                switch (m_type)
                {
                    case CrmTreeNodeType.Message:
                        prefix = "Message";
                        break;

                    case CrmTreeNodeType.MessageEntity:
                        prefix = "Entity";
                        break;

                    default:
                        throw new NotImplementedException($"NodeType = {m_type.ToString()}");
                }

                m_nodeText = $"({prefix}) {m_nodeText}";
            }

            #endregion Private Methods
        }

        #endregion Private Classes  
    }
}
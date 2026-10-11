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

namespace Xrm.Sdk.PluginRegistration.Controls
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Windows.Forms.Design;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    public enum CrmTreeNodeImageType
    {
        Assembly,
        AssemblySelected,
        Plugin,
        PluginSelected,
        PluginProfiler,
        PluginProfilerSelected,
        WorkflowActivity,
        WorkflowActivitySelected,
        StepEnabled,
        StepEnabledSelected,
        StepProfiled,
        StepProfiledSelected,
        StepDisabled,
        StepDisabledSelected,
        Image,
        ImageSelected,
        Connection,
        ConnectionSelected,
        Organization,
        OrganizationSelected,
        Message,
        MessageSelected,
        MessageEntity,
        MessageEntitySelected,
        ServiceEndpoint,
        ServiceEndpointSelected,
        Package,
        PackageSelected,
    }

    [Flags]
    public enum CrmTreeNodeType
    {
        /// <summary>
        /// A node of this type will never be listed.
        /// </summary>
        None = 0,

        Connection = 1,
        Organization = 2,
        Assembly = 4,
        Plugin = 8,
        WorkflowActivity = 16,
        Step = 32,
        Image = 64,
        Message = 128,
        MessageEntity = 256,
        ServiceEndpoint = 512,
        WebHook = 1024,
        Package = 2048,
        CustomApi = 4096
    }

    public interface ICrmEditableTreeNode : ICrmTreeNode
    {
        #region Public Properties

        /// <summary>
        /// Text that will be displayed while the label is being edited
        /// </summary>
        string NodeEditText { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Simulates behavior of NodeEditText without updating the property's value
        /// </summary>
        /// <param name="newText">Value to be simulated</param>
        /// <returns>Value that would have been returned from NodeEditText had it been updated</returns>
        string GetNodeEditTextForLabel(string newText);

        #endregion Public Methods
    }

    public interface ICrmTreeNode
    {
        #region Public Properties

        List<ICrmTreeNode> NodeChildren { get; }
        Guid NodeId { get; }
        CrmTreeNodeImageType NodeImageType { get; }
        CrmTreeNodeImageType NodeSelectedImageType { get; }

        /// <summary>
        /// Text that will be displayed in the node
        /// </summary>
        string NodeText { get; }

        CrmTreeNodeType NodeType { get; }
        string NodeTypeLabel { get; }

        #endregion Public Properties
    }

    [Designer(typeof(DocumentDesigner), typeof(IRootDesigner))]
    [DefaultEvent("SelectionChanged")]
    public partial class CrmTreeControl : UserControl, IRootDesigner
    {
        #region Private Fields

        private bool m_autoExpand = true;
        private ContextMenu m_contextMenu = null;
        private Dictionary<CrmTreeNodeType, ContextMenuStrip> m_contextMenuList = new Dictionary<CrmTreeNodeType, ContextMenuStrip>();
        private ContextMenuStrip m_contextMenuStrip = null;
        private Guid m_currentlyEditing = Guid.Empty;
        private bool m_disableCheckChange = false;
        private bool m_disableSelectionChange = false;
        private CrmTreeNodeType m_excludeTypes = CrmTreeNodeType.None;
        private Dictionary<Guid?, CrmTreeNode> m_nodeList = new Dictionary<Guid?, CrmTreeNode>();
        private bool m_singleParentCheck = false;
        private NodeSorter m_sorter = new NodeSorter();

        #endregion Private Fields

        #region Public Constructors

        public CrmTreeControl()
            : this(null)
        {
        }

        /// <summary>
        /// Create the TreeView with different images than the specific images
        /// </summary>
        /// <param name="nodeImages">Images that should be used instead of default images. The default image will
        /// be used for any items not found in this list.</param>
        public CrmTreeControl(Dictionary<CrmTreeNodeImageType, Image> nodeImages)
        {
            InitializeComponent();

            //Use the image provided in the nodeImages dictionary. If it is not given,
            //use the image that is in the Resources.resX file.
            ImageList imageList = new ImageList();
            List<CrmTreeNodeImageType> loadImageList = new List<CrmTreeNodeImageType>();
            foreach (CrmTreeNodeImageType type in Enum.GetValues(typeof(CrmTreeNodeImageType)))
            {
                if (nodeImages == null || !nodeImages.ContainsKey(type) || nodeImages[type] == null)
                {
                    loadImageList.Add(type);
                }
                else
                {
                    imageList.Images.Add(type.ToString(), nodeImages[type]);
                }
            }

            //Load the images that we want
            Dictionary<CrmTreeNodeImageType, Image> loadedImageList = null;
            try
            {
                loadedImageList = CrmResources.LoadImage(loadImageList.ToArray());
                foreach (KeyValuePair<CrmTreeNodeImageType, Image> imgInfo in loadedImageList)
                {
                    imageList.Images.Add(imgInfo.Key.ToString(), imgInfo.Value);
                }
            }
            catch (Exception)
            {
                if (loadedImageList != null)
                {
                    foreach (Image img in loadedImageList.Values)
                    {
                        if (img != null)
                        {
                            img.Dispose();
                        }
                    }
                }

                throw;
            }

            trvPlugins.ImageList = imageList;
            trvPlugins.TreeViewNodeSorter = m_sorter;
        }

        #endregion Public Constructors

        #region Public Events

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeLabelEditEventArgs> AfterLabelEdit;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeLabelEditEventArgs> BeforeLabelEdit;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeEventArgs> CheckStateChanged;

        public new event EventHandler<CrmTreeNodeEventArgs> Click;

        public new event EventHandler<CrmTreeNodeEventArgs> DoubleClick;

        public new event KeyEventHandler KeyDown;

        public new event KeyPressEventHandler KeyPress;

        public new event KeyEventHandler KeyUp;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeEventArgs> LabelEditCanceled;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeEventArgs> LabelEdited;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeEventArgs> NodeAdded;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeEventArgs> NodeRemoved;

        [Browsable(true)]
        public event EventHandler<CrmTreeNodeTreeEventArgs> SelectionChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        /// Gets whether every node is checked
        /// </summary>
        [Browsable(false)]
        public bool AllNodesChecked
        {
            get
            {
                if (SingleCheckParent)
                {
                    return (CheckedNodes.Count == m_nodeList.Count);
                }
                else
                {
                    //If the parent is checked, then every child is checked.
                    //If all of the parents are checked, then every node is checked
                    foreach (TreeNode node in trvPlugins.Nodes)
                    {
                        if (!node.Checked)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Expand new children automatically
        /// </summary>
        [DefaultValue(true)]
        public bool AutoExpand
        {
            get
            {
                return m_autoExpand;
            }
            set
            {
                m_autoExpand = value;
            }
        }

        public override Color BackColor
        {
            get
            {
                return trvPlugins.BackColor;
            }
            set
            {
                trvPlugins.BackColor = value;
            }
        }

        /// <summary>
        /// Adds checkboxes to the nodes. This can be set at any time.
        /// </summary>
        [DefaultValue(false)]
        public bool CheckBoxes
        {
            get
            {
                return trvPlugins.CheckBoxes;
            }

            set
            {
                trvPlugins.CheckBoxes = value;
            }
        }

        /// <summary>
        /// Retrieves a list of all of the checked nodes
        /// </summary>
        [Browsable(false)]
        public ICollection<ICrmTreeNode> CheckedNodes
        {
            get
            {
                List<ICrmTreeNode> nodeList = new List<ICrmTreeNode>();
                RetrieveCheckedNodes(trvPlugins.Nodes, nodeList, true);
                return nodeList;
            }
        }

        public IComponent Component
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        [Browsable(false)]
        [DefaultValue(null)]
        public override ContextMenu ContextMenu
        {
            get
            {
                return m_contextMenu;
            }
            set
            {
                if (value == m_contextMenu)
                {
                    return;
                }
                else
                {
                    m_contextMenu = value;

                    foreach (CrmTreeNode node in m_nodeList.Values)
                    {
                        node.TreeNode.ContextMenu = value;
                    }
                }
            }
        }

        [DefaultValue(null)]
        public override ContextMenuStrip ContextMenuStrip
        {
            get
            {
                return m_contextMenuStrip;
            }
            set
            {
                if (value == m_contextMenuStrip)
                {
                    return;
                }
                else
                {
                    m_contextMenuStrip = value;

                    foreach (CrmTreeNode node in m_nodeList.Values)
                    {
                        node.TreeNode.ContextMenuStrip = value;
                    }
                }
            }
        }

        [Browsable(false)]
        public int CountRootNodes
        {
            get
            {
                return trvPlugins.Nodes.Count;
            }
        }

        public int CountTotalNodes
        {
            get
            {
                return m_nodeList.Count;
            }
        }

        [Browsable(false)]
        public IComparer<ICrmTreeNode> CrmTreeNodeSorter
        {
            get
            {
                return m_sorter.Sorter;
            }

            set
            {
                m_sorter.Sorter = value;
            }
        }

        /// <summary>
        /// Include these types of nodes in the tree. This must be set before calling LoadNodes / RefreshNode.
        /// If a type is added that is not in this list, an error will be thrown
        /// </summary>
        [DefaultValue(CrmTreeNodeType.None)]
        public CrmTreeNodeType ExcludeTypes
        {
            get
            {
                return m_excludeTypes;
            }

            set
            {
                m_excludeTypes = value;
            }
        }

        public override Color ForeColor
        {
            get
            {
                return trvPlugins.ForeColor;
            }

            set
            {
                trvPlugins.ForeColor = value;
            }
        }

        /// <summary>
        /// Get or sets a value indicating whether a node's text can be edited
        /// </summary>
        public bool LabelEdit
        {
            get
            {
                return trvPlugins.LabelEdit;
            }
            set
            {
                trvPlugins.LabelEdit = value;
            }
        }

        [Browsable(false)]
        public AutoCompleteStringCollection NodeAutoCompleteCollection
        {
            get
            {
                AutoCompleteStringCollection autoCompleteList = new AutoCompleteStringCollection();
                Collection<string> itemList = new Collection<string>();
                foreach (CrmTreeNode node in m_nodeList.Values)
                {
                    string nodeKey = node.TreeNode.Text.ToLowerInvariant().Trim();
                    if (!itemList.Contains(nodeKey))
                    {
                        autoCompleteList.Add(node.TreeNode.Text);
                        itemList.Add(nodeKey);
                    }
                }

                return autoCompleteList;
            }
        }

        [Browsable(false)]
        public List<ICrmTreeNode> RootNodes
        {
            get
            {
                List<ICrmTreeNode> rootNodes = new List<ICrmTreeNode>();
                foreach (TreeNode node in trvPlugins.Nodes)
                {
                    if (null == node)
                    {
                        continue;
                    }

                    rootNodes.Add(((CrmTreeNode)node.Tag).CrmNode);
                }

                return rootNodes;
            }
        }

        /// <summary>
        /// Currently selected node. If none is selected, null is returned
        /// </summary>
        [Browsable(false)]
        public ICrmTreeNode SelectedNode
        {
            get
            {
                if (trvPlugins.SelectedNode == null)
                {
                    return null;
                }
                else
                {
                    return ((CrmTreeNode)trvPlugins.SelectedNode.Tag).CrmNode;
                }
            }

            set
            {
                if (value == null)
                {
                    trvPlugins.SelectedNode = null;
                }
                else if (HasNode(value.NodeId))
                {
                    trvPlugins.SelectedNode = m_nodeList[value.NodeId].TreeNode;
                }
                else
                {
                    throw new ArgumentException("Item is not in the tree");
                }
            }
        }

        public bool ShowNodeToolTips
        {
            get
            {
                return trvPlugins.ShowNodeToolTips;
            }

            set
            {
                trvPlugins.ShowNodeToolTips = value;
            }
        }

        /// <summary>
        /// If true, then checking a single child node will check the parent node. Otherwise, every child node has to be checked to check the parent node.
        /// Must be set before LoadNodes is called
        /// </summary>
        [DefaultValue(false)]
        public bool SingleCheckParent
        {
            get
            {
                return m_singleParentCheck;
            }

            set
            {
                m_singleParentCheck = value;
            }
        }

        public ViewTechnology[] SupportedTechnologies
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        [Browsable(false)]
        [DefaultValue(null)]
        public ContextMenu TreeContextMenu
        {
            get
            {
                return trvPlugins.ContextMenu;
            }
            set
            {
                trvPlugins.ContextMenu = value;
            }
        }

        [DefaultValue(null)]
        public ContextMenuStrip TreeContextMenuStrip
        {
            get
            {
                return trvPlugins.ContextMenuStrip;
            }
            set
            {
                trvPlugins.ContextMenuStrip = value;
            }
        }

        public DesignerVerbCollection Verbs
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion Public Properties

        #region Public Indexers

        /// <summary>
        /// Lookup a node based on id
        /// </summary>
        /// <param name="nodeId">Id to be found</param>
        /// <exception cref="ArgumentException">If the Guid cannot be found</exception>
        /// <returns>Node that was found</returns>
        [Browsable(false)]
        public ICrmTreeNode this[Guid? nodeId]
        {
            get
            {
                if (HasNode(nodeId))
                {
                    return m_nodeList[nodeId].CrmNode;
                }
                else
                {
                    throw new ArgumentException("Invalid Guid");
                }
            }
        }

        #endregion Public Indexers

        #region Public Methods

        /// <summary>
        /// Adds a new node (and its children)
        /// </summary>
        public void AddNode(Guid parentNodeId, ICrmTreeNode node)
        {
            AddNode(parentNodeId, node, false);
        }

        /// <summary>
        /// Adds a new node (and its children)
        /// </summary>
        public void AddNode(Guid parentNodeId, ICrmTreeNode node, bool checkNode)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            else if (Guid.Empty != parentNodeId && !HasNode(parentNodeId))
            {
                throw new ArgumentException("Invalid Node Id", "parentNodeId");
            }
            else if (HasNode(node.NodeId))
            {
                throw new ArgumentException("Node is already in the tree", "node");
            }

            TreeNodeCollection nodeList = RetrieveParentNodeCollection(parentNodeId);
            bool hasChildren = (nodeList.Count != 0);
            AddNodes(nodeList, new List<ICrmTreeNode> { node }, checkNode);
            if (m_autoExpand && !hasChildren)
            {
                if (parentNodeId == Guid.Empty)
                {
                    trvPlugins.ExpandAll();
                }
                else
                {
                    m_nodeList[parentNodeId].TreeNode.ExpandAll();
                }
            }
        }

        /// <summary>
        /// Begins the editing of the TreeNode
        /// </summary>
        public void BeginNodeTextEdit(Guid nodeId)
        {
            if (!LabelEdit)
            {
                throw new InvalidOperationException("BeginNodeTextEdit failed because LabelEdit is false");
            }
            else if (HasNode(nodeId))
            {
                m_nodeList[nodeId].TreeNode.BeginEdit();
                m_currentlyEditing = nodeId;
            }
            else
            {
                throw new ArgumentException("Invalid Node Id", "nodeId");
            }
        }

        /// <summary>
        /// Apply the specified value to all nodes
        /// </summary>
        public void CheckAllNodes(bool checkValue)
        {
            foreach (TreeNode node in trvPlugins.Nodes)
            {
                if (null == node)
                {
                    continue;
                }

                node.Checked = checkValue;
            }
        }

        /// <summary>
        /// Check a specific node
        /// </summary>
        public void CheckNode(Guid nodeId, bool checkValue)
        {
            if (!HasNode(nodeId))
            {
                throw new ArgumentException("nodeId");
            }
            else
            {
                m_nodeList[nodeId].TreeNode.Checked = checkValue;
            }
        }

        /// <summary>
        /// Collapses the entire tree
        /// </summary>
        public void Collapse()
        {
            Collapse(Guid.Empty, true);
        }

        /// <summary>
        /// Collapses the node. If none is specified, it applies to the entire tree
        /// </summary>
        /// <param name="nodeId">nodeId</param>
        /// <param name="applyToChildren">Collapse child nodes as well</param>
        public void Collapse(Guid nodeId, bool applyToChildren)
        {
            if (nodeId == Guid.Empty)
            {
                trvPlugins.CollapseAll();
            }
            else if (HasNode(nodeId))
            {
                if (applyToChildren)
                {
                    CollapseAll(m_nodeList[nodeId].TreeNode);
                }
                else
                {
                    m_nodeList[nodeId].TreeNode.Collapse();
                }
            }
            else
            {
                throw new ArgumentException("Invalid Node Id", "nodeId");
            }
        }

        public void DoDefaultAction()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Ends the editing of the tree node
        /// </summary>
        /// <param name="cancelChanges">Cancels the current changes being made</param>
        public void EndNodeTextEdit(Guid nodeId, bool cancelChanges)
        {
            if (!LabelEdit)
            {
                throw new InvalidOperationException("EndNodeTextEdit failed because LabelEdit is false");
            }
            else if (HasNode(nodeId))
            {
                m_nodeList[nodeId].TreeNode.EndEdit(cancelChanges);
                m_currentlyEditing = Guid.Empty;
            }
            else
            {
                throw new ArgumentException("Invalid Node Id", "nodeId");
            }
        }

        /// <summary>
        /// Expands the entire tree
        /// </summary>
        public void Expand()
        {
            Expand(Guid.Empty, true);
        }

        /// <summary>
        /// Expands the node. If none is specified, it applies to the entire tree
        /// </summary>
        /// <param name="nodeId">nodeId</param>
        /// <param name="applyToChildren">Expand child nodes as well</param>
        public void Expand(Guid nodeId, bool applyToChildren)
        {
            if (nodeId == Guid.Empty)
            {
                trvPlugins.ExpandAll();
            }
            else if (HasNode(nodeId))
            {
                if (applyToChildren)
                {
                    m_nodeList[nodeId].TreeNode.ExpandAll();
                }
                else
                {
                    m_nodeList[nodeId].TreeNode.Expand();
                }
            }
            else
            {
                throw new ArgumentException("Invalid Node Id", "nodeId");
            }
        }

        /// <summary>
        /// Gets a the ContextMenuStrip for the specified type
        /// </summary>
        public ContextMenuStrip GetContextMenuStrip(CrmTreeNodeType type)
        {
            if (HasContextMenuStrip(type))
            {
                return m_contextMenuList[type];
            }
            else
            {
                throw new ArgumentException("Invalid Type specified", "type");
            }
        }

        /// <summary>
        /// Retrieve the color of the specified node
        /// </summary>
        public Color GetNodeColor(Guid nodeId)
        {
            if (HasNode(nodeId))
            {
                return m_nodeList[nodeId].TreeNode.ForeColor;
            }
            else
            {
                throw new ArgumentException("Id is invalid", "nodeId");
            }
        }

        /// <summary>
        /// Retrieve the ToolTipText of the specified node
        /// </summary>
        public string GetNodeToolTipText(Guid nodeId)
        {
            if (HasNode(nodeId))
            {
                return m_nodeList[nodeId].TreeNode.ToolTipText;
            }
            else
            {
                throw new ArgumentException("Id is invalid", "nodeId");
            }
        }

        public object GetView(ViewTechnology technology)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Gets a value indicating whether the specified type has a ContextMenuStrip
        /// </summary>
        public bool HasContextMenuStrip(CrmTreeNodeType type)
        {
            return m_contextMenuList.ContainsKey(type);
        }

        /// <summary>
        /// Checks whether a node is in the tree
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns>True if the node exists, False if it doesn't</returns>
        public bool HasNode(Guid? nodeId)
        {
            return (m_nodeList.ContainsKey(nodeId));
        }

        public void Initialize(IComponent component)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        /// <summary>
        /// Specifies whether the specified node is checked
        /// </summary>
        /// <exception cref="ArgumentException">Invalid nodeId given</exception>
        public bool IsNodeChecked(Guid nodeId)
        {
            if (HasNode(nodeId))
            {
                return m_nodeList[nodeId].Checked;
            }
            else
            {
                throw new ArgumentException("nodeId");
            }
        }

        /// <summary>
        /// Indicates whether the given node's text can be edited
        /// </summary>
        /// <param name="node">Node to be checked</param>
        /// <returns>Value indicating whether the given node's text can be edited</returns>
        public bool IsNodeTextEditable(ICrmTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            ICrmEditableTreeNode editableNode = node as ICrmEditableTreeNode;
            return (editableNode != null);
        }

        /// <summary>
        /// Indicates whether the given node's text can be edited
        /// </summary>
        /// <param name="nodeId">Id for the node to be checked</param>
        /// <returns>Value indicating whether the given node's text can be edited</returns>
        public bool IsNodeTextEditable(Guid nodeId)
        {
            return IsNodeTextEditable(m_nodeList[nodeId].CrmNode);
        }

        /// <summary>
        /// Reloads the entire tree
        /// </summary>
        /// <param name="rootNodes">Nodes to be loaded. If not specifed, the tree will be empty</param>
        public void LoadNodes(List<ICrmTreeNode> rootNodes)
        {
            m_nodeList.Clear();
            trvPlugins.Nodes.Clear();

            if (rootNodes == null || rootNodes.Count == 0)
            {
                return;
            }
            else
            {
                AddNodes(trvPlugins.Nodes, rootNodes, false);
                trvPlugins.SelectedNode = null;
            }
        }

        /// <summary>
        /// Refeshes the specified node with new text and image
        /// </summary>
        /// <param name="nodeId">Node that needs to be refreshed</param>
        public void RefreshNode(Guid nodeId)
        {
            RefreshNode(nodeId, false, false);
        }

        /// <summary>
        /// Refeshes the specified node with new text and image
        /// </summary>
        /// <param name="nodeId">Node that needs to be refreshed</param>
        /// <param name="reloadChildren">If true, children of this node will be removed and added again</param>
        public void RefreshNode(Guid nodeId, bool reloadChildren)
        {
            RefreshNode(nodeId, reloadChildren, false);
        }

        /// <summary>
        /// Refeshes the specified node with new text and image
        /// </summary>
        /// <param name="nodeId">Node that needs to be refreshed</param>
        /// <param name="reloadChildren">If true, children of this node will be removed and added again</param>
        /// <param name="checkChildren">Indicates that each child should be checked</param>
        public void RefreshNode(Guid nodeId, bool reloadChildren, bool checkChildren)
        {
            if (HasNode(nodeId))
            {
                CrmTreeNode rootNode = m_nodeList[nodeId];

                rootNode.TreeNode.Text = rootNode.CrmNode.NodeText;
                rootNode.TreeNode.ImageKey = rootNode.CrmNode.NodeImageType.ToString();
                rootNode.TreeNode.SelectedImageKey = rootNode.CrmNode.NodeImageType.ToString() + "Selected";

                if (reloadChildren)
                {
                    Clear(rootNode.TreeNode.Nodes);

                    if (CheckIfReSortNeeded(rootNode))
                    {
                        Sort(rootNode.CrmNode.NodeId);
                    }

                    rootNode.ReloadChildren();

                    AddNodes(rootNode.TreeNode.Nodes, rootNode.CrmNode.NodeChildren, checkChildren);

                    if (m_autoExpand)
                    {
                        rootNode.TreeNode.ExpandAll();
                    }
                }
                else if (CheckIfReSortNeeded(rootNode))
                {
                    Sort(rootNode.CrmNode.NodeId);
                }
            }
        }

        /// <summary>
        /// Refeshes all nodes with new text and images
        /// </summary>
        /// <param name="checkChildren">Indicates that each child should be checked</param>
        public void RefreshNodes(bool checkChildren)
        {
            foreach (TreeNode node in trvPlugins.Nodes)
            {
                RefreshNode(((CrmTreeNode)node.Tag).CrmNode.NodeId, true, checkChildren);
            }
        }

        /// <summary>
        /// Remove a node with the given id
        /// </summary>
        public void RemoveNode(Guid nodeId)
        {
            if (!HasNode(nodeId))
            {
                //throw new ArgumentException("Invalid Node ID given");
                //NodeId doesn't exist. Most likely it is already removed. I don't see the use for throwing exception
                return;
            }

            CrmTreeNode node = m_nodeList[nodeId];

            var parentTreeNode = m_nodeList[nodeId].TreeNode.Parent;
            if (parentTreeNode != null)
            {
                var parentTag = parentTreeNode.Tag;
                if (parentTag != null)
                {
                    var crmnode = ((CrmTreeNode)parentTag).CrmNode;

                    switch (crmnode.NodeType)
                    {
                        case CrmTreeNodeType.Package:
                            if (crmnode.NodeChildren.Count > 0)
                            {
                                ((CrmPluginPackage)crmnode).RemoveAssembly(nodeId);
                            }
                            break;

                        case CrmTreeNodeType.Assembly:
                            if (crmnode.NodeChildren.Count > 0)
                            {
                                ((CrmPluginAssembly)crmnode).RemovePlugin(nodeId);
                            }
                            break;

                        case CrmTreeNodeType.Plugin:
                            if (crmnode.NodeChildren.Count > 0)
                            {
                                ((CrmPlugin)crmnode).RemoveStep(nodeId);
                            }
                            break;

                        case CrmTreeNodeType.Step:
                            if (crmnode.NodeChildren.Count > 0)
                            {
                                ((CrmPluginStep)crmnode).RemoveImage(nodeId);
                            }
                            break;

                        default:
                            //Ignoring other cases
                            break;
                    }
                }
            }
            //Remove all of the children
            RemoveNodes(node.TreeNode.Nodes);

            //Remove this item from its parent node
            node.TreeNode.Remove();

            //Retrieve the curent node
            if (null != NodeRemoved)
            {
                NodeRemoved(this, new CrmTreeNodeEventArgs(node.CrmNode));
            }

            //Remove the item from the list
            m_nodeList.Remove(nodeId);
        }

        /// <summary>
        /// Searches the tree. Unmatched nodes are removed
        /// </summary>
        /// <param name="text">Text to find (any node that contains this text)</param>
        public void SearchAndRemove(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            List<TreeNode> removeNodeList = new List<TreeNode>();
            foreach (TreeNode node in trvPlugins.Nodes)
            {
                if (null == node)
                {
                    continue;
                }

                Search(node, null, removeNodeList, text);
            }

            foreach (TreeNode node in removeNodeList)
            {
                if (null == node)
                {
                    continue;
                }

                m_nodeList.Remove(((CrmTreeNode)node.Tag).CrmNode.NodeId);
                node.Remove();
            }
        }

        /// <summary>
        /// Every Node of this type (added from now on) will have this ContextMenuStrip.
        /// If the value is set to Null, then the ContextMenuStrip is removed.
        /// </summary>
        public void SetContextMenuStrip(CrmTreeNodeType type, ContextMenuStrip strip)
        {
            if (m_contextMenuList.ContainsKey(type))
            {
                if (strip == null)
                {
                    m_contextMenuList.Remove(type);
                }
                else
                {
                    m_contextMenuList[type] = strip;
                }
            }
            else if (strip == null)
            {
                throw new ArgumentNullException("strip");
            }
            else
            {
                m_contextMenuList.Add(type, strip);
            }
        }

        /// <summary>
        /// Change the color of the specified node
        /// </summary>
        public void SetNodeColor(Guid nodeId, Color color)
        {
            if (HasNode(nodeId))
            {
                m_nodeList[nodeId].TreeNode.ForeColor = color;
            }
            else
            {
                throw new ArgumentException("Id is invalid", "nodeId");
            }
        }

        /// <summary>
        /// Change the ToolTipText of the specified node
        /// </summary>
        public void SetNodeToolTipText(Guid nodeId, string tip)
        {
            if (HasNode(nodeId))
            {
                m_nodeList[nodeId].TreeNode.ToolTipText = tip;
            }
            else
            {
                throw new ArgumentException("Id is invalid", "nodeId");
            }
        }

        /// <summary>
        /// Sorts the items using the CrmTreeNodeSorter
        /// </summary>
        public void Sort()
        {
            m_disableSelectionChange = true;
            TreeNode selectedNode = trvPlugins.SelectedNode;

            trvPlugins.Sort();

            trvPlugins.SelectedNode = selectedNode;
            m_disableSelectionChange = false;
        }

        #endregion Public Methods

        #region Private Methods

        private void AddNodes(TreeNodeCollection parentNodeList, List<ICrmTreeNode> nodes, bool checkNodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            foreach (ICrmTreeNode node in nodes)
            {
                if (node != null && IncludeType(node.NodeType))
                {
                    ICrmTreeNode crmNode = node;
                    bool firstChild = (parentNodeList.Count == 0);

                    var tNode = new TreeNode(node.NodeText)
                    {
                        ImageKey = crmNode.NodeImageType.ToString(),
                        SelectedImageKey = crmNode.NodeSelectedImageType.ToString()
                    };
                    tNode.Tag = new CrmTreeNode(SingleCheckParent, tNode, node);

                    if (HasContextMenuStrip(crmNode.NodeType))
                    {
                        tNode.ContextMenuStrip = GetContextMenuStrip(crmNode.NodeType);
                    }
                    else
                    {
                        tNode.ContextMenu = ContextMenu;
                        tNode.ContextMenuStrip = ContextMenuStrip;
                    }

                    parentNodeList.Add(tNode);

                    if (m_autoExpand && firstChild)
                    {
                        if (tNode.Parent == null)
                        {
                            trvPlugins.ExpandAll();
                        }
                        else
                        {
                            tNode.Parent.ExpandAll();
                        }
                    }

                    if (m_nodeList.ContainsKey(node.NodeId)) continue;

                    m_nodeList.Add(node.NodeId, (CrmTreeNode)tNode.Tag);
                    if (CheckBoxes)
                    {
                        if (checkNodes)
                        {
                            tNode.Checked = true;
                        }

                        ((CrmTreeNode)tNode.Tag).UpdateParentChecked();
                    }

                    if (null != NodeAdded)
                    {
                        NodeAdded(this, new CrmTreeNodeEventArgs(crmNode));
                    }

                    var childNodes = node.NodeChildren;
                    if (childNodes != null && childNodes.Count != 0)
                    {
                        AddNodes(tNode.Nodes, childNodes, checkNodes);
                    }
                }
            }
        }

        private bool CheckIfReSortNeeded(CrmTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            else if (node.TreeNode.PrevNode == null && node.TreeNode.NextNode == null)
            {
                return false;
            }

            //Do a comparison on the sibling before & after. If the node appears in the correct place,
            //there is no need to resort the entire tree

            //Result should be -1 or 0 if we don't need to resort
            int prevNode = m_sorter.Compare(node.TreeNode.PrevNode, node.TreeNode);

            //Result should be -1 or 0 if we don't need to resort
            int nextNode = m_sorter.Compare(node.TreeNode, node.TreeNode.NextNode);

            return !(prevNode <= 0 && nextNode <= 0);
        }

        private void Clear(TreeNodeCollection nodeList)
        {
            if (nodeList == null)
            {
                throw new ArgumentNullException("nodeList");
            }

            foreach (TreeNode node in nodeList)
            {
                if (null == node)
                {
                    continue;
                }
                else if (node.Nodes.Count != 0)
                {
                    Clear(node.Nodes);
                }

                m_nodeList.Remove(((CrmTreeNode)node.Tag).CrmNode.NodeId);
            }

            nodeList.Clear();
        }

        private void CollapseAll(TreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            if (node.Nodes.Count != 0)
            {
                foreach (TreeNode childNode in node.Nodes)
                {
                    if (null == childNode)
                    {
                        continue;
                    }

                    CollapseAll(childNode);
                }
            }

            node.Collapse();
        }

        private bool IncludeType(CrmTreeNodeType type)
        {
            if (type == CrmTreeNodeType.None)
            {
                return false;
            }
            else if (m_excludeTypes == CrmTreeNodeType.None)
            {
                return true;
            }
            else
            {
                return ((m_excludeTypes & type) != type);
            }
        }

        private void RemoveNodes(TreeNodeCollection parentNodeList)
        {
            if (parentNodeList != null && parentNodeList.Count != 0)
            {
                foreach (TreeNode node in parentNodeList)
                {
                    if (null == node)
                    {
                        //Sometimes the TreeNode in the TreeNodeCollection is null.
                        continue;
                    }

                    if (node.Nodes.Count != 0)
                    {
                        RemoveNodes(node.Nodes);
                    }

                    ICrmTreeNode crmNode = ((CrmTreeNode)node.Tag).CrmNode;
                    m_nodeList.Remove(crmNode.NodeId);
                    parentNodeList.Remove(node);

                    if (null != NodeRemoved)
                    {
                        NodeRemoved(this, new CrmTreeNodeEventArgs(crmNode));
                    }
                }
            }
        }

        private void RetrieveCheckedNodes(TreeNodeCollection nodes, List<ICrmTreeNode> checkedList, bool checkValue)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Checked == checkValue)
                {
                    checkedList.Add(((CrmTreeNode)node.Tag).CrmNode);
                }

                if (checkValue)
                {
                    //The parent node is checked if at least one child is checked. If the parent is not checked
                    //then no child is checked
                    if (SingleCheckParent && !node.Checked)
                    {
                        continue;
                    }
                }
                else
                {
                    //The parent is checked if every child is checked. Since the parent is checked, no
                    //child is checked
                    if (!SingleCheckParent && node.Checked)
                    {
                        continue;
                    }
                }

                //Retrieve the checked children
                if (node.Nodes.Count != 0)
                {
                    RetrieveCheckedNodes(node.Nodes, checkedList, checkValue);
                }
            }
        }

        private TreeNodeCollection RetrieveParentNodeCollection(Guid parentId)
        {
            if (parentId == Guid.Empty)
            {
                return trvPlugins.Nodes;
            }
            else if (HasNode(parentId))
            {
                return m_nodeList[parentId].TreeNode.Nodes;
            }
            else
            {
                throw new ArgumentException("Invalid Node Id", "parentId");
            }
        }

        private TreeNodeCollection RetrieveParentNodeCollection(TreeNode parentNode)
        {
            if (parentNode == null)
            {
                return trvPlugins.Nodes;
            }
            else
            {
                return parentNode.Nodes;
            }
        }

        private bool Search(TreeNode node, List<TreeNode> matchedNodes, List<TreeNode> unmatchedNodes, string text)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            //Loop through the child nodes to see if the data is found in one of those nodes
            bool foundAtLeastOne = false;
            foreach (TreeNode childNode in node.Nodes)
            {
                if (Search(childNode, matchedNodes, unmatchedNodes, text))
                {
                    foundAtLeastOne = true;
                }
            }

            //If at least one child has a match, then this node is considered a match (so that all of a matching node's ancestors are displayed)
            bool matchFound = foundAtLeastOne;

            //Check if the text is an id. If it is, compare it to the id for this node
            if (!matchFound)
            {
                //Convert the node id to text
                string nodeIdText = ((CrmTreeNode)node.Tag).CrmNode.NodeId.ToString();

                //Check if the node id is found in the search text
                matchFound = nodeIdText.IndexOf(text, StringComparison.OrdinalIgnoreCase) != -1;
            }

            //If a match has not been found, look for the text in the text of the node
            if (!matchFound)
            {
                matchFound = node.Text.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) != -1;
            }

            //Process the node
            if (matchFound)
            {
                if (matchedNodes != null)
                {
                    matchedNodes.Add(node);
                }
            }
            else
            {
                if (unmatchedNodes != null)
                {
                    unmatchedNodes.Add(node);
                }
            }

            return matchFound;
        }

        private void Sort(Guid nodeId)
        {
            if (!HasNode(nodeId))
            {
                throw new ArgumentException("Invalid Node Id", "nodeId");
            }

            TreeNode currentNode = m_nodeList[nodeId].TreeNode;
            TreeNodeCollection parentNodes = RetrieveParentNodeCollection(currentNode.Parent);

            if (parentNodes.Count > 1)
            {
                int newIndex = currentNode.Index;

                //Start looping towards the end
                for (int i = newIndex + 1; i < parentNodes.Count; i++)
                {
                    if (null == parentNodes[i])
                    {
                        continue;
                    }

                    //Compare the nodes
                    int result = m_sorter.Compare(currentNode, parentNodes[i]);

                    //If currentNode > parentNodes[i], have to keep looking
                    if (result > 0)
                    {
                        newIndex++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentNode.Index == newIndex)
                {
                    //If we are anywhere else, start looping towards the beginning
                    for (int i = newIndex - 1; i >= 0; i--)
                    {
                        if (null == parentNodes[i])
                        {
                            continue;
                        }

                        //If parentNodes[i] > current, have to keep looking
                        if (m_sorter.Compare(parentNodes[i], currentNode) > 0)
                        {
                            newIndex--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                //If the index has changed, readd
                if (newIndex != currentNode.Index)
                {
                    TreeNode selectedNode = trvPlugins.SelectedNode;
                    m_disableSelectionChange = true;

                    try
                    {
                        parentNodes.RemoveAt(currentNode.Index);
                        parentNodes.Insert(newIndex, currentNode);
                    }
                    finally
                    {
                        m_disableSelectionChange = false;
                    }

                    trvPlugins.SelectedNode = selectedNode;
                }
            }
        }

        private void trvPlugins_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!m_disableCheckChange)
            {
                try
                {
                    m_disableCheckChange = true;

                    CrmTreeNode node = (CrmTreeNode)e.Node.Tag;
                    node.Checked = e.Node.Checked;
                    if (CheckStateChanged != null)
                    {
                        CheckStateChanged(this, new CrmTreeNodeEventArgs(node.CrmNode));
                    }
                }
                finally
                {
                    m_disableCheckChange = false;
                }
            }
        }

        private void trvPlugins_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            CrmTreeNode treeNode = (CrmTreeNode)e.Node.Tag;

            if (!IsNodeTextEditable(treeNode.CrmNode))
            {
                e.CancelEdit = true;
                if (null != LabelEditCanceled)
                {
                    LabelEditCanceled(this, new CrmTreeNodeEventArgs(treeNode.CrmNode));
                }
                return;
            }

            ICrmEditableTreeNode node = (ICrmEditableTreeNode)treeNode.CrmNode;

            bool canceled = false;
            if (string.Equals(node.NodeEditText, e.Label, StringComparison.CurrentCulture) ||
                string.Equals(node.NodeText, e.Label, StringComparison.CurrentCulture))
            {
                canceled = true;
            }
            else
            {
                if (string.IsNullOrEmpty(e.Label))
                {
                    canceled = true;
                }
                else if (AfterLabelEdit != null)
                {
                    CrmTreeNodeLabelEditEventArgs args = new CrmTreeNodeLabelEditEventArgs(node,
                        node.GetNodeEditTextForLabel(e.Label));
                    AfterLabelEdit(this, args);
                    canceled = args.CancelEdit;
                }

                if (!canceled)
                {
                    node.NodeEditText = e.Label;
                    e.Node.Text = node.NodeText;
                    e.CancelEdit = true;
                }
            }

            if (canceled)
            {
                e.CancelEdit = true;
            }

            m_currentlyEditing = Guid.Empty;

            if (canceled)
            {
                if (null != LabelEditCanceled)
                {
                    LabelEditCanceled(this, new CrmTreeNodeEventArgs(node));
                }
            }
            else
            {
                if (null != LabelEdited)
                {
                    LabelEdited(this, new CrmTreeNodeEventArgs(node));
                }
            }
        }

        private void trvPlugins_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!m_disableSelectionChange && SelectionChanged != null)
            {
                SelectionChanged(this, new CrmTreeNodeTreeEventArgs(((CrmTreeNode)e.Node.Tag).CrmNode, e.Action));
            }
        }

        private void trvPlugins_BeforeLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            //Verify that the node can be edited
            CrmTreeNode treeNode = (CrmTreeNode)e.Node.Tag;
            if (!IsNodeTextEditable(treeNode.CrmNode))
            {
                e.CancelEdit = true;
                m_currentlyEditing = Guid.Empty;
                return;
            }

            ICrmEditableTreeNode node = (ICrmEditableTreeNode)treeNode.CrmNode;

            if (BeforeLabelEdit != null)
            {
                CrmTreeNodeLabelEditEventArgs args = new CrmTreeNodeLabelEditEventArgs(node, node.NodeEditText);
                BeforeLabelEdit(this, args);
                e.CancelEdit = args.CancelEdit;
            }

            if (!e.CancelEdit)
            {
                m_currentlyEditing = node.NodeId;
            }
        }

        private void trvPlugins_Click(object sender, EventArgs e)
        {
            TreeNode node = trvPlugins.HitTest(((MouseEventArgs)e).Location).Node;
            if (node != null)
            {
                if (Click != null)
                {
                    Click(this, new CrmTreeNodeEventArgs(((CrmTreeNode)node.Tag).CrmNode));
                }
            }
        }

        private void trvPlugins_DoubleClick(object sender, EventArgs e)
        {
            TreeNode node = trvPlugins.HitTest(((MouseEventArgs)e).Location).Node;
            if (node != null)
            {
                if (DoubleClick != null)
                {
                    DoubleClick(this, new CrmTreeNodeEventArgs(((CrmTreeNode)node.Tag).CrmNode));
                }
            }
        }

        private void trvPlugins_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyDown != null)
            {
                KeyDown(this, e);
            }
        }

        private void trvPlugins_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (KeyPress != null)
            {
                KeyPress(this, e);
            }
        }

        private void trvPlugins_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyUp != null)
            {
                KeyUp(this, e);
            }
        }

        private void trvPlugins_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && (ContextMenu != null || ContextMenuStrip != null))
            {
                TreeNode node = trvPlugins.HitTest(e.Location).Node;
                if (node != null)
                {
                    trvPlugins.SelectedNode = node;
                }
            }
        }

        #endregion Private Methods

        #region Private Classes

        private sealed class CrmTreeNode
        {
            #region Private Fields

            private Dictionary<Guid, CrmTreeNode> m_checkedList = new Dictionary<Guid, CrmTreeNode>();
            private ICrmTreeNode m_crmNode;
            private TreeNode m_node;
            private bool m_singleCheckParent;

            #endregion Private Fields

            #region Public Constructors

            public CrmTreeNode(bool singleCheckParent, TreeNode node, ICrmTreeNode crmNode)
            {
                m_singleCheckParent = singleCheckParent;
                m_node = node;
                m_crmNode = crmNode;
                ReloadChildren();
            }

            #endregion Public Constructors

            #region Public Properties

            public bool Checked
            {
                get
                {
                    return m_node.Checked;
                }

                set
                {
                    UpdateParentChecked();
                    UpdateChildChecked();
                }
            }

            public ICrmTreeNode CrmNode
            {
                get
                {
                    return m_crmNode;
                }
            }

            public CrmTreeNode ParentNode
            {
                get
                {
                    if (m_node == null)
                    {
                        return null;
                    }
                    else
                    {
                        return (CrmTreeNode)m_node.Parent.Tag;
                    }
                }
            }

            public TreeNode TreeNode
            {
                get
                {
                    return m_node;
                }
            }

            #endregion Public Properties

            #region Public Methods

            public void ReloadChildren()
            {
                m_checkedList.Clear();

                foreach (TreeNode childNode in m_node.Nodes)
                {
                    if (childNode.Checked)
                    {
                        CrmTreeNode checkedNode = (CrmTreeNode)childNode.Tag;
                        m_checkedList.Add(checkedNode.m_crmNode.NodeId, checkedNode);
                    }
                }
            }

            public void UpdateChildChecked()
            {
                if (m_node.Checked)
                {
                    foreach (TreeNode node in m_node.Nodes)
                    {
                        CrmTreeNode childNode = (CrmTreeNode)node.Tag;
                        if (!childNode.Checked)
                        {
                            childNode.m_node.Checked = true;
                            childNode.UpdateChildChecked();

                            if (!m_checkedList.ContainsKey(childNode.m_crmNode.NodeId))
                            {
                                m_checkedList.Add(childNode.m_crmNode.NodeId, childNode);
                            }
                        }
                    }
                }
                else
                {
                    List<Guid> removeList = new List<Guid>();
                    foreach (CrmTreeNode childNode in m_checkedList.Values)
                    {
                        if (childNode.Checked)
                        {
                            childNode.m_node.Checked = false;
                            childNode.UpdateChildChecked();

                            if (m_checkedList.ContainsKey(childNode.m_crmNode.NodeId))
                            {
                                removeList.Add(childNode.m_crmNode.NodeId);
                            }
                        }
                    }

                    foreach (Guid nodeId in removeList)
                    {
                        m_checkedList.Remove(nodeId);
                    }
                }
            }

            public void UpdateParentChecked()
            {
                if (m_node.Parent == null)
                {
                    return;
                }

                CrmTreeNode parentNode = ParentNode;
                if (Checked && !parentNode.m_checkedList.ContainsKey(CrmNode.NodeId))
                {
                    parentNode.m_checkedList.Add(CrmNode.NodeId, this);
                }
                else if (!Checked && parentNode.m_checkedList.ContainsKey(CrmNode.NodeId))
                {
                    parentNode.m_checkedList.Remove(CrmNode.NodeId);
                }

                bool parentChecked;
                if (m_singleCheckParent)
                {
                    parentChecked = (parentNode.m_checkedList.Count != 0);
                }
                else
                {
                    parentChecked = (parentNode.m_checkedList.Count == parentNode.m_node.Nodes.Count);
                }

                if (parentNode.m_node.Checked != parentChecked)
                {
                    parentNode.m_node.Checked = parentChecked;
                    parentNode.UpdateParentChecked();
                }
            }

            #endregion Public Methods
        }

        private sealed class NodeSorter : IComparer
        {
            #region Private Fields

            private IComparer<ICrmTreeNode> m_sorter = null;

            #endregion Private Fields

            #region Public Properties

            public IComparer<ICrmTreeNode> Sorter
            {
                get
                {
                    return m_sorter;
                }

                set
                {
                    m_sorter = value;
                }
            }

            #endregion Public Properties

            #region Public Methods

            public int Compare(object x, object y)
            {
                ICrmTreeNode node1 = GetCrmNode(x);
                ICrmTreeNode node2 = GetCrmNode(y);

                if (m_sorter == null)
                {
                    return string.Compare(GetNodeText(node1), GetNodeText(node2), false);
                }
                else
                {
                    return m_sorter.Compare(node1, node2);
                }
            }

            #endregion Public Methods

            #region Private Methods

            private ICrmTreeNode GetCrmNode(object item)
            {
                if (item == null)
                {
                    return null;
                }
                else if (item.GetType() == typeof(TreeNode))
                {
                    TreeNode node = (TreeNode)item;
                    return ((CrmTreeNode)node.Tag).CrmNode;
                }
                else
                {
                    throw new NotSupportedException("Invalid node in the tree");
                }
            }

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

        #endregion Private Classes
    }

    public sealed class CrmTreeNodeEventArgs : EventArgs
    {
        #region Private Fields

        private ICrmTreeNode m_node;

        #endregion Private Fields

        #region Public Constructors

        public CrmTreeNodeEventArgs(ICrmTreeNode node)
            : base()
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            m_node = node;
        }

        #endregion Public Constructors

        #region Public Properties

        public ICrmTreeNode Node
        {
            get
            {
                return m_node;
            }
        }

        #endregion Public Properties
    }

    public sealed class CrmTreeNodeLabelEditEventArgs : EventArgs
    {
        #region Private Fields

        private bool m_cancelEdit = false;
        private string m_label;
        private ICrmTreeNode m_node;

        #endregion Private Fields

        #region Public Constructors

        public CrmTreeNodeLabelEditEventArgs(ICrmTreeNode node, string label)
            : base()
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            m_node = node;
            m_label = label;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool CancelEdit
        {
            get
            {
                return m_cancelEdit;
            }
            set
            {
                m_cancelEdit = value;
            }
        }

        public string Label
        {
            get
            {
                return m_label;
            }
        }

        public ICrmTreeNode Node
        {
            get
            {
                return m_node;
            }
        }

        #endregion Public Properties
    }

    public sealed class CrmTreeNodeTreeEventArgs : EventArgs
    {
        #region Private Fields

        private TreeViewAction m_action;
        private ICrmTreeNode m_node;

        #endregion Private Fields

        #region Public Constructors

        public CrmTreeNodeTreeEventArgs(ICrmTreeNode node, TreeViewAction action)
            : base()
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            m_action = action;
            m_node = node;
        }

        #endregion Public Constructors

        #region Public Properties

        public TreeViewAction Action
        {
            get
            {
                return m_action;
            }
        }

        public ICrmTreeNode Node
        {
            get
            {
                return m_node;
            }
        }

        #endregion Public Properties
    }
}
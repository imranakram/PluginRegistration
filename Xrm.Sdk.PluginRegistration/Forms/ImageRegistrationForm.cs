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
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Forms;
    using Wrappers;

    public partial class ImageRegistrationForm : Form
    {
        #region Private Fields

        private CrmPluginImage m_currentImage;
        private CrmOrganization m_org;
        private MainControl m_orgControl;
        private ToolTip _toolTip; // Tooltip for "Pre" checkbox when it is disabled for Create message

        #endregion Private Fields

        #region Public Constructors

        public ImageRegistrationForm(CrmOrganization org, MainControl orgControl,
            List<ICrmTreeNode> rootNodes, CrmPluginImage image, Guid selectNodeId)
        {
            if (org == null)
            {
                throw new ArgumentNullException("org");
            }
            else if (orgControl == null)
            {
                throw new ArgumentNullException("orgControl");
            }
            else if (rootNodes == null)
            {
                throw new ArgumentNullException("rootNodes");
            }
            m_org = org;
            m_orgControl = orgControl;
            m_currentImage = image;

            InitializeComponent();

            _toolTip = new ToolTip();

            crmParameters.Organization = org;

            trvPlugins.CrmTreeNodeSorter = orgControl.CrmTreeNodeSorter;

            trvPlugins.AutoExpand = m_orgControl.IsAutoExpanded;
            trvPlugins.LoadNodes(rootNodes);

            CrmPluginStep step = null;

            if (image != null)
            {
                if (trvPlugins.HasNode(image.StepId))
                {
                    trvPlugins.SelectedNode = trvPlugins[image.StepId];
                }

                txtEntityAlias.Text = image.EntityAlias;
                txtName.Text = image.Name;

                step = m_org.Steps[image.StepId];
                // Set PreImage checkbox state
                SetPreImageCheckboxForStep(step);

                if (step.MessageEntityId == Guid.Empty)
                {
                    crmParameters.EntityName = "none";
                }
                else
                {
                    crmParameters.EntityName = m_org.Messages[step.MessageId][step.MessageEntityId].PrimaryEntity;
                    crmParameters.Attributes = image.Attributes;
                }

                switch (image.ImageType)
                {
                    case CrmPluginImageType.Both:
                        chkImageTypePost.Checked = true;
                        chkImageTypePre.Checked = true;
                        break;

                    case CrmPluginImageType.PostImage:
                        chkImageTypePost.Checked = true;
                        chkImageTypePre.Checked = false;
                        break;

                    case CrmPluginImageType.PreImage:
                        chkImageTypePost.Checked = false;
                        chkImageTypePre.Checked = true;
                        break;

                    default:
                        throw new NotImplementedException("ImageType = " + image.ImageType.ToString());
                }

                Text = "Update Existing Image";
                btnRegister.Text = "Update";

                trvPlugins.Visible = false;

                int difference = grpEntityAlias.Top - grpSteps.Top;

                grpSteps.Visible = false;
                Height -= difference;

                btnRegister.Enabled = true;
            }
            else
            {
                crmParameters.ClearAttributes();
                if (trvPlugins.HasNode(selectNodeId))
                {
                    // Opened from a step, or from a node (endpoint, plug-in, assembly...) with a single step under it
                    step = FindOnlyStep(trvPlugins[selectNodeId]);
                    if (step != null && trvPlugins.HasNode(step.NodeId))
                    {
                        trvPlugins.SelectedNode = trvPlugins[step.NodeId];
                        // Runs the selection logic (Register button, entity, pre-image) even if the tree
                        // has no window yet and so raises no event.
                        trvPlugins_SelectionChanged(trvPlugins, new CrmTreeNodeTreeEventArgs(step, TreeViewAction.Unknown));
                    }
                    else
                    {
                        step = null;
                    }
                }
            }
            crmParameters.Enabled = step != null && !step.MessageEntityId.Equals(Guid.Empty) && !org.MessageEntities[step.MessageEntityId].PrimaryEntity.Equals("none");
            if (!crmParameters.Enabled)
            {   // Force all atttibutes if disabled
                crmParameters.Attributes = null;
            }
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// The step to pre-select: the node itself when it is a step, otherwise the only step found
        /// beneath it. Null when there are none or more than one, so the user picks.
        /// </summary>
        public static CrmPluginStep FindOnlyStep(ICrmTreeNode node)
        {
            if (node == null)
            {
                return null;
            }
            if (node is CrmPluginStep self)
            {
                return self;
            }

            CrmPluginStep found = null;
            var pending = new Stack<ICrmTreeNode>(node.NodeChildren ?? new List<ICrmTreeNode>());
            while (pending.Count != 0)
            {
                var current = pending.Pop();
                if (current is CrmPluginStep step)
                {
                    if (found != null)
                    {
                        return null;
                    }
                    found = step;
                    continue; // images under a step aren't steps
                }
                foreach (var child in current.NodeChildren ?? new List<ICrmTreeNode>())
                {
                    pending.Push(child);
                }
            }
            return found;
        }

        /// <summary>
        /// A record has no state before Create, so a post-image is the only image type its steps can have.
        /// </summary>
        public static bool OnlyPostImageAllowed(string messageName) =>
            string.Equals(messageName, "Create", StringComparison.OrdinalIgnoreCase);

        #endregion Public Methods

        #region Private Methods

        private void btnRegister_Click(object sender, EventArgs e)
        {
            CrmPluginImage image = new CrmPluginImage(m_org);

            #region Extract Information

            if (trvPlugins.SelectedNode == null ||
                (m_currentImage == null && trvPlugins.SelectedNode.NodeType != CrmTreeNodeType.Step))
            {
                MessageBox.Show("A Step must be selected when registering an image",
                    "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (!chkImageTypePost.Checked && !chkImageTypePre.Checked)
            {
                MessageBox.Show("At least one Image Type must be specified. This field is required.",
                    "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (txtEntityAlias.TextLength == 0 || txtEntityAlias.Text.Trim().Length == 0)
            {
                MessageBox.Show("You must enter an alias for this Image. This is a required field.", "Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (!crmParameters.HasAttributes)
            {
                MessageBox.Show("You must specify at least one attribute. This is a required field", "Registration",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Retrieve the step for this image
            CrmPluginStep step;
            if (m_currentImage == null)
            {
                step = (CrmPluginStep)trvPlugins.SelectedNode;

                //Verify that the step is not a system item
                if (step.IsSystemCrmEntity)
                {
                    m_orgControl.ShowSystemItemError("Cannot register image on this step");
                    return;
                }
            }
            else
            {
                step = m_org.Steps[m_currentImage.StepId];
            }

            //Retrieve the message
            CrmMessage message = m_org.Messages[step.MessageId];

            //Verify that this step can have images
            if (0 == message.ImageMessagePropertyNames.Count)
            {
                //Create a list of the messages that can have images
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Only steps registered on the following messages can have images:");
                foreach (CrmMessage item in m_org.Messages.Values)
                {
                    if (0 != item.ImageMessagePropertyNames.Count)
                    {
                        sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "- {0}{1}",
                            item.Name, Environment.NewLine);
                    }
                }

                sb.AppendLine();
                sb.Append("Please select a different step and try again.");

                MessageBox.Show(sb.ToString(), "Invalid Step Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (crmParameters.AllAttributes)
            {
                if (MessageBox.Show("Registering images with ALL attributes is highly discouraged for performance reasons.\nPlease reconsider this pattern.\n\nYes, I want to specify explicit attributes.\nNo, I don't care about performance now.", "Registration", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    return;
                }
            }

            //Start populating the image that will be used
            image.AssemblyId = step.AssemblyId;
            image.PluginId = step.PluginId;
            image.StepId = step.StepId;
            image.Attributes = crmParameters.Attributes;
            image.EntityAlias = txtEntityAlias.Text.Trim();
            image.Name = txtName.Text.Trim();
            image.MessagePropertyName = MessagePropertyNameForm.SelectMessagePropertyName(message);
            if (null == image.MessagePropertyName)
            {
                return;
            }

            if (chkImageTypePre.Checked && chkImageTypePost.Checked)
            {
                image.ImageType = CrmPluginImageType.Both;
            }
            else if (chkImageTypePre.Checked)
            {
                image.ImageType = CrmPluginImageType.PreImage;
            }
            else
            {
                image.ImageType = CrmPluginImageType.PostImage;
            }

            #endregion Extract Information

            #region Register the Image

            try
            {
                if (m_currentImage == null)
                {
                    image.ImageId = RegistrationHelper.RegisterImage(m_org, image);

                    List<ICrmEntity> entityList = new List<ICrmEntity>(new ICrmEntity[] { image });
                    OrganizationHelper.UpdateDates(m_org, entityList);

                    step.AddImage(image);
                    m_orgControl.AddImage(image);
                }
                else
                {
                    image.ImageId = m_currentImage.ImageId;
                    RegistrationHelper.UpdateImage(m_org, image);

                    m_currentImage.AssemblyId = step.AssemblyId;
                    m_currentImage.PluginId = step.PluginId;
                    m_currentImage.StepId = step.StepId;
                    m_currentImage.ImageId = image.ImageId;
                    m_currentImage.Attributes = image.Attributes;
                    m_currentImage.EntityAlias = image.EntityAlias;
                    m_currentImage.MessagePropertyName = image.MessagePropertyName;
                    m_currentImage.ImageType = image.ImageType;

                    image = m_currentImage;

                    List<ICrmEntity> entityList = new List<ICrmEntity>(new ICrmEntity[] { image });
                    OrganizationHelper.UpdateDates(m_org, entityList);

                    m_orgControl.RefreshImage(m_currentImage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessageForm.ShowErrorMessageBox(this, "Unable to register the Image due to an error.", "Registration", ex);
                return;
            }

            #endregion Register the Image

            DialogResult = DialogResult.OK;
            Close();
        }

        private void trvPlugins_SelectionChanged(object sender, CrmTreeNodeTreeEventArgs e)
        {
            if ((m_currentImage == null && e.Node.NodeType == CrmTreeNodeType.Step) ||
                (m_currentImage != null && e.Node.NodeType == CrmTreeNodeType.Image))
            {
                CrmPluginStep step;
                switch (e.Node.NodeType)
                {
                    case CrmTreeNodeType.Step:
                        step = (CrmPluginStep)e.Node;
                        break;

                    case CrmTreeNodeType.Image:
                        step = m_org.Steps[((CrmPluginImage)e.Node).StepId];
                        break;

                    default:
                        throw new NotImplementedException("NodeType = " + e.Node.NodeType.ToString());
                }
                if (step.MessageEntityId != Guid.Empty)
                {
                    crmParameters.EntityName = m_org.Messages[step.MessageId][step.MessageEntityId].PrimaryEntity;
                }
                else
                {
                    crmParameters.EntityName = "none";
                    crmParameters.Enabled = false;
                }
                btnRegister.Enabled = true;
                // Set PreImage checkbox state for selection
                SetPreImageCheckboxForStep(step);
            }
            else
            {
                crmParameters.Attributes = null;
                btnRegister.Enabled = false;
                // Reset PreImage checkbox state
                chkImageTypePre.Enabled = true;
                _toolTip.SetToolTip(chkImageTypePre, string.Empty);
                chkImageTypePre.Tag = null;
            }
        }

        // Helper to enable/disable PreImage checkbox for Create message
        private void SetPreImageCheckboxForStep(CrmPluginStep step)
        {
            if (step == null)
                return;
            var message = m_org.Messages[step.MessageId];
            if (OnlyPostImageAllowed(message.Name))
            {
                chkImageTypePre.Checked = false;
                chkImageTypePre.Enabled = false;
                // The only valid choice, so it's made for the user
                chkImageTypePost.Checked = true;
                //Note: The tooltip just does not show up on disabled controls, leaving it as-is for now.
                _toolTip.SetToolTip(chkImageTypePre, "Message Create does not support this image type.");
                chkImageTypePre.Tag = "Message Create does not support this image type.";
            }
            else
            {
                chkImageTypePre.Enabled = true;
                _toolTip.SetToolTip(chkImageTypePre, string.Empty);
                chkImageTypePre.Tag = null;
            }
        }
       
        #endregion Private Methods


    }
}
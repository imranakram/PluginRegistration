using System.Collections.Generic;
using System.Windows.Forms;

namespace Xrm.Sdk.PluginRegistration
{
    public partial class MainControl
    {
        private int connectionGeneration;
        private bool connectionReady;
        private bool registrationControlsEnabled;
        private bool organizationLoading;

        private bool CanRunRegistrationCommand => registrationControlsEnabled && connectionReady && m_org != null;

        private void ResetConnectionState()
        {
            // An unsuccessful connection change must not leave commands targeting the previous organization.
            connectionReady = false;
            SetRegistrationControlsEnabled(false);
            m_org = null;
            m_con = null;
            m_rootNodeList = null;
            m_stepParentList = null;
            m_viewNodeList = null;
            trvPlugins.LoadNodes(new List<Controls.ICrmTreeNode>());
            propGridEntity.SelectedObject = null;
            grvData.DataSource = null;
            grpGrid.Visible = false;
            SelectItem(null);
        }

        private void SetRegistrationControlsEnabled(bool enabled)
        {
            // XrmToolBox adds its loading overlay to this control. Disabling the parent also
            // disables that overlay, so block only our own controls during loading.
            registrationControlsEnabled = enabled;
            foreach (ToolStripItem item in toolBar.Items) item.Enabled = enabled;
            toolClose.Enabled = true;
            toolRefresh.Enabled = !organizationLoading && (enabled || !connectionReady) && Service != null && ConnectionDetail != null;
            tsbFilterAssemblies.Enabled = !organizationLoading && (enabled || !connectionReady) && m_settings != null && ConnectionDetail != null;
            splitterDisplay.Enabled = enabled;
            grpGrid.Enabled = enabled;
            mnuContextNode.Enabled = enabled;
            mnuContextGeneral.Enabled = enabled;
            if (enabled) SelectItem(trvPlugins.SelectedNode);
        }
    }
}

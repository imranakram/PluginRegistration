using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.Connections
{
    [TestFixture]
    public class ConnectionStateTests
    {
        [Test, Apartment(ApartmentState.STA)]
        public void ConnectionResetRemovesPreviousOrganizationAndDisablesItsCommands()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var control = new MainControl())
            {
                var type = typeof(MainControl);
                var tree = (CrmTreeControl)type.GetField("trvPlugins", flags).GetValue(control);
                var assembly = new CrmPluginAssembly(null) { AssemblyId = Guid.NewGuid(), Name = "Old.Assembly" };
                tree.LoadNodes(new List<ICrmTreeNode> { assembly });
                type.GetField("connectionReady", flags).SetValue(control, true);
                type.GetField("m_org", flags).SetValue(control, FormatterServices.GetUninitializedObject(typeof(CrmOrganization)));
                control.Enabled = true;
                // XrmToolBox attaches its progress panel directly to the plugin control.
                var loadingPanel = new Panel();
                var animation = new PictureBox();
                loadingPanel.Controls.Add(animation);
                control.Controls.Add(loadingPanel);

                type.GetMethod("ResetConnectionState", flags).Invoke(control, null);

                Assert.That(type.GetField("connectionReady", flags).GetValue(control), Is.False);
                Assert.That(type.GetField("m_org", flags).GetValue(control), Is.Null);
                Assert.That(control.Enabled, Is.True, "The host's loading overlay must not inherit a disabled parent.");
                Assert.That(animation.Enabled, Is.True);
                Assert.That(tree.RootNodes, Is.Empty);
                var register = (ToolStripDropDownButton)type.GetField("toolRegister", flags).GetValue(control);
                Assert.That(register.Enabled, Is.False);
                var update = (ToolStripButton)type.GetField("toolUpdate", flags).GetValue(control);
                Assert.That(update.Enabled, Is.False);
                var splitter = (SplitContainer)type.GetField("splitterDisplay", flags).GetValue(control);
                Assert.That(splitter.Enabled, Is.False);
                var contextMenu = (ContextMenuStrip)type.GetField("mnuContextGeneral", flags).GetValue(control);
                Assert.That(contextMenu.Enabled, Is.False);
                Assert.That(((ToolStrip)type.GetField("toolBar", flags).GetValue(control)).Enabled, Is.True);
                Assert.That(((ToolStripButton)type.GetField("toolClose", flags).GetValue(control)).Enabled, Is.True);
                Assert.That(((ToolStripButton)type.GetField("toolRefresh", flags).GetValue(control)).Enabled, Is.False,
                    "Refresh needs a connection, but Close must always work.");
                type.GetMethod("SetRegistrationControlsEnabled", flags).Invoke(control, new object[] { true });
                Assert.That(splitter.Enabled, Is.True);
                Assert.That(register.Enabled, Is.True);
                Assert.That(contextMenu.Enabled, Is.True);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void FailedInitialLoadKeepsRefreshAndFiltersAvailableForRecovery()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var control = new MainControl())
            {
                var type = typeof(MainControl);
                type.BaseType.GetField("<Service>k__BackingField", flags).SetValue(control, new RecordingOrganizationService());
                var connection = type.GetProperty("ConnectionDetail");
                connection.SetValue(control, Activator.CreateInstance(connection.PropertyType));
                type.GetField("m_settings", flags).SetValue(control, new Settings());
                type.GetMethod("ResetConnectionState", flags).Invoke(control, null);

                Assert.That(((ToolStripButton)type.GetField("toolRefresh", flags).GetValue(control)).Enabled, Is.True);
                Assert.That(((ToolStripButton)type.GetField("tsbFilterAssemblies", flags).GetValue(control)).Enabled, Is.True);
                Assert.That(((ToolStripButton)type.GetField("toolClose", flags).GetValue(control)).Enabled, Is.True);
                Assert.That(type.GetProperty("CanRunRegistrationCommand", flags).GetValue(control), Is.False);
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void LoadingBlocksRegistrationShortcutsAndKeepsCloseAvailable()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var control = new MainControl())
            {
                var type = typeof(MainControl);
                type.GetField("connectionReady", flags).SetValue(control, true);
                type.GetField("m_org", flags).SetValue(control, FormatterServices.GetUninitializedObject(typeof(CrmOrganization)));
                type.GetField("organizationLoading", flags).SetValue(control, true);
                type.GetMethod("SetRegistrationControlsEnabled", flags).Invoke(control, new object[] { false });
                int registrations = 0;
                var step = (ToolStripMenuItem)type.GetField("toolStepRegister", flags).GetValue(control);
                step.Click += (sender, args) => registrations++;
                control.ReceiveKeyDownShortcut(new KeyEventArgs(Keys.Control | Keys.T));
                control.ReceiveKeyDownShortcut(new KeyEventArgs(Keys.Delete));
                control.ReceiveKeyDownShortcut(new KeyEventArgs(Keys.F5));

                Assert.That(registrations, Is.Zero);
                Assert.That(type.GetProperty("CanRunRegistrationCommand", flags).GetValue(control), Is.False);
                Assert.That(((ToolStripButton)type.GetField("toolRefresh", flags).GetValue(control)).Enabled, Is.False);
                Assert.That(((ToolStripButton)type.GetField("toolClose", flags).GetValue(control)).Enabled, Is.True);

                type.GetField("organizationLoading", flags).SetValue(control, false);
                type.GetMethod("SetRegistrationControlsEnabled", flags).Invoke(control, new object[] { true });
                Assert.That(type.GetProperty("CanRunRegistrationCommand", flags).GetValue(control), Is.True);
            }
        }
    }
}

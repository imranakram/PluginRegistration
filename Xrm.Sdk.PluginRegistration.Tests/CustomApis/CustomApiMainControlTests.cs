using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    /// <summary>
    /// Covers how the main window treats a Custom API node: which commands it offers, where the
    /// node's commands are sent, and where a step on the API's message is placed. Everything here
    /// stops short of the modal dialogs and background work, which need a host and a user.
    /// </summary>
    [TestFixture, Apartment(ApartmentState.STA)]
    public class CustomApiMainControlTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string RegisterText = "Register New Custom API";
        private static readonly Type Main = typeof(MainControl);

        [Test]
        public void RegisterCommandIsOfferedInEveryMenuAndFollowsCustomApiSupport()
        {
            using (var control = new MainControl())
            {
                // The context menus are disabled as a whole until there is a connection.
                Connect(control);
                var items = new[]
                {
                    Field<ToolStripDropDownButton>(control, "toolRegister").DropDownItems,
                    Field<ContextMenuStrip>(control, "mnuContextNode").Items,
                    Field<ContextMenuStrip>(control, "mnuContextGeneral").Items
                }.Select(menu => menu.Cast<ToolStripItem>().Where(item => item.Text == RegisterText).ToList()).ToList();

                Assert.That(items.Select(found => found.Count), Is.All.EqualTo(1), "One register command per menu");
                var commands = items.SelectMany(found => found).ToList();
                Assert.That(commands.Select(item => item.Enabled), Is.All.False, "Support is unknown until the catalog loads");

                ApplyCatalog(control, supported: true);
                Assert.That(commands.Select(item => item.Enabled), Is.All.True);

                ApplyCatalog(control, supported: false);
                Assert.That(commands.Select(item => item.Enabled), Is.All.False);

                // A new connection may not support Custom APIs, so the old answer must not carry over.
                ApplyCatalog(control, supported: true);
                Invoke(control, "ResetConnectionState");
                Connect(control);
                Assert.That(commands.Select(item => item.Enabled), Is.All.False);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SelectingAnApiOffersUpdateAndAddToSolutionAndProtectsManagedApis(bool managed)
        {
            using (var control = new MainControl())
            {
                // The context menu is disabled as a whole until there is a connection.
                Connect(control);
                var api = Api(managed);
                var step = Step(api);
                api.AddStep(step);
                Field<CrmTreeControl>(control, "trvPlugins").LoadNodes(new List<ICrmTreeNode> { api });

                Select(control, api);

                // Available rather than Visible: an item on a toolbar that is not on screen never reports Visible.
                foreach (var update in new[] { Item(control, "toolUpdate"), Item(control, "mnuContextNodeUpdate") })
                {
                    Assert.That(update.Available, Is.True, update.Name);
                    Assert.That(update.Enabled, Is.True, update.Name + ": a managed API can still be opened");
                }
                foreach (var unregister in new[] { Item(control, "toolUnregister"), Item(control, "mnuContextNodeUnregister") })
                {
                    Assert.That(unregister.Enabled, Is.EqualTo(!managed), unregister.Name);
                }
                Assert.That(Item(control, "toolAddToSolution").Available, Is.True);
                Assert.That(Item(control, "tsmiAddToSolution").Available, Is.True);
                Assert.That(Field<PropertyGrid>(control, "propGridEntity").SelectedObject, Is.SameAs(api));

                // The grid lists the API's steps under the API's own name, not its message ID.
                var rows = GridRows(control);
                Assert.That(rows.Count, Is.EqualTo(1));
                Assert.That(rows[0]["Id"], Is.EqualTo(step.StepId));
                Assert.That(rows[0]["Message"], Is.EqualTo(api.UniqueName));

                Invoke(control, "SelectItem", new object[] { null });
                Assert.That(Item(control, "toolUpdate").Available, Is.False);
                Assert.That(Item(control, "toolAddToSolution").Available, Is.False);
                Assert.That(Item(control, "toolUnregister").Enabled, Is.False);
            }
        }

        [Test]
        public void ApiWithoutStepsShowsAnEmptyStepGrid()
        {
            using (var control = new MainControl())
            {
                var api = Api(managed: false);
                Field<CrmTreeControl>(control, "trvPlugins").LoadNodes(new List<ICrmTreeNode> { api });

                Select(control, api);

                Assert.That(GridRows(control), Is.Empty);
                Assert.That(Item(control, "toolUpdate").Enabled, Is.True);
            }
        }

        [Test]
        public void ApiCommandsAreSentToTheCustomApiHandlersAndDoNothingWithoutSupport()
        {
            using (var control = new MainControl())
            {
                var service = Connect(control);
                var tree = Field<CrmTreeControl>(control, "trvPlugins");
                var api = Api(managed: false);
                tree.LoadNodes(new List<ICrmTreeNode> { api });
                tree.SelectedNode = api;
                Assert.That(tree.SelectedNode, Is.SameAs(api));

                // The catalog failed to load or the organization has no Custom API tables. The ordinary
                // handlers do not know this node type, so reaching them would throw or open a dialog.
                foreach (var handler in new[] { "toolUpdate_Click", "toolUnregister_Click", "tsmiAddToSolution_Click" })
                {
                    Assert.DoesNotThrow(() => Invoke(control, handler, null, EventArgs.Empty), handler);
                }

                Assert.That(service.Requests, Is.Empty);
                Assert.That(service.Queries, Is.Empty);
                Assert.That(service.DeletedRecords, Is.Empty);
                Assert.That(tree.HasNode(api.Id), Is.True);
            }
        }

        [Test]
        public void ApiCommandsAreBlockedWhileRegistrationCommandsAreDisabled()
        {
            using (var control = new MainControl())
            {
                var service = Connect(control);
                ApplyCatalog(control, supported: true);
                var api = Api(managed: false);
                Field<CrmTreeControl>(control, "trvPlugins").LoadNodes(new List<ICrmTreeNode> { api });

                // As during a load or refresh: the organization behind the tree is being replaced.
                Invoke(control, "SetRegistrationControlsEnabled", false);
                Assert.That(Main.GetProperty("CanRunRegistrationCommand", Instance).GetValue(control), Is.False);

                Invoke(control, "OpenCustomApiEditor", new object[] { null });
                Invoke(control, "OpenCustomApiEditor", (Guid?)api.Id);
                Invoke(control, "DeleteCustomApi", api);
                Invoke(control, "AddCustomApiToSolution", api);

                Assert.That(service.Requests, Is.Empty);
                Assert.That(service.Queries, Is.Empty);
                Assert.That(service.DeletedRecords, Is.Empty);
            }
        }

        [Test]
        public void StepOnAnApiMessageIsPlacedUnderTheApiAndRemovingItKeepsTheApi()
        {
            using (var control = new MainControl())
            {
                var tree = Field<CrmTreeControl>(control, "trvPlugins");
                var api = Api(managed: false);
                var parents = ShowCustomApiView(control, api);
                var step = Step(api);

                control.AddStep(step);

                Assert.That(tree.HasNode(step.StepId), Is.True);
                Assert.That(api.NodeChildren.Single(), Is.SameAs(step));
                Assert.That(parents[step.StepId], Is.EqualTo(api.Id));
                Assert.That(tree.CountRootNodes, Is.EqualTo(1), "The step must not create a second root for the API's message");

                // Unregistering the last step of an ordinary message removes the message node. An API
                // exists without steps, so its node has to stay.
                tree.RemoveNode(step.StepId);

                Assert.That(tree.HasNode(step.StepId), Is.False);
                Assert.That(tree.HasNode(api.Id), Is.True);
                Assert.That(api.NodeChildren, Is.Empty);
                Assert.That(parents.ContainsKey(step.StepId), Is.False);
            }
        }

        [Test]
        public void StepAddedWhileItsApiIsSelectedAppearsInTheGrid()
        {
            using (var control = new MainControl())
            {
                var api = Api(managed: false);
                ShowCustomApiView(control, api);
                Select(control, api);
                Assert.That(GridRows(control), Is.Empty);

                var step = Step(api);
                control.AddStep(step);

                Assert.That(GridRows(control).Select(row => row["Id"]), Is.EqualTo(new object[] { step.StepId }));
            }
        }

        [Test]
        public void TheCustomApiViewIsOfferedNextToTheMessageViewWhenSupported()
        {
            using (var control = new MainControl())
            {
                var view = Field<ToolStripDropDownButton>(control, "toolView").DropDownItems;
                var customApiView = Item(control, "toolViewCustomApi");

                Assert.That(view.IndexOf(customApiView), Is.EqualTo(view.IndexOf(Item(control, "toolViewMessage")) + 1));
                Assert.That(customApiView.Tag.ToString(), Is.EqualTo("CustomApi"));
                Assert.That(view.Cast<ToolStripItem>().Select(item => item.Tag), Has.None.Null, "The View menu holds views only");
                Assert.That(customApiView.Enabled, Is.False);

                ApplyCatalog(control, supported: true);
                Assert.That(customApiView.Enabled, Is.True);
                ApplyCatalog(control, supported: false);
                Assert.That(customApiView.Enabled, Is.False);
            }
        }

        [Test]
        public void CustomApisHaveTheirOwnIconAndUpdateHasAFullSizeOne()
        {
            using (var control = new MainControl())
            {
                Assert.That(Api(managed: false).NodeImageType, Is.EqualTo(CrmTreeNodeImageType.CustomApi));
                Assert.That(Api(managed: false).NodeSelectedImageType, Is.EqualTo(CrmTreeNodeImageType.CustomApiSelected));
                Assert.That(CrmResources.LoadImage(CrmTreeNodeImageType.CustomApiSelected), Is.Not.Null);

                var apiIcon = Item(control, "toolViewCustomApi").Image;
                Assert.That(apiIcon, Is.Not.Null);
                Assert.That(apiIcon, Is.Not.SameAs(Item(control, "toolViewMessage").Image), "Custom APIs and messages look different");
                var registerItems = new[]
                {
                    Field<ToolStripDropDownButton>(control, "toolRegister").DropDownItems,
                    Field<ContextMenuStrip>(control, "mnuContextNode").Items,
                    Field<ContextMenuStrip>(control, "mnuContextGeneral").Items
                }.SelectMany(menu => menu.Cast<ToolStripItem>()).Where(item => item.Text == RegisterText).ToList();
                Assert.That(registerItems.Select(item => item.Image), Is.All.SameAs(apiIcon));

                // The toolbar's icons are 24 pixels; a smaller one is blown up and blurred.
                Assert.That(Item(control, "toolUpdate").Image.Size, Is.EqualTo(Item(control, "toolRegister").Image.Size));
            }
        }

        [Test]
        public void TreeAndEnableButtonIconsKeepFullColourAtTheirSize()
        {
            using (var control = new MainControl())
            {
                var tree = Field<CrmTreeControl>(control, "trvPlugins");
                var icons = ((TreeView)typeof(CrmTreeControl).GetField("trvPlugins", Instance).GetValue(tree)).ImageList;
                int size = CrmTreeControl.TreeIconSize(tree.DeviceDpi);
                Assert.That(icons.ColorDepth, Is.EqualTo(ColorDepth.Depth32Bit), "256 colours made the icons rough");
                Assert.That(icons.ImageSize, Is.EqualTo(new System.Drawing.Size(size, size)));
                Assert.That(icons.Images.Keys, Does.Contain(nameof(CrmTreeNodeImageType.CustomApi)));

                var enable = Field<ImageList>(control, "imlEnableImages");
                Assert.That(enable.ColorDepth, Is.EqualTo(ColorDepth.Depth32Bit));
                Assert.That(enable.ImageSize, Is.EqualTo(Item(control, "toolRegister").Image.Size));
            }

            Assert.That(CrmTreeControl.TreeIconSize(96), Is.EqualTo(20));
            Assert.That(CrmTreeControl.TreeIconSize(144), Is.EqualTo(30), "Display scaling enlarges the icons");
            using (var wide = new System.Drawing.Bitmap(32, 16))
            using (var fitted = CrmTreeControl.FitIcon(wide, 20))
            {
                Assert.That(fitted.Size, Is.EqualTo(new System.Drawing.Size(20, 20)), "Proportions kept, centred in the square");
            }
        }

        [Test]
        public void HideManagedButtonSitsByTheFilterAndShowsInTheCustomApiViewOnly()
        {
            using (var control = new MainControl())
            {
                var toolbar = Field<ToolStrip>(control, "toolBar").Items;
                var button = Field<ToolStripButton>(control, "tsbHideManagedCustomApis");

                Assert.That(toolbar.IndexOf(button), Is.EqualTo(toolbar.IndexOf(Item(control, "tsbFilterAssemblies")) + 1));
                Assert.That(button.Checked, Is.True, "Managed APIs are hidden by default");

                SetView(control, "CustomApi");
                Invoke(control, "UpdateManagedCustomApisButton");
                Assert.That(button.Available, Is.True);

                foreach (var otherView in new[] { "Assembly", "Message", "Entity", "Package" })
                {
                    SetView(control, otherView);
                    Invoke(control, "UpdateManagedCustomApisButton");
                    Assert.That(button.Available, Is.False, otherView);
                }
            }
        }

        [Test]
        public void ManagedApisAreListedOnlyWhenAskedFor()
        {
            using (var control = new MainControl())
            {
                var mine = Summary(managed: false);
                var microsofts = Summary(managed: true);
                ApplyCatalog(control, true, mine, microsofts);
                var create = Main.GetMethod("CreateCustomApiNodes", Instance);

                var hidden = (List<CrmCustomApiNode>)create.Invoke(control, new object[] { false });
                Assert.That(hidden.Select(api => api.Id), Is.EqualTo(new[] { mine.Id }));
                Assert.That(Field<Dictionary<Guid, CrmCustomApiNode>>(control, "customApiMessageNodes").Keys,
                    Is.EqualTo(new[] { mine.SdkMessageId.Value }), "Steps on a hidden API's message are not placed in the view");

                var all = (List<CrmCustomApiNode>)create.Invoke(control, new object[] { true });
                Assert.That(all.Select(api => api.Id), Is.EquivalentTo(new[] { mine.Id, microsofts.Id }));
            }
        }

        [Test]
        public void StepOnAnotherMessageIsLeftOutOfTheCustomApiView()
        {
            using (var control = new MainControl())
            {
                var tree = Field<CrmTreeControl>(control, "trvPlugins");
                var api = Api(managed: false);
                var parents = ShowCustomApiView(control, api);
                var step = new CrmPluginStep(null) { StepId = Guid.NewGuid(), MessageId = Guid.NewGuid(), Name = "Step on Update" };

                control.AddStep(step);

                Assert.That(tree.HasNode(step.StepId), Is.False);
                Assert.That(parents, Is.Empty);
                Assert.That(tree.CountRootNodes, Is.EqualTo(1));
                Assert.That(api.NodeChildren, Is.Empty);
            }
        }

        private static CrmCustomApiNode Api(bool managed) => new CrmCustomApiNode(Summary(managed));

        private static CustomApiSummary Summary(bool managed) => new CustomApiSummary
        {
            Id = Guid.NewGuid(), SdkMessageId = Guid.NewGuid(), Name = "Sample API",
            UniqueName = "sample_DoWork", IsManaged = managed
        };

        private static CrmPluginStep Step(CrmCustomApiNode api) => new CrmPluginStep(null)
        {
            StepId = Guid.NewGuid(), MessageId = api.SdkMessageId.Value, Name = "Step on " + api.UniqueName
        };

        /// <summary>A connected control whose organization answers through a service that records every call.</summary>
        private static RecordingOrganizationService Connect(MainControl control)
        {
            var service = new RecordingOrganizationService();
            var org = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            typeof(CrmOrganization).GetField("m_organizationService", Instance).SetValue(org, service);
            Main.GetField("m_org", Instance).SetValue(control, org);
            Main.GetField("connectionReady", Instance).SetValue(control, true);
            Invoke(control, "SetRegistrationControlsEnabled", true);
            Assert.That(Main.GetProperty("CanRunRegistrationCommand", Instance).GetValue(control), Is.True);
            return service;
        }

        /// <summary>
        /// Puts the control in the state LoadNodes leaves behind for Display by Custom API, without the
        /// organization that LoadNodes reads. Returns the step parent list for assertions.
        /// </summary>
        private static Dictionary<Guid, Guid> ShowCustomApiView(MainControl control, CrmCustomApiNode api)
        {
            SetView(control, "CustomApi");
            var parents = new Dictionary<Guid, Guid>();
            Main.GetField("m_stepParentList", Instance).SetValue(control, parents);
            Field<Dictionary<Guid, CrmCustomApiNode>>(control, "customApiMessageNodes")[api.SdkMessageId.Value] = api;
            Field<CrmTreeControl>(control, "trvPlugins").LoadNodes(new List<ICrmTreeNode> { api });
            return parents;
        }

        /// <summary>
        /// Selects the node as a click does. A tree that is not on screen does not raise its
        /// selection event, so the handler that event calls is invoked directly.
        /// </summary>
        private static void Select(MainControl control, ICrmTreeNode node)
        {
            var tree = Field<CrmTreeControl>(control, "trvPlugins");
            tree.SelectedNode = node;
            Assert.That(tree.SelectedNode, Is.SameAs(node));
            Invoke(control, "SelectItem", node);
        }

        private static void SetView(MainControl control, string view)
        {
            var viewType = Main.GetNestedType("CrmViewType", BindingFlags.NonPublic);
            Main.GetField("m_currentView", Instance).SetValue(control, Enum.Parse(viewType, view));
        }

        private static void ApplyCatalog(MainControl control, bool supported, params CustomApiSummary[] apis)
        {
            var catalogType = Main.GetNestedType("CustomApiCatalog", BindingFlags.NonPublic);
            var catalog = Activator.CreateInstance(catalogType, true);
            catalogType.GetProperty("Supported").SetValue(catalog, supported);
            catalogType.GetProperty("Definitions").SetValue(catalog, apis);
            Invoke(control, "ApplyCustomApiCatalog", catalog);
        }

        private static List<DataRow> GridRows(MainControl control)
        {
            var manager = Field<DataGridView>(control, "grvData").DataSource as DataViewManager;
            Assert.That(manager, Is.Not.Null, "The step grid should be bound");
            return manager.DataSet.Tables[0].Rows.Cast<DataRow>().ToList();
        }

        private static ToolStripItem Item(MainControl control, string name) => Field<ToolStripItem>(control, name);

        private static T Field<T>(MainControl control, string name)
        {
            var field = Main.GetField(name, Instance);
            Assert.That(field, Is.Not.Null, "Field " + name);
            return (T)field.GetValue(control);
        }

        private static void Invoke(MainControl control, string name, params object[] arguments)
        {
            var method = Main.GetMethod(name, Instance);
            Assert.That(method, Is.Not.Null, "Method " + name);
            try
            {
                method.Invoke(control, arguments);
            }
            catch (TargetInvocationException error)
            {
                // Report the handler's own failure instead of the reflection wrapper.
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            }
        }
    }
}

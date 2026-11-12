using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    [TestFixture]
    public class CustomApiTreeTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void KnownSupportSkipsSchemaReadsButStillLoadsSupportedCatalogs(bool supported)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var service = new RecordingOrganizationService { OnRetrieveMultiple = _ => new Microsoft.Xrm.Sdk.EntityCollection() };
            var org = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            typeof(CrmOrganization).GetField("m_organizationService", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(org, service);
            var catalog = typeof(MainControl).GetMethod("ReadCustomApiCatalog", flags)
                .Invoke(null, new object[] { org, new Action<string>(_ => { }), supported });
            Assert.That(catalog.GetType().GetProperty("Error").GetValue(catalog), Is.Null);
            Assert.That(catalog.GetType().GetProperty("Supported").GetValue(catalog), Is.EqualTo(supported));
            Assert.That(service.Requests, Is.Empty);
            Assert.That(service.Queries.Count, Is.EqualTo(supported ? 1 : 0));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void CatalogFailureAndReconnectInvalidateSupportCache()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var control = new MainControl())
            {
                var type = typeof(MainControl);
                var catalogType = type.GetNestedType("CustomApiCatalog", BindingFlags.NonPublic);
                var catalog = Activator.CreateInstance(catalogType, true);
                catalogType.GetProperty("Supported").SetValue(catalog, true);
                type.GetMethod("ApplyCustomApiCatalog", flags).Invoke(control, new[] { catalog });
                Assert.That(type.GetField("customApiSupport", flags).GetValue(control), Is.True);

                // A previously reported error must not display another modal on refresh.
                type.GetField("customApiErrorGeneration", flags).SetValue(control, 0);
                catalogType.GetProperty("Error").SetValue(catalog, new TimeoutException());
                type.GetMethod("ApplyCustomApiCatalog", flags).Invoke(control, new[] { catalog });
                Assert.That(type.GetField("customApiSupport", flags).GetValue(control), Is.Null);

                catalogType.GetProperty("Error").SetValue(catalog, null);
                type.GetMethod("ApplyCustomApiCatalog", flags).Invoke(control, new[] { catalog });
                type.GetMethod("ResetConnectionState", flags).Invoke(control, null);
                Assert.That(type.GetField("customApiSupport", flags).GetValue(control), Is.Null);
            }
        }

        [Test]
        public void ApiWithoutGeneratedMessageOrStepsStillHasItsOwnIdentity()
        {
            var summary = new CustomApiSummary { Id = Guid.NewGuid(), UniqueName = "test_PrivateApi" };
            var node = new CrmCustomApiNode(summary);

            Assert.That(node.NodeId, Is.EqualTo(summary.Id));
            Assert.That(node.NodeType, Is.EqualTo(CrmTreeNodeType.CustomApi));
            Assert.That(node.NodeText, Does.Contain(summary.UniqueName));
            Assert.That(node.NodeChildren, Is.Empty);
            Assert.Throws<ArgumentException>(() => node.AddStep(new CrmPluginStep(null) { MessageId = Guid.NewGuid() }));
        }

        [Test]
        public void StepsUseGeneratedMessageIdentityAndLastRemovalPreservesDefinition()
        {
            var summary = new CustomApiSummary { Id = Guid.NewGuid(), SdkMessageId = Guid.NewGuid(), UniqueName = "test_Api" };
            var node = new CrmCustomApiNode(summary);
            var step = new CrmPluginStep(null) { StepId = Guid.NewGuid(), MessageId = summary.SdkMessageId.Value };
            node.AddStep(step);
            node.AddStep(step);
            Assert.That(node.NodeChildren.Single(), Is.SameAs(step));
            Assert.That(node.NodeId, Is.Not.EqualTo(step.MessageId));
            Assert.Throws<ArgumentException>(() => node.AddStep(new CrmPluginStep(null) { MessageId = Guid.NewGuid() }));

            node.RemoveStep(step.StepId);
            Assert.That(node.NodeChildren, Is.Empty);
            Assert.That(node.NodeId, Is.EqualTo(summary.Id));
        }

        [Test]
        public void PropertyGridCannotEditSummaryAsACompleteDefinition()
        {
            var node = new CrmCustomApiNode(new CustomApiSummary());
            var displayed = TypeDescriptor.GetProperties(node).Cast<PropertyDescriptor>().Where(property => property.IsBrowsable);
            Assert.That(displayed, Is.Not.Empty);
            Assert.That(displayed.All(property => property.IsReadOnly), Is.True);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void RebuildingAndSwitchingViewsDoesNotDuplicateApiRootsOrSteps()
        {
            var summary = new CustomApiSummary { Id = Guid.NewGuid(), SdkMessageId = Guid.NewGuid(), UniqueName = "test_Api" };
            var step = new CrmPluginStep(null) { StepId = Guid.NewGuid(), MessageId = summary.SdkMessageId.Value };
            using (var tree = new CrmTreeControl())
            {
                for (int i = 0; i < 3; i++)
                {
                    var api = new CrmCustomApiNode(summary);
                    api.AddStep(step);
                    tree.LoadNodes(new List<ICrmTreeNode> { api });
                    Assert.That(tree.CountRootNodes, Is.EqualTo(1));
                    Assert.That(tree.HasNode(api.Id), Is.True);
                    Assert.That(tree.HasNode(step.StepId), Is.True);
                    Assert.That(tree[api.Id].NodeChildren, Has.Count.EqualTo(1));
                    tree.LoadNodes(new List<ICrmTreeNode>());
                    Assert.That(tree.HasNode(api.Id), Is.False);
                    Assert.That(tree.HasNode(step.StepId), Is.False);
                }
            }
        }

        [Test, Apartment(ApartmentState.STA)]
        public void RemovingLastStepKeepsApiInTreeAndOnRefresh()
        {
            var api = new CrmCustomApiNode(new CustomApiSummary { Id = Guid.NewGuid(), SdkMessageId = Guid.NewGuid() });
            var step = new CrmPluginStep(null) { StepId = Guid.NewGuid(), MessageId = api.SdkMessageId.Value };
            api.AddStep(step);
            using (var tree = new CrmTreeControl())
            {
                tree.NodeRemoved += (sender, args) => api.RemoveStep(args.Node.NodeId);
                tree.LoadNodes(new List<ICrmTreeNode> { api });
                tree.RemoveNode(step.StepId);
                tree.RefreshNode(api.Id, true);
                Assert.That(tree.HasNode(api.Id), Is.True);
                Assert.That(tree.HasNode(step.StepId), Is.False);
                Assert.That(tree.CountRootNodes, Is.EqualTo(1));
                Assert.That(tree[api.Id].NodeChildren, Is.Empty);
            }
        }
    }
}

using NUnit.Framework;
using System;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.Assemblies
{
    /// <summary>
    /// Issue #16: an assembly update must carry its custom workflow activities' types, or Dataverse keeps
    /// their old arguments. The integration test WorkflowActivityArgumentTests shows it against a real
    /// environment; these cover which types go and when their group is renamed.
    /// </summary>
    [TestFixture]
    public class WorkflowActivityUpdateTests
    {
        private const string OldGroup = "PrtIt.Activities (1.0.0.0)";
        private const string NewGroup = "PrtIt.Activities (1.1.0.0)";

        [Test]
        public void EveryActivityGoesWithTheUpdateButNoPlugin()
        {
            var plugin = Plugin(CrmPluginType.Plugin, null);
            var sameVersion = Plugin(CrmPluginType.WorkflowActivity, OldGroup);
            var ownGroup = Plugin(CrmPluginType.WorkflowActivity, "My activities");

            var types = RegistrationHelper.WorkflowActivityTypesForUpdate(new[] { plugin, sameVersion, ownGroup }, OldGroup, OldGroup);

            Assert.That(types.Select(t => t.Id), Is.EqualTo(new[] { sameVersion.PluginId, ownGroup.PluginId }),
                "Activities go even when nothing about them changes, so their arguments are read again");
            Assert.That(types.Single(t => t.Id == ownGroup.PluginId).WorkflowActivityGroupName, Is.Null,
                "A group the user chose isn't touched");
        }

        [Test]
        public void ANewVersionMovesActivitiesInTheDefaultGroupOnly()
        {
            var defaultGroup = Plugin(CrmPluginType.WorkflowActivity, OldGroup);
            var ownGroup = Plugin(CrmPluginType.WorkflowActivity, "My activities");

            var types = RegistrationHelper.WorkflowActivityTypesForUpdate(new[] { defaultGroup, ownGroup }, OldGroup, NewGroup);

            Assert.That(types.Single(t => t.Id == defaultGroup.PluginId).WorkflowActivityGroupName, Is.EqualTo(NewGroup));
            Assert.That(types.Single(t => t.Id == ownGroup.PluginId).WorkflowActivityGroupName, Is.Null);
            Assert.That(RegistrationHelper.WorkflowActivityTypesForUpdate(null, OldGroup, NewGroup), Is.Empty);
        }

        private static CrmPlugin Plugin(CrmPluginType type, string group) => new CrmPlugin(null)
        {
            PluginId = Guid.NewGuid(),
            PluginType = type,
            WorkflowActivityGroupName = group
        };
    }
}

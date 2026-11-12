using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using Xrm.Sdk.PluginRegistration.Entities;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Wrappers;
using XrmToolBox.Extensibility.Args;

namespace Xrm.Sdk.PluginRegistration.Tests.Unregister
{
    /// <summary>
    /// Covers unregister in its three parts: listing the records, deleting them (the part that
    /// runs in the background) and removing them from the cached registrations afterwards.
    /// </summary>
    [TestFixture]
    public class UnregisterTests
    {
        private static readonly Guid AssemblyId = Guid.NewGuid();
        private static readonly Guid[] PluginIds = { Guid.NewGuid(), Guid.NewGuid() };
        private static readonly Guid StepId = Guid.NewGuid();
        private static readonly Guid ProfilerStepId = Guid.NewGuid();
        private static readonly Guid SecureConfigId = Guid.NewGuid();
        private static readonly Guid ImageId = Guid.NewGuid();

        [Test]
        public void PlanListsEverythingBelowAnAssemblyIncludingProfilerSteps()
        {
            var service = AssemblyService();
            var org = NewOrganization(service);
            Cache<CrmPluginStep>(org, "m_stepList")[StepId] = new CrmPluginStep(org) { StepId = StepId, ProfilerStepId = ProfilerStepId };

            var plan = RegistrationHelper.PlanUnregister(org, new TreeRecord(PluginAssembly.EntityLogicalName, AssemblyId));

            Assert.That(plan.Assemblies, Is.EqualTo(new[] { AssemblyId }));
            Assert.That(plan.Plugins, Is.EqualTo(PluginIds));
            Assert.That(plan.Steps, Is.EqualTo(new[] { StepId, ProfilerStepId }));
            Assert.That(plan.SecureConfigs, Is.EqualTo(new[] { SecureConfigId }));
            Assert.That(plan.Images, Is.EqualTo(new[] { ImageId }));
            Assert.That(plan.Total, Is.EqualTo(7));
            Assert.That(service.DeletedRecords, Is.Empty, "Planning must not delete anything");
        }

        [Test]
        public void PlanRefusesAnAssemblyThatIsPartOfAPackage()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    Assert.That(query.EntityName, Is.EqualTo(PluginAssembly.EntityLogicalName));
                    Assert.That(query.Criteria.Conditions.Select(c => c.AttributeName + " " + c.Operator),
                        Is.EqualTo(new[] { "pluginassemblyid In", "packageid NotNull" }));
                    return new EntityCollection(new List<Entity>
                    {
                        new Entity(PluginAssembly.EntityLogicalName)
                        {
                            ["name"] = "PrtTest.ManyPluginsPackage",
                            ["packageid"] = new EntityReference(PluginPackage.EntityLogicalName, Guid.NewGuid()) { Name = "prt_PrtTest.ManyPluginsPackage" },
                        },
                    });
                },
            };
            var org = NewOrganization(service);

            Assert.That(() => RegistrationHelper.PlanUnregister(org, new TreeRecord(PluginAssembly.EntityLogicalName, AssemblyId)),
                Throws.TypeOf<UnregisterRefusedException>().With.Message.StartsWith(
                    "The assembly PrtTest.ManyPluginsPackage is part of the plug-in package prt_PrtTest.ManyPluginsPackage"));
            Assert.That(service.Queries, Has.Count.EqualTo(1), "Nothing else is read");
            Assert.That(service.DeletedRecords, Is.Empty);
        }

        [Test]
        public void PlanRefusesAPluginWhoseAssemblyIsPartOfAPackage()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    Assert.That(query.EntityName, Is.EqualTo(PluginType.EntityLogicalName));
                    Assert.That(query.LinkEntities.Single().LinkCriteria.Conditions.Single().AttributeName, Is.EqualTo("packageid"));
                    var package = new EntityReference(PluginPackage.EntityLogicalName, Guid.NewGuid()) { Name = "prt_Contoso" };
                    return new EntityCollection(new List<Entity>
                    {
                        new Entity(PluginType.EntityLogicalName)
                        {
                            ["typename"] = "Contoso.Plugins.Plugin001",
                            ["assembly.packageid"] = new AliasedValue(PluginAssembly.EntityLogicalName, "packageid", package),
                        },
                    });
                },
            };
            var org = NewOrganization(service);

            Assert.That(() => RegistrationHelper.PlanUnregister(org, new TreeRecord(PluginType.EntityLogicalName, PluginIds[0])),
                Throws.TypeOf<UnregisterRefusedException>().With.Message.StartsWith(
                    "The plug-in Contoso.Plugins.Plugin001 is part of the plug-in package prt_Contoso"));
        }

        [Test]
        public void ExecuteDeletesChildrenFirstAndReportsEachDelete()
        {
            var service = new RecordingOrganizationService { OnDelete = (entity, id) => { } };
            var org = NewOrganization(service);
            var plan = FullPlan();
            var reports = new List<UnregisterProgress>();

            var result = RegistrationHelper.ExecuteUnregister(org, plan, reports.Add);

            Assert.That(result.Error, Is.Null);
            Assert.That(service.DeletedRecords.Select(r => r.LogicalName).Distinct(), Is.EqualTo(new[]
            {
                SdkMessageProcessingStepImage.EntityLogicalName, SdkMessageProcessingStep.EntityLogicalName,
                SdkMessageProcessingStepSecureConfig.EntityLogicalName, PluginType.EntityLogicalName, PluginAssembly.EntityLogicalName,
            }));
            Assert.That(result.Deleted.Total, Is.EqualTo(plan.Total));
            Assert.That(reports.Select(r => r.ToString()), Is.EqualTo(new[]
            {
                "Unregistering images (0 of 7)", "Unregistering images (1 of 7)",
                "Unregistering steps (1 of 7)", "Unregistering steps (2 of 7)", "Unregistering steps (3 of 7)",
                "Unregistering secure configurations (3 of 7)", "Unregistering secure configurations (4 of 7)",
                "Unregistering plug-ins (4 of 7)", "Unregistering plug-ins (5 of 7)", "Unregistering plug-ins (6 of 7)",
                "Unregistering assemblies (6 of 7)", "Unregistering assemblies (7 of 7)",
            }));
            Assert.That(reports.Last().Percent, Is.EqualTo(100));
            Assert.That(result.Deleted.Summary(), Is.EqualTo(string.Join(Environment.NewLine,
                "1 Assembly Unregistered Successfully", "2 Plugins Unregistered Successfully",
                "2 Steps Unregistered Successfully", "1 Image Unregistered Successfully")));
        }

        [Test]
        public void ExecuteStopsAtTheFirstFailureAndKeepsWhatWasDeleted()
        {
            var failure = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault { Message = "Plug-in type is in use" });
            var service = new RecordingOrganizationService
            {
                OnDelete = (entity, id) => { if (id == PluginIds[1]) throw failure; }
            };
            var org = NewOrganization(service);

            var result = RegistrationHelper.ExecuteUnregister(org, FullPlan(), null);

            Assert.That(result.Error, Is.SameAs(failure));
            Assert.That(result.Deleted.Images, Is.EqualTo(new[] { ImageId }));
            Assert.That(result.Deleted.Steps, Is.EqualTo(new[] { StepId, ProfilerStepId }));
            Assert.That(result.Deleted.Plugins, Is.EqualTo(new[] { PluginIds[0] }));
            Assert.That(result.Deleted.Assemblies, Is.Empty);
            Assert.That(service.DeletedRecords.Last().Id, Is.EqualTo(PluginIds[1]), "Nothing is deleted after the failure");
        }

        [Test]
        public void ApplyRemovesOnlyDeletedRecordsFromTheCache()
        {
            var org = NewOrganization(new RecordingOrganizationService());
            var plugins = Cache<CrmPlugin>(org, "m_pluginList");
            var steps = Cache<CrmPluginStep>(org, "m_stepList");
            var images = Cache<CrmPluginImage>(org, "m_imageList");
            foreach (var id in PluginIds) plugins[id] = Uninitialized<CrmPlugin>();
            steps[StepId] = Uninitialized<CrmPluginStep>();
            images[ImageId] = Uninitialized<CrmPluginImage>();

            var deleted = new UnregisterPlan();
            deleted.Images.Add(ImageId);
            deleted.Steps.Add(StepId);
            deleted.Plugins.Add(PluginIds[0]);
            RegistrationHelper.ApplyUnregister(org, deleted);

            Assert.That(images.Keys, Is.Empty);
            Assert.That(steps.Keys, Is.Empty);
            Assert.That(plugins.Keys, Is.EqualTo(new[] { PluginIds[1] }));
        }

        [Test]
        public void ApplyClearsMultipleVersionsWhenOneVersionIsLeft()
        {
            var org = NewOrganization(new RecordingOrganizationService());
            var assemblies = Cache<CrmPluginAssembly>(org, "m_assemblyList");
            var oldVersion = new CrmPluginAssembly(org) { AssemblyId = AssemblyId, Name = "Contoso.Plugins", MultipleVersions = true };
            var newVersion = new CrmPluginAssembly(org) { AssemblyId = Guid.NewGuid(), Name = "Contoso.Plugins", MultipleVersions = true };
            assemblies[oldVersion.AssemblyId] = oldVersion;
            assemblies[newVersion.AssemblyId] = newVersion;

            var deleted = new UnregisterPlan();
            deleted.Assemblies.Add(AssemblyId);
            deleted.Assemblies.Add(Guid.NewGuid()); // not cached: ignored
            RegistrationHelper.ApplyUnregister(org, deleted);

            Assert.That(assemblies.Keys, Is.EqualTo(new[] { newVersion.AssemblyId }));
            Assert.That(newVersion.MultipleVersions, Is.False);
        }

        [Test, Apartment(ApartmentState.STA)]
        public void SynchronousUnregisterReportsToTheStatusBarAndRethrowsAFailure()
        {
            var sent = new List<StatusBarMessageEventArgs>();
            var progress = new ProgressIndicator(new Action<StatusBarMessageEventArgs>(sent.Add));
            var stepId = Guid.NewGuid();
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => new EntityCollection(),
                OnDelete = (entity, id) => { },
            };
            var org = NewOrganization(service);

            var stats = RegistrationHelper.Unregister(org, progress, new TreeRecord(SdkMessageProcessingStep.EntityLogicalName, stepId));

            Assert.That(stats, Is.EqualTo(new Dictionary<string, int> { ["Step"] = 1 }));
            Assert.That(sent.Where(m => m.Progress.HasValue).Select(m => m.Progress.Value + "|" + m.Message),
                Is.EqualTo(new[] { "0|", "0|Unregistering steps", "100|Unregistering steps" }));
            Assert.That(sent.Last().Message, Is.Empty, "The status bar is cleared at the end");

            service.OnDelete = (entity, id) => throw new InvalidOperationException("boom");
            Assert.That(() => RegistrationHelper.Unregister(org, progress, new TreeRecord(SdkMessageProcessingStep.EntityLogicalName, stepId)),
                Throws.InvalidOperationException.With.Message.EqualTo("boom"));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void WithNothingSelectedTheTreeTakesTheGridSpace()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            using (var control = new MainControl())
            {
                var type = typeof(MainControl);
                var grid = (System.Windows.Forms.Control)type.GetField("grpGrid", flags).GetValue(control);
                var splitter = (System.Windows.Forms.Control)type.GetField("splitterDisplay", flags).GetValue(control);
                // As after selecting an assembly: the grid shows below a shorter tree
                grid.Visible = true;
                splitter.Height = grid.Top - splitter.Top - 10;

                // After unregistering, nothing is selected
                type.GetMethod("SelectItem", flags).Invoke(control, new object[] { null });

                Assert.That(grid.Visible, Is.False);
                Assert.That(splitter.Bottom, Is.EqualTo(grid.Bottom));
            }
        }

        /// <summary>An assembly with two plug-ins; the first has a step with a secure configuration and an image.</summary>
        private static RecordingOrganizationService AssemblyService()
        {
            return new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    var filter = query.Criteria.Conditions.First().AttributeName;
                    switch (query.EntityName + "/" + filter)
                    {
                        case "pluginassembly/pluginassemblyid":
                            return new EntityCollection(); // not part of a package
                        case "plugintype/pluginassemblyid":
                            return Rows(query, PluginIds.Select(id => (object)id));
                        case "sdkmessageprocessingstep/plugintypeid":
                            return Rows(query, new object[] { StepId });
                        case "sdkmessageprocessingstep/eventhandler":
                            return new EntityCollection();
                        case "sdkmessageprocessingstep/sdkmessageprocessingstepid":
                            return Rows(query, new object[] { new EntityReference(SdkMessageProcessingStepSecureConfig.EntityLogicalName, SecureConfigId) });
                        case "sdkmessageprocessingstepimage/sdkmessageprocessingstepid":
                            return Rows(query, new object[] { ImageId });
                        default:
                            throw new InvalidOperationException("Unexpected query: " + query.EntityName + " by " + filter);
                    }
                },
            };
        }

        private static EntityCollection Rows(QueryExpression query, IEnumerable<object> values)
        {
            var column = query.ColumnSet.Columns.Single();
            return new EntityCollection(values.Select(value => new Entity(query.EntityName) { [column] = value }).ToList());
        }

        private static UnregisterPlan FullPlan()
        {
            var plan = new UnregisterPlan();
            plan.Images.Add(ImageId);
            plan.Steps.Add(StepId);
            plan.Steps.Add(ProfilerStepId);
            plan.SecureConfigs.Add(SecureConfigId);
            plan.Plugins.AddRange(PluginIds);
            plan.Assemblies.Add(AssemblyId);
            return plan;
        }

        private static CrmOrganization NewOrganization(IOrganizationService service)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var org = Uninitialized<CrmOrganization>();
            typeof(CrmOrganization).GetField("m_organizationService", flags).SetValue(org, service);
            typeof(CrmOrganization).GetField("m_assemblyList", flags).SetValue(org, new Dictionary<Guid, CrmPluginAssembly>());
            typeof(CrmOrganization).GetField("m_pluginList", flags).SetValue(org, new Dictionary<Guid, CrmPlugin>());
            typeof(CrmOrganization).GetField("m_stepList", flags).SetValue(org, new Dictionary<Guid, CrmPluginStep>());
            typeof(CrmOrganization).GetField("m_imageList", flags).SetValue(org, new Dictionary<Guid, CrmPluginImage>());
            typeof(CrmOrganization).GetField("m_serviceEndpointList", flags).SetValue(org, new Dictionary<Guid, CrmServiceEndpoint>());
            typeof(CrmOrganization).GetField("m_packageList", flags).SetValue(org, new Dictionary<Guid, CrmPluginPackage>());
            return org;
        }

        private static Dictionary<Guid, T> Cache<T>(CrmOrganization org, string field)
        {
            return (Dictionary<Guid, T>)typeof(CrmOrganization).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(org);
        }

        private static T Uninitialized<T>()
        {
            return (T)FormatterServices.GetUninitializedObject(typeof(T));
        }

        /// <summary>A record as the tree hands it to unregister.</summary>
        private sealed class TreeRecord : ICrmEntity
        {
            public TreeRecord(string type, Guid id)
            {
                EntityType = type;
                EntityId = id;
            }

            public Guid EntityId { get; }
            public string EntityType { get; }
            public bool IsSystemCrmEntity => false;
            public Dictionary<string, object> Values => new Dictionary<string, object>();
            public Dictionary<string, object> GenerateCrmEntities() => throw new NotSupportedException();
            public void UpdateDates(DateTime? createdOn, DateTime? modifiedOn) => throw new NotSupportedException();
        }
    }
}

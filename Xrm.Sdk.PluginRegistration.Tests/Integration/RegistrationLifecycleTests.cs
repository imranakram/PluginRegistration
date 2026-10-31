namespace Xrm.Sdk.PluginRegistration.Tests.Integration
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.TestPlugins;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    /// <summary>
    /// Registers the TestPlugins assembly in a real environment through the same helpers the
    /// tool's forms use, checks that the registrations load back into the tree and actually run,
    /// and unregisters everything again. The tests depend on each other, so they run in order.
    /// </summary>
    [TestFixture]
    [Category(TestBench.Category)]
    [NonParallelizable]
    internal class RegistrationLifecycleTests
    {
        private const string AssemblyName = "Xrm.Sdk.PluginRegistration.TestPlugins";
        private const string ContactPrefix = "PRT-IT ";
        private const string WebhookName = "PRT-IT webhook";
        private static readonly TimeSpan PropagationTimeout = TimeSpan.FromMinutes(2);

        private readonly List<Guid> createdContacts = new List<Guid>();
        private TestBench bench;
        private CrmOrganization org;

        private Guid assemblyId;
        private Guid createStepId;
        private Guid updateStepId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            bench = TestBench.LoadOrIgnore();
            org = bench.LoadOrganization();
            RemoveLeftovers();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (bench?.Service == null)
            {
                return;
            }

            try
            {
                RemoveLeftovers();
            }
            finally
            {
                foreach (var id in createdContacts)
                {
                    try { bench.Service.Delete("contact", id); } catch (Exception ex) { TestContext.Progress.WriteLine($"Unable to delete contact {id}: {ex.Message}"); }
                }
            }
        }

        [Test, Order(1)]
        public void Connect_LoadsMessagesForThisOrganization()
        {
            Assert.That(org.Connected, Is.True);
            Assert.That(FindMessage("Create").FindMessageEntity("contact", "none"), Is.Not.Null);
            Assert.That(FindMessage("Update").ImageMessagePropertyNames, Is.Not.Empty);
        }

        [Test, Order(2)]
        public void RegisterAssembly_RegistersAssemblyAndAllTypes()
        {
            var path = typeof(StampPlugin).Assembly.Location;

            // As PluginRegistrationForm does for a new assembly
            var assembly = RegistrationHelper.RetrievePluginsFromAssembly(path);
            assembly.SourceType = CrmAssemblySourceType.Database;
            assembly.IsolationMode = CrmAssemblyIsolationMode.Sandbox;
            assembly.ServerFileName = null;
            Assert.That(assembly.Plugins.Values.Select(p => p.TypeName), Is.EquivalentTo(new[]
            {
                typeof(StampPlugin).FullName, typeof(NoOpPlugin).FullName, typeof(EchoApiPlugin).FullName, typeof(NoOpActivity).FullName,
            }));

            assemblyId = RegistrationHelper.RegisterAssembly(org, path, assembly);
            foreach (var plugin in assembly.Plugins.Values.ToList())
            {
                plugin.AssemblyId = assemblyId;
                RegistrationHelper.RegisterPlugin(org, plugin);
            }

            Reload();
            var loaded = org[assemblyId];
            Assert.That(loaded.Name, Is.EqualTo(AssemblyName));
            Assert.That(loaded.IsolationMode, Is.EqualTo(CrmAssemblyIsolationMode.Sandbox));
            Assert.That(loaded.Plugins.Values.ToDictionary(p => p.TypeName, p => p.PluginType), Is.EquivalentTo(new Dictionary<string, CrmPluginType>
            {
                [typeof(StampPlugin).FullName] = CrmPluginType.Plugin,
                [typeof(NoOpPlugin).FullName] = CrmPluginType.Plugin,
                [typeof(EchoApiPlugin).FullName] = CrmPluginType.Plugin,
                [typeof(NoOpActivity).FullName] = CrmPluginType.WorkflowActivity,
            }));
        }

        [Test, Order(3)]
        public void RegisterStep_RunsWithItsConfiguration()
        {
            RequireAssembly();
            var step = NewStep("Create", "contact", "create-config");
            step.SecureConfiguration = "secret";
            createStepId = RegistrationHelper.RegisterStep(org, step);

            Reload();
            var loaded = StampPluginNode()[createStepId];
            Assert.That(loaded.UnsecureConfiguration, Is.EqualTo("create-config"));
            Assert.That(loaded.Stage, Is.EqualTo(CrmPluginStepStage.PreOperation));
            Assert.That(loaded.Enabled, Is.True);

            WaitForCreateStamp(StampPlugin.Stamp("Create", "create-config", true, null));
        }

        [Test, Order(4)]
        public void UpdateStep_ChangesConfiguration()
        {
            RequireStep(createStepId);
            var step = StampPluginNode()[createStepId];
            step.UnsecureConfiguration = "updated-config";

            Assert.That(RegistrationHelper.UpdateStep(org, step, step.SecureConfigurationId, null), Is.True);

            Reload();
            Assert.That(StampPluginNode()[createStepId].UnsecureConfiguration, Is.EqualTo("updated-config"));
            WaitForCreateStamp(StampPlugin.Stamp("Create", "updated-config", true, null));
        }

        [Test, Order(5)]
        public void DisableStep_StopsItRunning_EnableStep_StartsItAgain()
        {
            RequireStep(createStepId);

            RegistrationHelper.UpdateStepStatus(org, createStepId, false);
            Reload();
            Assert.That(StampPluginNode()[createStepId].Enabled, Is.False);
            WaitForCreateStamp(null);

            RegistrationHelper.UpdateStepStatus(org, createStepId, true);
            Reload();
            Assert.That(StampPluginNode()[createStepId].Enabled, Is.True);
            WaitForCreateStamp(StampPlugin.Stamp("Create", "updated-config", true, null));
        }

        [Test, Order(6)]
        public void RegisterImage_PassesPreImageToPlugin()
        {
            RequireAssembly();
            var step = NewStep("Update", "contact", "update-config");
            step.FilteringAttributes = "lastname";
            updateStepId = RegistrationHelper.RegisterStep(org, step);
            Reload();
            step = StampPluginNode()[updateStepId];

            // As ImageRegistrationForm does
            var image = new CrmPluginImage(org)
            {
                AssemblyId = step.AssemblyId,
                PluginId = step.PluginId,
                StepId = step.StepId,
                Attributes = "lastname",
                EntityAlias = StampPlugin.PreImageAlias,
                Name = StampPlugin.PreImageAlias,
                MessagePropertyName = "Target",
                ImageType = CrmPluginImageType.PreImage,
            };
            var imageId = RegistrationHelper.RegisterImage(org, image, step);

            Reload();
            var loaded = StampPluginNode()[updateStepId].Images[imageId];
            Assert.That(loaded.EntityAlias, Is.EqualTo(StampPlugin.PreImageAlias));
            Assert.That(loaded.Attributes, Is.EqualTo("lastname"));

            var contactId = CreateContact(out var lastName);
            WaitFor("the Update step to stamp the pre image", () =>
            {
                var newName = ContactPrefix + Guid.NewGuid().ToString("N");
                bench.Service.Update(new Entity("contact", contactId) { ["lastname"] = newName });
                var description = ReadDescription(contactId);
                var expected = StampPlugin.Stamp("Update", "update-config", false, lastName);
                lastName = newName;
                return description == expected;
            });
        }

        [Test, Order(7)]
        public void UpdateAssembly_UploadsContent()
        {
            RequireAssembly();
            var assembly = org[assemblyId];
            var modifiedBefore = (DateTime)bench.Service.Retrieve("pluginassembly", assemblyId, new ColumnSet("modifiedon"))["modifiedon"];

            RegistrationHelper.UpdateAssembly(org, typeof(StampPlugin).Assembly.Location, assembly);

            var modifiedAfter = (DateTime)bench.Service.Retrieve("pluginassembly", assemblyId, new ColumnSet("modifiedon"))["modifiedon"];
            Assert.That(modifiedAfter, Is.GreaterThan(modifiedBefore));

            Reload();
            Assert.That(org[assemblyId].Plugins.Count, Is.EqualTo(4), "Updating the content must keep the registered types");
        }

        [Test, Order(8)]
        public void WebHook_RegisterUpdateAddStepAndUnregister()
        {
            // As WebHookRegistrationForm does. The URL is never called: no account is created.
            var webhook = new CrmServiceEndpoint(org)
            {
                Url = "https://example.invalid/prt-integration-test",
                Name = WebhookName,
                AuthType = CrmServiceEndpointAuthType.WebhookKey,
                AuthValue = "test-key",
                ConnectionMode = CrmServiceEndpointConnectionMode.Normal,
                Contract = CrmServiceEndpointContract.Webhook,
                UserClaim = CrmServiceEndpointUserClaim.None,
            };
            var webhookId = RegistrationHelper.RegisterWebHook(org, webhook);

            Reload();
            webhook = org.Webhooks[webhookId];
            Assert.That(webhook.Url, Is.EqualTo("https://example.invalid/prt-integration-test"));

            webhook.Url = "https://example.invalid/prt-integration-test/updated";
            RegistrationHelper.UpdatewebHook(org, webhook);

            // A step on a webhook has no assembly, and the webhook as its handler
            var step = NewStep("Create", "account", null);
            step.AssemblyId = Guid.Empty;
            step.PluginId = webhookId;
            step.Mode = CrmPluginStepMode.Asynchronous;
            step.Stage = CrmPluginStepStage.PostOperation;
            var stepId = RegistrationHelper.RegisterStep(org, step);

            Reload();
            webhook = org.Webhooks[webhookId];
            Assert.That(webhook.Url, Is.EqualTo("https://example.invalid/prt-integration-test/updated"));
            Assert.That(webhook.Steps.ContainsKey(stepId), Is.True);

            RegistrationHelper.Unregister(org, null, webhook);

            Reload();
            Assert.That(org.Webhooks.ContainsKey(webhookId), Is.False);
            Assert.That(Exists("sdkmessageprocessingstep", stepId), Is.False);
        }

        [Test, Order(9)]
        public void Unregister_Assembly_RemovesItsTypesStepsAndImages()
        {
            RequireAssembly();
            var assembly = org[assemblyId];
            var pluginIds = assembly.Plugins.Values.Select(p => p.PluginId).ToList();
            var stepIds = assembly.Plugins.Values.SelectMany(p => p.Steps.Values).Select(s => s.StepId).ToList();
            var imageIds = assembly.Plugins.Values.SelectMany(p => p.Steps.Values).SelectMany(s => s.Images.Values).Select(i => i.ImageId).ToList();
            Assert.That(stepIds, Has.Count.EqualTo(2));
            Assert.That(imageIds, Has.Count.EqualTo(1));

            var stats = RegistrationHelper.Unregister(org, null, assembly);
            TestContext.Progress.WriteLine("Unregistered: " + string.Join(", ", stats.Select(s => $"{s.Key}={s.Value}")));

            Assert.That(Exists("pluginassembly", assemblyId), Is.False);
            Assert.That(pluginIds.Any(id => Exists("plugintype", id)), Is.False);
            Assert.That(stepIds.Any(id => Exists("sdkmessageprocessingstep", id)), Is.False);
            Assert.That(imageIds.Any(id => Exists("sdkmessageprocessingstepimage", id)), Is.False);
            assemblyId = Guid.Empty;

            Reload();
            Assert.That(org.Assemblies.Values.Any(a => a.Name == AssemblyName), Is.False);
        }

        private CrmPluginStep NewStep(string messageName, string entity, string unsecureConfig)
        {
            // As StepRegistrationForm does
            var message = FindMessage(messageName);
            var plugin = StampPluginNode();
            return new CrmPluginStep(org)
            {
                MessageId = message.MessageId,
                MessageEntityId = message.FindMessageEntity(entity, "none").MessageEntityId,
                AssemblyId = plugin.AssemblyId,
                PluginId = plugin.PluginId,
                ImpersonatingUserId = Guid.Empty,
                Name = $"PRT-IT {messageName} of {entity}",
                UnsecureConfiguration = unsecureConfig,
                Rank = 1,
                Mode = CrmPluginStepMode.Synchronous,
                Stage = CrmPluginStepStage.PreOperation,
                Deployment = CrmPluginStepDeployment.ServerOnly,
                InvocationSource = null,
                Enabled = true,
            };
        }

        private CrmMessage FindMessage(string name) => org.Messages.Values.Single(m => m.Name == name);

        private CrmPlugin StampPluginNode() => org[assemblyId].Plugins.Values.Single(p => p.TypeName == typeof(StampPlugin).FullName);

        private void Reload() => org = bench.LoadOrganization();

        private void RequireAssembly()
        {
            if (assemblyId == Guid.Empty) Assert.Inconclusive("The assembly was not registered by an earlier test.");
        }

        private void RequireStep(Guid stepId)
        {
            if (stepId == Guid.Empty) Assert.Inconclusive("The step was not registered by an earlier test.");
        }

        private Guid CreateContact(out string lastName)
        {
            lastName = ContactPrefix + Guid.NewGuid().ToString("N");
            var id = bench.Service.Create(new Entity("contact") { ["lastname"] = lastName });
            createdContacts.Add(id);
            return id;
        }

        private string ReadDescription(Guid contactId) =>
            bench.Service.Retrieve("contact", contactId, new ColumnSet("description")).GetAttributeValue<string>("description");

        /// <summary>
        /// Step changes reach the sandbox after a short delay, so create contacts until one is stamped as expected.
        /// </summary>
        private void WaitForCreateStamp(string expected)
        {
            WaitFor(expected == null ? "the Create step to stop running" : $"a contact stamped '{expected}'",
                () => ReadDescription(CreateContact(out _)) == expected);
        }

        private static void WaitFor(string what, Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            while (!condition())
            {
                if (watch.Elapsed > PropagationTimeout)
                {
                    Assert.Fail($"Timed out after {PropagationTimeout.TotalSeconds:0}s waiting for {what}.");
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            TestContext.Progress.WriteLine($"Waited {watch.Elapsed.TotalSeconds:0}s for {what}.");
        }

        private bool Exists(string entity, Guid id)
        {
            var query = new QueryExpression(entity) { ColumnSet = new ColumnSet(false), TopCount = 1 };
            query.Criteria.AddCondition(entity + "id", ConditionOperator.Equal, id);
            return bench.Service.RetrieveMultiple(query).Entities.Count > 0;
        }

        /// <summary>
        /// Removes what an earlier, interrupted run may have left behind.
        /// </summary>
        private void RemoveLeftovers()
        {
            Reload();
            var leftovers = org.Assemblies.Values.Where(a => a.Name == AssemblyName).Cast<ICrmEntity>()
                .Concat(org.Webhooks.Values.Where(w => w.Name == WebhookName))
                .ToArray();
            if (leftovers.Length > 0)
            {
                TestContext.Progress.WriteLine($"Removing {leftovers.Length} leftover registration(s).");
                RegistrationHelper.Unregister(org, null, leftovers);
                Reload();
            }

            var contacts = new QueryExpression("contact") { ColumnSet = new ColumnSet(false) };
            contacts.Criteria.AddCondition("lastname", ConditionOperator.BeginsWith, ContactPrefix);
            foreach (var contact in bench.Service.RetrieveMultiple(contacts).Entities)
            {
                if (!createdContacts.Contains(contact.Id)) createdContacts.Add(contact.Id);
            }
        }
    }
}

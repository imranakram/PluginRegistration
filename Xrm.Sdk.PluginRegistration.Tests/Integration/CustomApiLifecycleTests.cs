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
    using Xrm.Sdk.PluginRegistration.Models;
    using Xrm.Sdk.PluginRegistration.TestPlugins;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    /// <summary>
    /// Creates a Custom API in a real environment through the service the Custom API editor uses,
    /// with a plug-in type from the TestPlugins assembly as its handler. Checks that it loads back the
    /// way the tool loads it after connecting or refreshing, that it runs, that a step on its message
    /// runs, and that edits, solution membership and deletion take effect. The tests depend on each
    /// other, so they run in order.
    /// </summary>
    [TestFixture]
    [Category(TestBench.Category)]
    [NonParallelizable]
    internal class CustomApiLifecycleTests
    {
        private const string AssemblyName = "Xrm.Sdk.PluginRegistration.TestPlugins";
        private const string NamePart = "_PrtIt";
        private const string SolutionName = "PrtItCustomApis";
        private const string SecondSolutionName = "PrtItCustomApisCopy";
        private const string SuffixName = "Suffix";
        private const string StepConfig = "step-config";
        private const string Text = "hello";
        private static readonly TimeSpan PropagationTimeout = TimeSpan.FromMinutes(2);

        private TestBench bench;
        private CrmOrganization org;
        private CustomApiService apis;
        private IReadOnlyList<CustomApiSummary> catalog;
        private Entity publisher;
        private string prefix;
        private string echoApiName;

        private Guid assemblyId;
        private Guid echoApiId;
        private Guid echoMessageId;
        private Guid stepId;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            bench = TestBench.LoadOrIgnore();
            org = bench.LoadOrganization();
            apis = new CustomApiService(bench.Service);
            publisher = FindPublisher();
            prefix = publisher.GetAttributeValue<string>("customizationprefix");
            echoApiName = prefix + NamePart + "Echo" + RandomPart();
            RemoveLeftovers();
            CreateSolution(SolutionName);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (bench?.Service == null || prefix == null)
            {
                return;
            }

            RemoveLeftovers();
        }

        [Test, Order(1)]
        public void Connect_FindsCustomApiSupport()
        {
            Assert.That(apis.IsSupported(), Is.True);
        }

        [Test, Order(2)]
        public void RegisterAssembly_IncludesTheApiHandler()
        {
            var path = typeof(EchoApiPlugin).Assembly.Location;

            // As PluginRegistrationForm does for a new assembly
            var assembly = RegistrationHelper.RetrievePluginsFromAssembly(path);
            assembly.SourceType = CrmAssemblySourceType.Database;
            assembly.IsolationMode = CrmAssemblyIsolationMode.Sandbox;
            assembly.ServerFileName = null;
            assemblyId = RegistrationHelper.RegisterAssembly(org, path, assembly);
            foreach (var plugin in assembly.Plugins.Values.ToList())
            {
                plugin.AssemblyId = assemblyId;
                RegistrationHelper.RegisterPlugin(org, plugin);
            }

            Reload();
            Assert.That(EchoPluginNode().PluginType, Is.EqualTo(CrmPluginType.Plugin));
        }

        [Test, Order(3)]
        public void Create_SavesTheApiWithItsParametersInTheSolution()
        {
            RequireAssembly();
            var draft = Definition(echoApiName);
            draft.AllowedCustomProcessingStepType = CustomApiProcessingStepType.SyncAndAsync;
            draft.PluginTypeId = EchoPluginNode().PluginId;
            draft.RequestParameters.Add(Parameter<CustomApiRequestParameter>(EchoApiPlugin.InputName));
            draft.ResponseProperties.Add(Parameter<CustomApiResponseProperty>(EchoApiPlugin.OutputName));

            echoApiId = apis.Create(draft, SolutionName);

            var saved = apis.Retrieve(echoApiId);
            Assert.That(saved.UniqueName, Is.EqualTo(echoApiName));
            Assert.That(saved.PluginTypeId, Is.EqualTo(draft.PluginTypeId));
            Assert.That(saved.AllowedCustomProcessingStepType, Is.EqualTo(CustomApiProcessingStepType.SyncAndAsync));
            Assert.That(saved.SdkMessageId, Is.Not.Null);
            Assert.That(saved.RequestParameters.Select(p => p.UniqueName), Is.EqualTo(new[] { EchoApiPlugin.InputName }));
            Assert.That(saved.ResponseProperties.Select(p => p.UniqueName), Is.EqualTo(new[] { EchoApiPlugin.OutputName }));
            Assert.That(SolutionComponents(SolutionName), Does.Contain(echoApiId));

            // The editor validates an edit against what it loaded, so a loaded API must pass unchanged
            Assert.That(CustomApiValidator.Validate(apis.Retrieve(echoApiId), saved), Is.Empty);
            echoMessageId = saved.SdkMessageId.Value;
        }

        [Test, Order(4)]
        public void Reload_ListsTheApiAndLoadsItsMessage()
        {
            RequireApi();

            Reload();

            var summary = catalog.Single(api => api.Id == echoApiId);
            Assert.That(summary.UniqueName, Is.EqualTo(echoApiName));
            Assert.That(summary.SdkMessageId, Is.EqualTo(echoMessageId));
            Assert.That(org.Messages.ContainsKey(echoMessageId), Is.True, "The step editor needs the API's message");
            Assert.That(org.Messages[echoMessageId].Name, Is.EqualTo(echoApiName));
            Assert.That(org.Messages[echoMessageId].MessageEntities.Values, Is.Not.Empty, "An API that allows steps has a message filter to register them on");
        }

        [Test, Order(5)]
        public void Execute_RunsTheHandler()
        {
            RequireApi();
            WaitForEcho(EchoApiPlugin.Echo(Text));
        }

        [Test, Order(6)]
        public void RegisterStep_OnTheApiMessage_RunsAfterTheHandler()
        {
            RequireApi();

            // As StepRegistrationForm does for a message without a table
            var plugin = EchoPluginNode();
            var step = new CrmPluginStep(org)
            {
                MessageId = echoMessageId,
                MessageEntityId = Guid.Empty,
                AssemblyId = plugin.AssemblyId,
                PluginId = plugin.PluginId,
                ImpersonatingUserId = Guid.Empty,
                Name = "PRT-IT step on " + echoApiName,
                UnsecureConfiguration = StepConfig,
                Rank = 1,
                Mode = CrmPluginStepMode.Synchronous,
                Stage = CrmPluginStepStage.PostOperation,
                Deployment = CrmPluginStepDeployment.ServerOnly,
                InvocationSource = null,
                Enabled = true,
            };
            stepId = RegistrationHelper.RegisterStep(org, step);

            Reload();
            var loaded = EchoPluginNode()[stepId];
            Assert.That(loaded.MessageId, Is.EqualTo(echoMessageId));
            Assert.That(loaded.Stage, Is.EqualTo(CrmPluginStepStage.PostOperation));
            Assert.That(org.Messages.ContainsKey(loaded.MessageId), Is.True, "Editing the step needs its message to be loaded");

            WaitForEcho(ExpectedEcho());
        }

        [Test, Order(7)]
        public void Update_ChangesOnlyWhatWasEdited()
        {
            RequireApi();
            var original = apis.Retrieve(echoApiId);
            var changed = apis.Retrieve(echoApiId);
            changed.Description = "Updated by the integration tests.";
            changed.RequestParameters.Single().DisplayName = "Updated text";
            var suffix = Parameter<CustomApiRequestParameter>(SuffixName);
            suffix.IsOptional = true;
            changed.RequestParameters.Add(suffix);

            Assert.That(apis.Update(original, changed), Is.True);

            var saved = apis.Retrieve(echoApiId);
            var text = saved.RequestParameters.Single(p => p.UniqueName == EchoApiPlugin.InputName);
            Assert.That(saved.Description, Is.EqualTo(changed.Description));
            Assert.That(text.Id, Is.EqualTo(original.RequestParameters.Single().Id), "Editing a parameter must update it in place");
            Assert.That(text.DisplayName, Is.EqualTo("Updated text"));
            Assert.That(saved.RequestParameters.Single(p => p.UniqueName == SuffixName).IsOptional, Is.True);
            Assert.That(saved.ResponseProperties.Single().Id, Is.EqualTo(original.ResponseProperties.Single().Id));
            Assert.That(apis.Update(saved, apis.Retrieve(echoApiId)), Is.False, "An unchanged definition must not send an update");

            // The handler ignores the new parameter; the server accepting it shows the new signature is live
            WaitForEcho(ExpectedEcho(), suffix: "ignored");
        }

        [Test, Order(8)]
        public void Update_FromAStaleSnapshot_RollsBackEveryChange()
        {
            RequireApi();
            var original = apis.Retrieve(echoApiId);
            var suffix = original.RequestParameters.SingleOrDefault(p => p.UniqueName == SuffixName);
            if (suffix == null) Assert.Inconclusive("The Suffix parameter was not added by an earlier test.");
            var changed = original.Clone();
            changed.Description = "Must roll back.";
            changed.RequestParameters.Single(p => p.Id == suffix.Id).DisplayName = "Must roll back";

            // Someone else removes the parameter after the editor loaded the API
            bench.Service.Delete("customapirequestparameter", suffix.Id);

            var error = Assert.Catch(() => apis.Update(original, changed));
            Assert.That(CustomApiService.IsSaveOutcomeUnknown(error), Is.False, "The editor must report a server rejection as a failed save: " + error.Message);
            Assert.That(apis.Retrieve(echoApiId).Description, Is.EqualTo(original.Description), "The API change must roll back with the failed parameter change");
        }

        [Test, Order(9)]
        public void Update_ReplacingAParameter_KeepsTheApiWorking()
        {
            RequireApi();
            var original = apis.Retrieve(echoApiId);
            var changed = apis.Retrieve(echoApiId);
            var oldText = changed.RequestParameters.Single(p => p.UniqueName == EchoApiPlugin.InputName);

            // Delete and recreate with the same unique name in one save
            changed.RequestParameters.Remove(oldText);
            changed.RequestParameters.Add(Parameter<CustomApiRequestParameter>(EchoApiPlugin.InputName));
            Assert.That(apis.Update(original, changed), Is.True);

            var saved = apis.Retrieve(echoApiId);
            Assert.That(saved.RequestParameters.Select(p => p.UniqueName), Is.EqualTo(new[] { EchoApiPlugin.InputName }));
            Assert.That(saved.RequestParameters.Single().Id, Is.Not.EqualTo(oldText.Id));
            WaitForEcho(ExpectedEcho());
        }

        [Test, Order(10)]
        public void MakePrivate_KeepsTheApiListedAndItsStepEditable()
        {
            RequireApi();
            var original = apis.Retrieve(echoApiId);
            var changed = apis.Retrieve(echoApiId);
            changed.IsPrivate = true;
            Assert.That(apis.Update(original, changed), Is.True);
            Assert.That(apis.Retrieve(echoApiId).IsPrivate, Is.True);

            // The tool's general message query leaves out private messages; the Custom API catalog adds them back
            org = bench.LoadOrganization();
            TestContext.Progress.WriteLine($"The general message query {(org.Messages.ContainsKey(echoMessageId) ? "still includes" : "leaves out")} the private API's message.");
            Reload();

            Assert.That(catalog.Any(api => api.Id == echoApiId), Is.True, "A private API must stay listed");
            Assert.That(org.Messages.ContainsKey(echoMessageId), Is.True, "The step editor needs the private API's message");
            if (stepId != Guid.Empty)
            {
                Assert.That(EchoPluginNode()[stepId].MessageId, Is.EqualTo(echoMessageId));
            }

            // Private only hides the API from discovery; it can still be called
            WaitForEcho(ExpectedEcho());
        }

        [Test, Order(11)]
        public void AddToSolution_AddsTheApiAndItsParameters()
        {
            RequireApi();
            CreateSolution(SecondSolutionName);

            apis.AddToSolution(echoApiId, SecondSolutionName);

            var saved = apis.Retrieve(echoApiId);
            var expected = new[] { echoApiId }
                .Concat(saved.RequestParameters.Select(p => p.Id))
                .Concat(saved.ResponseProperties.Select(p => p.Id));
            Assert.That(SolutionComponents(SecondSolutionName), Is.SupersetOf(expected));
        }

        [Test, Order(12)]
        public void EntityBoundFunction_RoundTripsThroughTheEditorModel()
        {
            var draft = Definition(prefix + NamePart + "Bound" + RandomPart());
            draft.BindingType = CustomApiBindingType.Entity;
            draft.BoundEntityLogicalName = "account";
            draft.IsFunction = true;
            draft.AllowedCustomProcessingStepType = CustomApiProcessingStepType.SyncAndAsync;
            draft.ResponseProperties.Add(Parameter<CustomApiResponseProperty>("Result"));
            var id = apis.Create(draft, SolutionName);
            try
            {
                var saved = apis.Retrieve(id);
                Assert.That(saved.BindingType, Is.EqualTo(CustomApiBindingType.Entity));
                Assert.That(saved.BoundEntityLogicalName, Is.EqualTo("account"));
                Assert.That(saved.IsFunction, Is.True);
                TestContext.Progress.WriteLine($"The entity-bound API loads with {saved.RequestParameters.Count(p => p.UniqueName == "Target")} Target request parameter(s).");
                Assert.That(CustomApiValidator.Validate(apis.Retrieve(id), saved), Is.Empty, "A loaded entity-bound API must pass the editor's validation");

                var changed = apis.Retrieve(id);
                changed.Description = "Updated entity-bound API.";
                Assert.That(apis.Update(saved, changed), Is.True);
                Assert.That(apis.Retrieve(id).Description, Is.EqualTo(changed.Description));

                Reload();
                Assert.That(org.Messages[saved.SdkMessageId.Value].FindMessageEntity("account", "none"), Is.Not.Null,
                    "Steps on an entity-bound API are registered on its table");
            }
            finally
            {
                apis.Delete(id);
            }

            Assert.That(Exists("customapi", id), Is.False);
        }

        [Test, Order(13)]
        public void Delete_RemovesTheApiItsParametersAndMessage()
        {
            RequireApi();
            if (stepId != Guid.Empty)
            {
                // A step on the API's message can block deleting it, so unregister the step first
                RegistrationHelper.Unregister(org, null, EchoPluginNode()[stepId]);
                Assert.That(Exists("sdkmessageprocessingstep", stepId), Is.False);
                stepId = Guid.Empty;
            }

            apis.Delete(echoApiId);

            Assert.That(Exists("customapi", echoApiId), Is.False);
            Assert.That(HasParameters(echoApiId), Is.False);
            Assert.That(Exists("sdkmessage", echoMessageId), Is.False, "Dataverse removes the API's message with it");

            Reload();
            Assert.That(catalog.Any(api => api.Id == echoApiId), Is.False);
            Assert.That(org.Messages.ContainsKey(echoMessageId), Is.False);
            echoApiId = Guid.Empty;
        }

        private static CustomApiDefinition Definition(string uniqueName) => new CustomApiDefinition
        {
            Name = uniqueName,
            UniqueName = uniqueName,
            DisplayName = "PRT-IT " + uniqueName,
            Description = "Created by the Plugin Registration Tool integration tests.",
        };

        private static T Parameter<T>(string name) where T : CustomApiParameter, new() => new T
        {
            Name = name,
            UniqueName = name,
            DisplayName = name,
            Type = CustomApiParameterType.String,
        };

        private static string RandomPart() => Guid.NewGuid().ToString("N").Substring(0, 8);

        private string ExpectedEcho() => stepId == Guid.Empty
            ? EchoApiPlugin.Echo(Text)
            : EchoApiPlugin.AfterStep(EchoApiPlugin.Echo(Text), StepConfig);

        private CrmPlugin EchoPluginNode() => org[assemblyId].Plugins.Values.Single(p => p.TypeName == typeof(EchoApiPlugin).FullName);

        /// <summary>
        /// Loads the organization and its Custom APIs the way the tool does after connecting or refreshing.
        /// </summary>
        private void Reload()
        {
            org = bench.LoadOrganization();
            catalog = apis.List();
            OrganizationHelper.LoadCustomApiStepMessages(org, catalog);
        }

        private void RequireAssembly()
        {
            if (assemblyId == Guid.Empty) Assert.Inconclusive("The assembly was not registered by an earlier test.");
        }

        private void RequireApi()
        {
            if (echoApiId == Guid.Empty) Assert.Inconclusive("The Custom API was not created by an earlier test.");
        }

        /// <summary>
        /// Registration changes reach the sandbox after a short delay, so call the API until it answers as expected.
        /// </summary>
        private void WaitForEcho(string expected, string suffix = null)
        {
            var watch = Stopwatch.StartNew();
            string answer;
            while ((answer = TryExecute(suffix)) != expected)
            {
                if (watch.Elapsed > PropagationTimeout)
                {
                    Assert.Fail($"Timed out after {PropagationTimeout.TotalSeconds:0}s waiting for the API to answer '{expected}'. Last answer: {answer}");
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            TestContext.Progress.WriteLine($"Waited {watch.Elapsed.TotalSeconds:0}s for the API to answer '{expected}'.");
        }

        private string TryExecute(string suffix)
        {
            var request = new OrganizationRequest(echoApiName) { [EchoApiPlugin.InputName] = Text };
            if (suffix != null)
            {
                request[SuffixName] = suffix;
            }

            try
            {
                var results = bench.Service.Execute(request).Results;
                return results.Contains(EchoApiPlugin.OutputName) ? (string)results[EchoApiPlugin.OutputName] : null;
            }
            catch (Exception ex)
            {
                // Until the change reaches the server the call can fail; the last answer goes into the timeout message
                return "error: " + ex.Message;
            }
        }

        /// <summary>
        /// The configured publisher, or the default solution's publisher when none is configured or found.
        /// </summary>
        private Entity FindPublisher()
        {
            if (bench.Publisher != null)
            {
                var query = new QueryExpression("publisher") { ColumnSet = new ColumnSet("customizationprefix") };
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, bench.Publisher);
                var configured = bench.Service.RetrieveMultiple(query).Entities.FirstOrDefault();
                if (configured != null)
                {
                    return configured;
                }

                TestContext.Progress.WriteLine($"Publisher '{bench.Publisher}' was not found; using the default solution's publisher.");
            }

            var solution = new QueryExpression("solution") { ColumnSet = new ColumnSet("publisherid") };
            solution.Criteria.AddCondition("uniquename", ConditionOperator.Equal, "Default");
            var publisherId = bench.Service.RetrieveMultiple(solution).Entities.Single().GetAttributeValue<EntityReference>("publisherid").Id;
            return bench.Service.Retrieve("publisher", publisherId, new ColumnSet("customizationprefix"));
        }

        private void CreateSolution(string uniqueName)
        {
            bench.Service.Create(new Entity("solution")
            {
                ["uniquename"] = uniqueName,
                ["friendlyname"] = "PRT-IT " + uniqueName,
                ["publisherid"] = publisher.ToEntityReference(),
                ["version"] = "1.0.0.0",
            });
        }

        private List<Guid> SolutionComponents(string solutionUniqueName)
        {
            var query = new QueryExpression("solutioncomponent") { ColumnSet = new ColumnSet("objectid") };
            query.AddLink("solution", "solutionid", "solutionid").LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);
            return bench.Service.RetrieveMultiple(query).Entities.Select(e => e.GetAttributeValue<Guid>("objectid")).ToList();
        }

        private bool HasParameters(Guid apiId)
        {
            return new[] { "customapirequestparameter", "customapiresponseproperty" }.Any(table =>
            {
                var query = new QueryExpression(table) { ColumnSet = new ColumnSet(false), TopCount = 1 };
                query.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
                return bench.Service.RetrieveMultiple(query).Entities.Count > 0;
            });
        }

        private bool Exists(string entity, Guid id)
        {
            var query = new QueryExpression(entity) { ColumnSet = new ColumnSet(false), TopCount = 1 };
            query.Criteria.AddCondition(entity + "id", ConditionOperator.Equal, id);
            return bench.Service.RetrieveMultiple(query).Entities.Count > 0;
        }

        /// <summary>
        /// Removes what an earlier, interrupted run may have left behind. Steps on the APIs' messages and
        /// the APIs go first, because the assembly's types are their handlers.
        /// </summary>
        private void RemoveLeftovers()
        {
            var leftovers = new QueryExpression("customapi") { ColumnSet = new ColumnSet("uniquename", "sdkmessageid") };
            leftovers.Criteria.AddCondition("uniquename", ConditionOperator.BeginsWith, prefix + NamePart);
            foreach (var api in bench.Service.RetrieveMultiple(leftovers).Entities)
            {
                TestContext.Progress.WriteLine("Removing leftover Custom API " + api.GetAttributeValue<string>("uniquename"));
                var messageId = api.GetAttributeValue<EntityReference>("sdkmessageid")?.Id;
                if (messageId.HasValue)
                {
                    var steps = new QueryExpression("sdkmessageprocessingstep") { ColumnSet = new ColumnSet(false) };
                    steps.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageId.Value);
                    foreach (var step in bench.Service.RetrieveMultiple(steps).Entities)
                    {
                        bench.Service.Delete("sdkmessageprocessingstep", step.Id);
                    }
                }

                apis.Delete(api.Id);
            }

            org = bench.LoadOrganization();
            var assemblies = org.Assemblies.Values.Where(a => a.Name == AssemblyName).Cast<ICrmEntity>().ToArray();
            if (assemblies.Length > 0)
            {
                TestContext.Progress.WriteLine("Removing the leftover test plug-in assembly.");
                RegistrationHelper.Unregister(org, null, assemblies);
            }

            var solutions = new QueryExpression("solution") { ColumnSet = new ColumnSet(false) };
            solutions.Criteria.AddCondition("uniquename", ConditionOperator.In, SolutionName, SecondSolutionName);
            foreach (var solution in bench.Service.RetrieveMultiple(solutions).Entities)
            {
                bench.Service.Delete("solution", solution.Id);
            }
        }
    }
}

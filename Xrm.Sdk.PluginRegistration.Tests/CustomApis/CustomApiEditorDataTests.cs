using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using NUnit.Framework;
using System;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    [TestFixture]
    public class CustomApiEditorDataTests
    {
        [Test]
        public void EditingLoadsHandlersWithoutRequiringSolutionAccess()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => query.EntityName == "solution"
                    ? throw new InvalidOperationException("Solution access denied") : new EntityCollection()
            };
            var currentHandler = Guid.NewGuid();
            var data = CustomApiEditorData.Load(service, currentHandler, includeSolutions: false);

            Assert.That(data.Solutions, Is.Empty);
            Assert.That(data.PluginTypes.Single().Id, Is.EqualTo(currentHandler));
            Assert.That(service.Queries.Single().EntityName, Is.EqualTo("plugintype"));
        }

        [Test]
        public void CloneKeepsServerStateAndSeparatesAllEditableChildren()
        {
            var original = new CustomApiDefinition
            {
                Id = Guid.NewGuid(), Name = "API", UniqueName = "sample_API", DisplayName = "API display",
                Description = "Description", BindingType = CustomApiBindingType.Entity, BoundEntityLogicalName = "account",
                AllowedCustomProcessingStepType = CustomApiProcessingStepType.AsyncOnly, ExecutePrivilegeName = "prvReadAccount",
                PluginTypeId = Guid.NewGuid(), SdkMessageId = Guid.NewGuid(), IsFunction = true, IsPrivate = true,
                WorkflowSdkStepEnabled = true, IsManaged = true, IsCustomizable = false
            };
            original.RequestParameters.Add(new CustomApiRequestParameter
            {
                Id = Guid.NewGuid(), Name = "Input", UniqueName = "Input", DisplayName = "Input", Description = "Input text",
                LogicalEntityName = "account", Type = CustomApiParameterType.EntityReference,
                IsOptional = true, IsManaged = true, IsCustomizable = false
            });
            original.ResponseProperties.Add(new CustomApiResponseProperty
            {
                Id = Guid.NewGuid(), Name = "Result", Type = CustomApiParameterType.String, IsCustomizable = false
            });

            var draft = original.Clone();

            // Compare every scalar property so new server flags cannot be silently lost in a future editor copy.
            foreach (var property in typeof(CustomApiDefinition).GetProperties().Where(p => p.PropertyType.Namespace == "System" || p.PropertyType.IsEnum))
                Assert.That(property.GetValue(draft), Is.EqualTo(property.GetValue(original)), property.Name);
            foreach (var property in typeof(CustomApiRequestParameter).GetProperties())
                Assert.That(property.GetValue(draft.RequestParameters[0]), Is.EqualTo(property.GetValue(original.RequestParameters[0])), property.Name);
            draft.RequestParameters[0].Description = "Draft input";
            draft.ResponseProperties[0].Name = "Draft result";
            draft.RequestParameters.Clear();
            Assert.That(original.RequestParameters.Single().Description, Is.EqualTo("Input text"));
            Assert.That(original.ResponseProperties.Single().Name, Is.EqualTo("Result"));
        }

        [Test]
        public void EmptyLookupsAreValidAndCurrentHandlerIsRetained()
        {
            var current = Guid.NewGuid();
            var service = new RecordingOrganizationService { OnRetrieveMultiple = _ => new EntityCollection() };

            var data = CustomApiEditorData.Load(service, current);

            Assert.That(data.Solutions, Is.Empty);
            Assert.That(data.PluginTypes.Single().Id, Is.EqualTo(current));
            var solutions = service.Queries.Single(q => q.EntityName == "solution");
            Assert.That(solutions.Criteria.Conditions.Any(c => c.AttributeName == "ismanaged" && Equals(c.Values.Single(), false)), Is.True);
            var types = service.Queries.Single(q => q.EntityName == "plugintype");
            Assert.That(types.Criteria.FilterOperator, Is.EqualTo(LogicalOperator.Or));
            Assert.That(types.Criteria.Conditions.Single().Values.Single(), Is.EqualTo(current));
            Assert.That(types.LinkEntities.Single().JoinOperator, Is.EqualTo(JoinOperator.LeftOuter));
        }

        [Test]
        public void LookupQueriesFollowPagingAndPreserveAssemblyLabels()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    var row = query.EntityName == "solution"
                        ? new Entity("solution", Guid.NewGuid())
                        {
                            ["uniquename"] = "Solution" + query.PageInfo.PageNumber, ["friendlyname"] = "Solution",
                            ["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "sample")
                        }
                        : new Entity("plugintype", Guid.NewGuid())
                        {
                            ["typename"] = "Example.Handler" + query.PageInfo.PageNumber,
                            ["assembly.name"] = new AliasedValue("pluginassembly", "name", "Example")
                        };
                    return new EntityCollection(new[] { row }) { MoreRecords = query.PageInfo.PageNumber == 1, PagingCookie = "page-one" };
                }
            };

            var data = CustomApiEditorData.Load(service, null);

            Assert.That(data.Solutions, Has.Count.EqualTo(2));
            Assert.That(data.Solutions.All(s => s.PublisherPrefix == "sample"), Is.True);
            Assert.That(data.PluginTypes, Has.Count.EqualTo(2));
            Assert.That(data.PluginTypes.All(p => p.AssemblyName == "Example"), Is.True);
            Assert.That(service.QueryPageNumbers, Is.EqualTo(new[] { 1, 2, 1, 2 }));
            Assert.That(service.QueryPagingCookies, Is.EqualTo(new[] { null, "page-one", null, "page-one" }));
        }

        [TestCase("Bad name", "Valid", "1.0.0.0")]
        [TestCase("Valid", "", "1.0.0.0")]
        [TestCase("Valid", "Valid", "1.0")]
        public void InvalidSolutionInputsStopBeforeWriting(string name, string display, string version)
        {
            var service = new RecordingOrganizationService();
            Assert.Throws<ArgumentException>(() => CustomApiEditorData.CreateSolution(service, name, display, version, Publisher()));
            Assert.That(service.Requests, Is.Empty);
        }

        [Test]
        public void SolutionCreationUsesSelectedPublisherAndReturnsTheSavedId()
        {
            var id = Guid.NewGuid();
            var publisher = Publisher();
            var service = new RecordingOrganizationService { OnExecute = _ => new CreateResponse { Results = { ["id"] = id } } };
            var solution = CustomApiEditorData.CreateSolution(service, "Example", "Example solution", "1.0.0.0", publisher);

            var request = (CreateRequest)service.Requests.Single();
            Assert.That(request.Target.LogicalName, Is.EqualTo("solution"));
            Assert.That(request.Target.GetAttributeValue<EntityReference>("publisherid").Id, Is.EqualTo(publisher.Id));
            Assert.That(request.Target.GetAttributeValue<string>("version"), Is.EqualTo("1.0.0.0"));
            Assert.That(solution.Id, Is.EqualTo(id));
            Assert.That(solution.PublisherPrefix, Is.EqualTo(publisher.Prefix));
        }

        [Test]
        public void LostSolutionCreateResponseIsNeverRetried()
        {
            var service = new RecordingOrganizationService { OnExecute = _ => throw new TimeoutException() };
            Assert.Throws<TimeoutException>(() => CustomApiEditorData.CreateSolution(service, "Example", "Example", "1.0.0.0", Publisher()));
            Assert.That(service.Requests, Has.Count.EqualTo(1));
        }

        private static CustomApiPublisherOption Publisher() => new CustomApiPublisherOption
        {
            Id = Guid.NewGuid(), Name = "Example publisher", Prefix = "sample"
        };
    }
}

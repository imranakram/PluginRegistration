using Microsoft.Xrm.Sdk;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    /// <summary>
    /// The step form's Message and Primary Entity lists, and the Async Only rule for Custom API messages.
    /// </summary>
    [TestFixture]
    public class StepMessageChoicesTests
    {
        [Test]
        public void CustomApiMessagesAreMarkedAndEveryMessageIsKeyedById()
        {
            var create = Message("Create");
            var echo = Message("cr418_Echo");
            var apis = new[] { new CustomApiSummary { UniqueName = "cr418_Echo", SdkMessageId = echo.MessageId } };

            var choices = StepMessageChoices.Messages(new[] { create, echo }, apis);

            Assert.That(choices.Select(c => c.Value), Is.EqualTo(new[] { "Create", "cr418_Echo" }));
            Assert.That(choices.Select(c => c.Detail), Is.EqualTo(new[] { null, StepMessageChoices.CustomApiDetail }));
            Assert.That(choices.Select(c => c.Key), Is.EqualTo(new object[] { create.MessageId, echo.MessageId }));
            Assert.That(Controls.FilteringComboBox.Filter(choices, "custom api").Single().Value, Is.EqualTo("cr418_Echo"),
                "Typing \"custom api\" finds the Custom API messages.");
        }

        [Test]
        public void MessagesWithoutCustomApisAreListedPlain()
        {
            var choices = StepMessageChoices.Messages(new[] { Message("Update"), null }, null);

            Assert.That(choices.Single().Detail, Is.Null);
        }

        [Test]
        public void PrimaryEntitiesAreTheMessagesTablesOnceEach()
        {
            var update = Message("Update", "account", "contact", "Account");
            var global = Message("cr418_Echo", "none");

            // As Microsoft's tool always has, "none" is offered too: for Update it registers on every table.
            var tables = StepMessageChoices.PrimaryEntities(update);
            Assert.That(tables.Select(c => c.Value), Is.EquivalentTo(new[] { "account", "contact", "none" }));
            Assert.That(tables.Single(c => c.Value == "none").Detail, Is.EqualTo(StepMessageChoices.AllTablesDetail));
            var none = StepMessageChoices.PrimaryEntities(global).Single();
            Assert.That(none.Value, Is.EqualTo("none"));
            Assert.That(none.Detail, Is.EqualTo(StepMessageChoices.NoTableDetail));
            Assert.That(StepMessageChoices.PrimaryEntities(null), Is.Empty);
        }

        [Test]
        public void OnlyAnAsyncOnlyApiLimitsTheStep()
        {
            var asyncOnly = Guid.NewGuid();
            var syncAndAsync = Guid.NewGuid();
            var notRead = Guid.NewGuid();
            var apis = new[]
            {
                new CustomApiSummary { UniqueName = "cr418_Later", SdkMessageId = asyncOnly, AllowedCustomProcessingStepType = CustomApiProcessingStepType.AsyncOnly },
                new CustomApiSummary { UniqueName = "cr418_Any", SdkMessageId = syncAndAsync, AllowedCustomProcessingStepType = CustomApiProcessingStepType.SyncAndAsync },
                new CustomApiSummary { UniqueName = "cr418_Unknown", SdkMessageId = notRead }
            };

            var limit = StepMessageChoices.LimitFor(asyncOnly, apis);
            Assert.That(limit.ApiName, Is.EqualTo("cr418_Later"));
            Assert.That(limit.Hint, Does.Contain("Async Only").And.Contain("cr418_Later"));
            Assert.That(StepMessageChoices.LimitFor(syncAndAsync, apis), Is.Null);
            Assert.That(StepMessageChoices.LimitFor(notRead, apis), Is.Null);
            Assert.That(StepMessageChoices.LimitFor(Guid.NewGuid(), apis), Is.Null, "A message of no Custom API");
            Assert.That(StepMessageChoices.LimitFor(null, apis), Is.Null);
            Assert.That(StepMessageChoices.LimitFor(asyncOnly, null), Is.Null);
        }

        [Test]
        public void TheListReadsEachApisAllowedStepType()
        {
            var organization = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query =>
                {
                    Assert.That(query.ColumnSet.Columns, Does.Contain("allowedcustomprocessingsteptype"));
                    var asyncOnly = new Entity("customapi", Guid.NewGuid()) { ["uniquename"] = "cr418_Later" };
                    asyncOnly["allowedcustomprocessingsteptype"] = new OptionSetValue((int)CustomApiProcessingStepType.AsyncOnly);
                    return new EntityCollection(new[] { asyncOnly, new Entity("customapi", Guid.NewGuid()) { ["uniquename"] = "cr418_Plain" } });
                }
            };

            var apis = new CustomApiService(organization).List();

            Assert.That(apis.Single(a => a.UniqueName == "cr418_Later").AllowedCustomProcessingStepType, Is.EqualTo(CustomApiProcessingStepType.AsyncOnly));
            Assert.That(apis.Single(a => a.UniqueName == "cr418_Plain").AllowedCustomProcessingStepType, Is.Null);
        }

        private static CrmMessage Message(string name, params string[] tables)
        {
            var message = new CrmMessage(null, Guid.NewGuid(), name, false, 1, null, null, new List<ImageMessagePropertyName>());
            foreach (var table in tables)
            {
                message.AddMessageEntity(new CrmMessageEntity(null, message.MessageId, Guid.NewGuid(), table, "none",
                    CrmPluginStepDeployment.ServerOnly, null, null));
            }
            return message;
        }
    }
}

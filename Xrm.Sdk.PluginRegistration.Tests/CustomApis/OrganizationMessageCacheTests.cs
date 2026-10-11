namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Models;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    [TestFixture]
    internal class OrganizationMessageCacheTests
    {
        [Test]
        public void KnownPrivateApiUsesServerStepFiltersWithoutExposingUnrelatedMessages()
        {
            var message = Message("dx_PrivateApi");
            var filter = Filter(message.Id);
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => new EntityCollection(new[] { query.EntityName == "sdkmessage" ? message : filter })
            };
            var organization = Organization(service);

            OrganizationHelper.LoadCustomApiStepMessages(organization, new[] { new CustomApiSummary { SdkMessageId = message.Id } });

            Assert.That(organization.FindMessage("dx_PrivateApi").MessageId, Is.EqualTo(message.Id));
            Assert.That(organization.Messages[message.Id][filter.Id].PrimaryEntity, Is.EqualTo("account"));
            Assert.That(organization.MessageEntities.ContainsKey(filter.Id), Is.True);
            var filterQuery = service.Queries.First();
            Assert.That(filterQuery.Criteria.Conditions.Single(c => c.AttributeName == "sdkmessageid").Values,
                Is.EquivalentTo(new[] { message.Id }));
            Assert.That(filterQuery.Criteria.Conditions.Single(c => c.AttributeName == "iscustomprocessingstepallowed").Values.Single(), Is.EqualTo(true));
            Assert.That(filterQuery.Criteria.Conditions.Any(c => c.AttributeName == "isvisible"), Is.False);
            Assert.That(service.Queries.Last().Criteria.Conditions.Single().Values, Is.EquivalentTo(new[] { message.Id }));
        }

        [Test]
        public void ApiWithoutEligibleStepFiltersIsNotAddedToStepChoices()
        {
            var service = new RecordingOrganizationService { OnRetrieveMultiple = query => new EntityCollection() };
            var organization = Organization(service);

            OrganizationHelper.LoadCustomApiStepMessages(organization, new[] { new CustomApiSummary { SdkMessageId = Guid.NewGuid() } });

            Assert.That(organization.Messages, Is.Empty);
            Assert.That(organization.MessageEntities, Is.Empty);
            Assert.That(service.Queries.Single().EntityName, Is.EqualTo("sdkmessagefilter"));
        }

        [Test]
        public void HydratingAgainDoesNotDuplicateMessagesOrFilters()
        {
            var message = Message("dx_PrivateApi");
            var filter = Filter(message.Id);
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => new EntityCollection(new[] { query.EntityName == "sdkmessage" ? message : filter })
            };
            var organization = Organization(service);
            var summaries = new[] { new CustomApiSummary { SdkMessageId = message.Id }, new CustomApiSummary { SdkMessageId = message.Id } };

            OrganizationHelper.LoadCustomApiStepMessages(organization, summaries);
            OrganizationHelper.LoadCustomApiStepMessages(organization, summaries);

            Assert.That(organization.Messages.Count, Is.EqualTo(1));
            Assert.That(organization.MessageEntities.Count, Is.EqualTo(1));
            Assert.That(service.Queries, Has.Count.EqualTo(2));
        }

        [Test]
        public void FailedMessageReadDoesNotLeavePartialStepChoices()
        {
            var messageId = Guid.NewGuid();
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => query.EntityName == "sdkmessagefilter"
                    ? new EntityCollection(new[] { Filter(messageId) })
                    : throw new InvalidOperationException("Read denied")
            };
            var organization = Organization(service);

            Assert.Throws<InvalidOperationException>(() => OrganizationHelper.LoadCustomApiStepMessages(organization,
                new[] { new CustomApiSummary { SdkMessageId = messageId } }));

            Assert.That(organization.Messages, Is.Empty);
            Assert.That(organization.MessageEntities, Is.Empty);
        }

        [Test]
        public void CustomApiTableSuggestionsUseLoadedMessageFilters()
        {
            var message = Message("dx_PrivateApi");
            var filters = new[] { Filter(message.Id, "contact"), Filter(message.Id, "none"), Filter(message.Id), Filter(message.Id, "Account") };
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => query.EntityName == "sdkmessage"
                    ? new EntityCollection(new[] { message }) : new EntityCollection(filters)
            };
            var organization = Organization(service);
            OrganizationHelper.LoadCustomApiStepMessages(organization, new[] { new CustomApiSummary { SdkMessageId = message.Id } });
            int queries = service.Queries.Count;

            var tables = (List<string>)typeof(MainControl).GetMethod("CustomApiTableNames", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { organization });

            Assert.That(tables, Is.EqualTo(new[] { "account", "contact" }).IgnoreCase);
            Assert.That(service.Queries, Has.Count.EqualTo(queries), "Suggestions must not query the server.");
        }

        private static Entity Filter(Guid messageId, string table = "account")
        {
            var id = Guid.NewGuid();
            return new Entity("sdkmessagefilter", id)
            {
                ["sdkmessagefilterid"] = id, ["sdkmessageid"] = new EntityReference("sdkmessage", messageId),
                ["primaryobjecttypecode"] = table, ["availability"] = 0
            };
        }

        private static Entity Message(string name)
        {
            var id = Guid.NewGuid();
            return new Entity("sdkmessage", id) { ["sdkmessageid"] = id, ["name"] = name };
        }

        private static RecordingOrganizationService Service(Entity message)
        {
            return new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => new EntityCollection(new[] { message })
            };
        }

        private static CrmOrganization Organization(IOrganizationService service)
        {
            // The public constructor loads the entire registration tree. These tests need
            // only the service boundary and independent message collections.
            var organization = (CrmOrganization)FormatterServices.GetUninitializedObject(typeof(CrmOrganization));
            typeof(CrmOrganization).GetField("m_organizationService", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(organization, service);
            typeof(CrmOrganization).GetField("m_messageList", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(organization, new Dictionary<Guid, CrmMessage>());
            typeof(CrmOrganization).GetField("m_messageEntityList", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(organization, new Dictionary<Guid, CrmMessageEntity>());
            return organization;
        }
    }
}

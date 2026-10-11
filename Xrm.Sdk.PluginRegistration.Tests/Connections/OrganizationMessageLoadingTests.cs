namespace Xrm.Sdk.PluginRegistration.Tests.Connections
{
    using Microsoft.Xrm.Sdk;
    using Microsoft.Xrm.Sdk.Query;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Wrappers;

    [TestFixture]
    internal class OrganizationMessageLoadingTests
    {
        [Test]
        [NonParallelizable]
        public void ConcurrentInitialConnectionsShareCompleteMessageColumns()
        {
            var columns = (IDictionary<string, ColumnSet>)typeof(OrganizationHelper)
                .GetField("m_entityColumns", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            const int connectionCount = 16;
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // Force a cold cache even when other tests have already loaded messages.
                // All workers finish before the next reset, and each owns its service.
                columns.Remove("sdkmessage");
                var messages = Enumerable.Range(0, connectionCount)
                    .Select(index => Message("dx_Concurrent_" + index)).ToArray();
                var services = messages.Select(Service).ToArray();
                var organizations = services.Select(Organization).ToArray();
                using (var ready = new CountdownEvent(connectionCount))
                using (var start = new ManualResetEventSlim())
                {
                    var loads = organizations.Select(organization => Task.Factory.StartNew(() =>
                    {
                        ready.Signal();
                        if (!start.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                        return OrganizationHelper.LoadMessages(organization);
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
                    try
                    {
                        Assert.That(ready.Wait(TimeSpan.FromSeconds(10)), Is.True);
                    }
                    finally
                    {
                        start.Set();
                        Task.WaitAll(loads);
                    }

                    var cachedColumns = services[0].Queries.Single().ColumnSet;
                    Assert.That(cachedColumns.Columns, Is.EquivalentTo(new[]
                    {
                        "sdkmessageid", "createdon", "modifiedon", "name", "customizationlevel"
                    }));
                    for (int index = 0; index < connectionCount; index++)
                    {
                        Assert.That(loads[index].Result.Keys, Is.EquivalentTo(new[] { messages[index].Id }));
                        Assert.That(services[index].Queries, Has.Count.EqualTo(1));
                        Assert.That(services[index].Queries[0].ColumnSet, Is.SameAs(cachedColumns));
                    }
                }
            }
        }

        [Test]
        public void SlowConnectionDoesNotBlockAnotherConnectionsMessageLoad()
        {
            using (var started = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var slow = Organization(new RecordingOrganizationService
                {
                    OnRetrieveMultiple = query =>
                    {
                        started.Set();
                        if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                        return new EntityCollection(new[] { Message("dx_Slow") });
                    }
                });
                var fast = Organization(Service(Message("dx_Fast")));
                var slowLoad = Task.Run(() => OrganizationHelper.LoadMessages(slow));
                Task fastLoad = null;
                try
                {
                    Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                    fastLoad = Task.Run(() => OrganizationHelper.LoadMessages(fast));
                    Assert.That(fastLoad.Wait(TimeSpan.FromSeconds(5)), Is.True,
                        "An unrelated connection must finish while the first network read is blocked.");
                }
                finally
                {
                    release.Set();
                    slowLoad.GetAwaiter().GetResult();
                    fastLoad?.GetAwaiter().GetResult();
                }
            }
        }

        [Test]
        public void ConnectionsHaveIndependentMessageIdentifiers()
        {
            var firstMessage = Message("dx_Example");
            var secondMessage = Message("dx_Example");
            var firstService = Service(firstMessage);
            var secondService = Service(secondMessage);
            var first = Organization(firstService);
            var second = Organization(secondService);

            var firstMessages = OrganizationHelper.LoadMessages(first);
            var secondMessages = OrganizationHelper.LoadMessages(second);

            Assert.That(firstMessages.ContainsKey(firstMessage.Id), Is.True);
            Assert.That(secondMessages.ContainsKey(secondMessage.Id), Is.True);
            Assert.That(secondMessages.ContainsKey(firstMessage.Id), Is.False);
            Assert.That(firstService.Queries, Has.Count.EqualTo(1));
            Assert.That(secondService.Queries, Has.Count.EqualTo(1));
        }

        [Test]
        public void ReloadingReadsServerChangesWithoutMutatingEarlierSnapshots()
        {
            var message = Message("Assign");
            var service = Service(message);
            var organization = Organization(service);
            var snapshot = OrganizationHelper.LoadMessages(organization);
            var initial = snapshot[message.Id];
            initial.Name = "Changed locally";
            initial.ImageMessagePropertyNames.Clear();
            var addedMessage = Message("dx_Added");
            service.OnRetrieveMultiple = query => new EntityCollection(new[] { message, addedMessage });

            var refreshed = OrganizationHelper.LoadMessages(organization);
            var reloaded = refreshed[message.Id];

            Assert.That(refreshed.ContainsKey(addedMessage.Id), Is.True);
            Assert.That(snapshot.ContainsKey(addedMessage.Id), Is.False);
            Assert.That(reloaded.Name, Is.EqualTo("Assign"));
            Assert.That(reloaded.ImageMessagePropertyNames, Has.Count.EqualTo(1));
            Assert.That(reloaded, Is.Not.SameAs(initial));
            Assert.That(service.Queries, Has.Count.EqualTo(2));
        }

        [Test]
        public void FailedRetrievalCanBeRetried()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => throw new InvalidOperationException("Server unavailable")
            };
            var organization = Organization(service);
            Assert.Throws<InvalidOperationException>(() => OrganizationHelper.LoadMessages(organization));
            var message = Message("dx_Recovered");
            service.OnRetrieveMultiple = query => new EntityCollection(new[] { message });

            Assert.That(OrganizationHelper.LoadMessages(organization).ContainsKey(message.Id), Is.True);
            Assert.That(service.Queries, Has.Count.EqualTo(2));
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

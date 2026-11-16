using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xrm.Sdk.PluginRegistration.Controls;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;
using Label = System.Windows.Forms.Label;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class CustomApiMetadataTests
    {
        [Test]
        public void LoadAsksOnlyForTableNamesAndSortsBothLists()
        {
            var service = Service(tables: new[] { Table("contact", "Contact"), Table("account", "Account"), Table("new_x", null) },
                privileges: new[] { "prvWriteAccount", "prvReadAccount", "prvReadAccount" });

            var metadata = CustomApiMetadata.Load(service);

            Assert.That(metadata.HasErrors, Is.False);
            var request = service.Requests.OfType<RetrieveMetadataChangesRequest>().Single();
            Assert.That(request.Query.Properties.AllProperties, Is.False);
            Assert.That(request.Query.Properties.PropertyNames, Is.EquivalentTo(new[] { "LogicalName", "DisplayName" }));
            Assert.That(request.Query.AttributeQuery, Is.Null, "Columns are not needed for the picker.");
            var condition = request.Query.Criteria.Conditions.Single();
            Assert.That((condition.PropertyName, condition.ConditionOperator, condition.Value),
                Is.EqualTo(("IsPrivate", MetadataConditionOperator.Equals, (object)false)));
            Assert.That(metadata.Tables.Select(t => t.Value), Is.EqualTo(new[] { "account", "contact", "new_x" }));
            Assert.That(metadata.Tables.Select(t => t.Detail), Is.EqualTo(new[] { "Account", "Contact", null }));

            var query = service.Queries.Single();
            Assert.That(query.EntityName, Is.EqualTo("privilege"));
            Assert.That(query.ColumnSet.Columns, Is.EqualTo(new[] { "name" }));
            Assert.That(metadata.Privileges.Select(p => p.Value), Is.EqualTo(new[] { "prvReadAccount", "prvWriteAccount" }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AFailureInOneListKeepsTheOther(bool tablesFail)
        {
            var service = Service(tables: tablesFail ? null : new[] { Table("account", "Account") },
                privileges: tablesFail ? new[] { "prvReadAccount" } : null);

            var metadata = CustomApiMetadata.Load(service);

            Assert.That(metadata.TableError != null, Is.EqualTo(tablesFail));
            Assert.That(metadata.PrivilegeError != null, Is.EqualTo(!tablesFail));
            Assert.That(metadata.Tables.Count, Is.EqualTo(tablesFail ? 0 : 1));
            Assert.That(metadata.Privileges.Count, Is.EqualTo(tablesFail ? 1 : 0));
        }

        [Test]
        public void CacheSharesOneLoadUntilReloaded()
        {
            int loads = 0;
            using (var release = new ManualResetEventSlim())
            {
                var cache = new CustomApiMetadataCache(() =>
                {
                    Interlocked.Increment(ref loads);
                    release.Wait(TimeSpan.FromSeconds(10));
                    return CustomApiMetadata.Load(Service(new[] { Table("account", "Account") }, new[] { "prvReadAccount" }));
                });

                var first = cache.GetAsync();
                var second = cache.GetAsync();
                Assert.That(second, Is.SameAs(first), "Editors opened while loading share the load.");
                release.Set();
                first.Wait(TimeSpan.FromSeconds(10));
                Assert.That(cache.GetAsync(), Is.SameAs(first), "A good result is kept for the connection.");

                var reloaded = cache.GetAsync(reload: true);
                Assert.That(reloaded, Is.Not.SameAs(first));
                reloaded.Wait(TimeSpan.FromSeconds(10));
                Assert.That(loads, Is.EqualTo(2));
            }
        }

        [Test]
        public void CacheReadsAgainAfterAFailedLoad()
        {
            int loads = 0;
            var cache = new CustomApiMetadataCache(() =>
            {
                bool fail = Interlocked.Increment(ref loads) == 1;
                return CustomApiMetadata.Load(Service(new[] { Table("account", "Account") }, fail ? null : new[] { "prvReadAccount" }));
            });

            var failed = cache.GetAsync().Result;
            Assert.That(failed.PrivilegeError, Is.Not.Null);
            var retried = cache.GetAsync().Result;
            Assert.That(retried.HasErrors, Is.False);
            Assert.That(cache.GetAsync().Result, Is.SameAs(retried));
            Assert.That(loads, Is.EqualTo(2));
        }

        [TestCase("acc", true)]
        [TestCase("ACC", true)]
        [TestCase("Person", true)]
        [TestCase("ount", true)]
        [TestCase(" ", true)]
        [TestCase("lead", false)]
        public void MatchesFindsTheTextInTheNameOrTheDisplayName(string text, bool expected)
        {
            Assert.That(FilteringComboBox.Matches(new SearchChoice("account", "Account Person"), text), Is.EqualTo(expected));
        }

        [Test]
        public void FilterListsNamesStartingWithTheTextFirstAndCapsTheList()
        {
            var choices = new[] { new SearchChoice("new_account"), new SearchChoice("account"), new SearchChoice("contact", "Account contact") };
            Assert.That(FilteringComboBox.Filter(choices, "account").Select(c => c.Value),
                Is.EqualTo(new[] { "account", "new_account", "contact" }));
            Assert.That(FilteringComboBox.Filter(choices, "").Count, Is.EqualTo(3));

            var many = Enumerable.Range(0, 5000).Select(i => new SearchChoice("prvRead" + i)).ToList();
            Assert.That(FilteringComboBox.Filter(many, "prvRead").Count, Is.EqualTo(FilteringComboBox.MaximumMatches));
            Assert.That(FilteringComboBox.Filter(many, null).Count, Is.EqualTo(5000), "Opening the list without text shows everything.");
        }

        [Test]
        public void TypingNarrowsTheListAndKeepsTheTypedText()
        {
            using (var form = new Form())
            using (var combo = new FilteringComboBox())
            {
                form.Controls.Add(combo);
                combo.SetChoices(new[] { new SearchChoice("contact", "Contact"), new SearchChoice("account", "Account"), new SearchChoice("account", "Duplicate") });
                Assert.That(combo.Items.Cast<SearchChoice>().Select(c => c.Value), Is.EqualTo(new[] { "account", "contact" }));

                combo.FilterTo("cont");
                Assert.That(combo.Text, Is.EqualTo("cont"), "Filtering must not replace what was typed.");
                Assert.That(combo.Items.Cast<SearchChoice>().Select(c => c.Value), Is.EqualTo(new[] { "contact" }));

                combo.FilterTo("custom_table");
                Assert.That(combo.Text, Is.EqualTo("custom_table"), "A name that is not listed can still be entered.");
                Assert.That(combo.Items.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void EditorFillsThePickersFromTheCacheAndReloadReadsAgain()
        {
            int loads = 0;
            var cache = new CustomApiMetadataCache(() =>
            {
                int load = Interlocked.Increment(ref loads);
                var tables = new[] { Table("account", "Account"), Table("new_table" + load, "New table") };
                return CustomApiMetadata.Load(Service(tables, new[] { "prvReadAccount", "prvCustomThing" }));
            });
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), null, new[] { "account" }, cache))
            {
                var table = Field<FilteringComboBox>(dialog, "boundTable");
                var privilege = Field<FilteringComboBox>(dialog, "executePrivilege");
                Assert.That(table.Choices.Select(c => c.Value), Is.EqualTo(new[] { "account" }), "Message-filter tables until the full list arrives.");

                LoadMetadata(dialog, reload: false);
                Assert.That(table.Choices.Select(c => c.Value), Is.EqualTo(new[] { "account", "new_table1" }));
                Assert.That(table.Choices.First().Detail, Is.EqualTo("Account"));
                Assert.That(privilege.Choices.Select(c => c.Value), Is.EqualTo(new[] { "prvCustomThing", "prvReadAccount" }));
                Assert.That(Field<Label>(dialog, "metadataNote").Text, Is.Empty);

                LoadMetadata(dialog, reload: true);
                Assert.That(table.Choices.Select(c => c.Value), Does.Contain("new_table2"), "Reload choices picks up a new table.");
                Assert.That(loads, Is.EqualTo(2));

                privilege.Text = "prvNotListed";
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").ExecutePrivilegeName, Is.EqualTo("prvNotListed"));
            }
        }

        [Test]
        public void EditorKeepsTheFallbackTablesAndSaysSoWhenTablesCannotBeLoaded()
        {
            var cache = new CustomApiMetadataCache(() => CustomApiMetadata.Load(Service(null, new[] { "prvReadAccount" })));
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), null, new[] { "account" }, cache))
            {
                LoadMetadata(dialog, reload: false);
                Assert.That(Field<FilteringComboBox>(dialog, "boundTable").Choices.Select(c => c.Value), Is.EqualTo(new[] { "account" }));
                Assert.That(Field<FilteringComboBox>(dialog, "executePrivilege").Choices.Count, Is.EqualTo(1));
                Assert.That(Field<Label>(dialog, "metadataNote").Text, Does.Contain("table list could not be loaded"));
            }
        }

        [Test]
        public void ProtectedManagedApisKeepTheirPrivilegeFixed()
        {
            var api = new CustomApiDefinition
            {
                Id = Guid.NewGuid(), Name = "Managed", UniqueName = "new_Managed", DisplayName = "Managed",
                Description = "Managed", ExecutePrivilegeName = "prvReadAccount", IsManaged = true, IsCustomizable = false
            };
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), api))
            {
                var privilege = Field<FilteringComboBox>(dialog, "executePrivilege");
                Assert.That(privilege.Text, Is.EqualTo("prvReadAccount"));
                Assert.That(privilege.Enabled, Is.False);
            }
        }

        private static void LoadMetadata(CustomApiEditorForm dialog, bool reload)
        {
            var task = Invoke<Task>(dialog, "LoadMetadataChoices", reload);
            var timeout = Stopwatch.StartNew();
            while (!task.IsCompleted && timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            Assert.That(task.IsCompleted, Is.True, "Metadata should load.");
            task.GetAwaiter().GetResult();
        }

        /// <summary>A service that answers the table and privilege reads, or fails the one given as null.</summary>
        private static RecordingOrganizationService Service(EntityMetadata[] tables, string[] privileges)
        {
            return new RecordingOrganizationService
            {
                OnExecute = request =>
                {
                    if (!(request is RetrieveMetadataChangesRequest) || tables == null)
                        throw new InvalidOperationException("Tables unavailable.");
                    var response = new RetrieveMetadataChangesResponse();
                    var collection = new EntityMetadataCollection();
                    foreach (var table in tables) collection.Add(table);
                    response.Results["EntityMetadata"] = collection;
                    return response;
                },
                OnRetrieveMultiple = query =>
                {
                    if (query.EntityName != "privilege" || privileges == null)
                        throw new InvalidOperationException("Privileges unavailable.");
                    return new EntityCollection(privileges.Select(name => new Entity("privilege", Guid.NewGuid()) { ["name"] = name }).ToList());
                }
            };
        }

        private static EntityMetadata Table(string logicalName, string displayName)
        {
            var table = new EntityMetadata { LogicalName = logicalName };
            if (displayName != null)
                table.DisplayName = new Microsoft.Xrm.Sdk.Label { UserLocalizedLabel = new LocalizedLabel(displayName, 1033) };
            return table;
        }

        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static T Invoke<T>(object target, string name, params object[] arguments)
        {
            return (T)target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
        }
    }
}

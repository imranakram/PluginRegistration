using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Label = System.Windows.Forms.Label;
using Xrm.Sdk.PluginRegistration.Forms;
using Xrm.Sdk.PluginRegistration.Helpers;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class CustomApiEditorFormTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void ChoiceLoadingAllowsClosingAndIgnoresCompletionAfterDisposal(bool fails, bool solutionDialog)
        {
            using (var started = new ManualResetEventSlim())
            using (var release = new ManualResetEventSlim())
            {
                var service = new RecordingOrganizationService
                {
                    OnRetrieveMultiple = _ =>
                    {
                        started.Set();
                        if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                        if (fails) throw new InvalidOperationException("Unavailable");
                        return new EntityCollection();
                    }
                };
                using (Form dialog = solutionDialog ? (Form)new CustomApiSolutionForm(service) : new CustomApiEditorForm(service, SavedApi()))
                {
                    var loading = Invoke<Task>(dialog, solutionDialog ? "LoadPublishers" : "LoadChoices");
                    try
                    {
                        Assert.That(started.Wait(TimeSpan.FromSeconds(5)), Is.True);
                        Assert.That(Field<Button>(dialog, "cancel").Enabled, Is.True);
                        var closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
                        Invoke<object>(dialog, "OnFormClosing", closing);
                        Assert.That(closing.Cancel, Is.False);
                        dialog.Dispose();
                    }
                    finally { release.Set(); }
                    PumpUntil(() => loading.IsCompleted);
                    loading.GetAwaiter().GetResult();
                }
            }
        }

        [Test]
        public void SolutionCreationBlocksClosingUntilTheWriteCompletes()
        {
            using (var release = new ManualResetEventSlim())
            {
                var solutionId = Guid.NewGuid();
                var service = new RecordingOrganizationService
                {
                    OnRetrieveMultiple = _ => new EntityCollection(new[] { new Entity("publisher", Guid.NewGuid())
                    {
                        ["friendlyname"] = "Example publisher", ["customizationprefix"] = "test"
                    } }),
                    OnExecute = _ =>
                    {
                        if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                        return new CreateResponse { Results = { ["id"] = solutionId } };
                    }
                };
                using (var dialog = new CustomApiSolutionForm(service))
                {
                    LoadChoices(dialog, "LoadPublishers");
                    Field<ComboBox>(dialog, "publisher").SelectedIndex = 0;
                    Field<TextBox>(dialog, "uniqueName").Text = "Example";
                    Field<TextBox>(dialog, "displayName").Text = "Example solution";
                    try
                    {
                        Invoke<object>(dialog, "CreateClicked", null, EventArgs.Empty);
                        Assert.That(Field<Button>(dialog, "cancel").Enabled, Is.False);
                        var closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
                        Invoke<object>(dialog, "OnFormClosing", closing);
                        Assert.That(closing.Cancel, Is.True);
                    }
                    finally { release.Set(); }
                    PumpUntil(() => !Field<bool>(dialog, "saving"));
                    Assert.That(dialog.CreatedSolution.Id, Is.EqualTo(solutionId));
                    Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.OK));
                    Assert.That(service.Requests.Count, Is.EqualTo(1));
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SavingReportsOnlyActualChangesAndBlocksClosingDuringTheWrite(bool changed)
        {
            using (var release = new ManualResetEventSlim())
            {
                var service = new RecordingOrganizationService
                {
                    OnRetrieveMultiple = _ => new EntityCollection(),
                    OnExecute = _ =>
                    {
                        if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
                        return new ExecuteTransactionResponse();
                    }
                };
                using (var dialog = new CustomApiEditorForm(service, SavedApi()))
                {
                    LoadChoices(dialog);
                    if (changed) Field<TextBox>(dialog, "displayName").Text = "Changed";
                    try
                    {
                        Invoke<object>(dialog, "SaveClicked", null, EventArgs.Empty);
                        if (changed)
                        {
                            Assert.That(Field<Button>(dialog, "cancel").Enabled, Is.False);
                            var closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
                            Invoke<object>(dialog, "OnFormClosing", closing);
                            Assert.That(closing.Cancel, Is.True);
                        }
                    }
                    finally { release.Set(); }
                    PumpUntil(() => !Field<bool>(dialog, "saving"));
                    Assert.That(dialog.Saved, Is.EqualTo(changed));
                    Assert.That(dialog.SavedApiId.HasValue, Is.EqualTo(changed));
                    Assert.That(dialog.SaveOutcomeUnknown, Is.False);
                    Assert.That(dialog.DialogResult, Is.EqualTo(changed ? DialogResult.OK : DialogResult.Cancel));
                    Assert.That(service.Requests.Count, Is.EqualTo(changed ? 1 : 0));
                }
            }
        }

        private static void PumpUntil(Func<bool> complete)
        {
            var timeout = Stopwatch.StartNew();
            while (!complete() && timeout.Elapsed < TimeSpan.FromSeconds(10))
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            Assert.That(complete(), Is.True, "Background work should complete.");
        }

        [Test]
        public void NewBoundApiSuggestsTablesAndExplainsGlobalState()
        {
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), null, new[] { "account", "contact" }))
            {
                var table = Field<TextBox>(dialog, "boundTable");
                var binding = Field<ComboBox>(dialog, "binding");
                Assert.That(table.AutoCompleteMode, Is.EqualTo(AutoCompleteMode.Suggest));
                Assert.That(table.AutoCompleteSource, Is.EqualTo(AutoCompleteSource.CustomSource));
                Assert.That(table.AutoCompleteCustomSource.Cast<string>(), Is.EqualTo(new[] { "account", "contact" }));
                Assert.That(table.Enabled, Is.False);
                Assert.That(Field<Label>(dialog, "boundTableHint").Text, Does.Contain("Global APIs have no bound table"));
                foreach (var type in new[] { CustomApiBindingType.Entity, CustomApiBindingType.EntityCollection })
                {
                    binding.SelectedItem = type;
                    Assert.That(table.Enabled && !table.ReadOnly, Is.True);
                    table.Text = "account";
                    Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").BoundEntityLogicalName, Is.EqualTo("account"));
                }
                binding.SelectedItem = CustomApiBindingType.Global;
                Assert.That(table.Text, Is.Empty);
            }
        }

        [Test]
        public void SavedBoundApiDisplaysItsTableAndExplainsWhyItIsFixed()
        {
            var original = SavedApi();
            original.BindingType = CustomApiBindingType.Entity;
            original.BoundEntityLogicalName = "account";
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), original))
            {
                Assert.That(Field<TextBox>(dialog, "boundTable").Text, Is.EqualTo("account"));
                Assert.That(Field<TextBox>(dialog, "boundTable").ReadOnly, Is.True);
                Assert.That(Field<Label>(dialog, "boundTableHint").Text, Does.Contain("fixed after"));
            }
        }

        [Test]
        public void LoadingChoicesRequiresExplicitSolutionAndPreservesItOnReload()
        {
            var service = new RecordingOrganizationService
            {
                OnRetrieveMultiple = query => query.EntityName == "solution"
                    ? new EntityCollection(new[] { new Entity("solution", Guid.NewGuid())
                    {
                        ["uniquename"] = "Example", ["friendlyname"] = "Example solution",
                        ["publisher.customizationprefix"] = new AliasedValue("publisher", "customizationprefix", "dx")
                    } }) : new EntityCollection()
            };
            using (var dialog = new CustomApiEditorForm(service))
            {
                LoadChoices(dialog);
                var solution = Field<ComboBox>(dialog, "solution");
                Assert.That(solution.Items.Count, Is.EqualTo(1));
                Assert.That(solution.SelectedIndex, Is.EqualTo(-1));
                Assert.That(Field<Label>(dialog, "uniqueNamePrefix").Text, Is.Empty);
                Assert.That(Field<TextBox>(dialog, "uniqueName").Enabled, Is.False);

                solution.SelectedIndex = 0;
                LoadChoices(dialog);
                Assert.That(((CustomApiSolutionOption)solution.SelectedItem).UniqueName, Is.EqualTo("Example"));
                Assert.That(Field<Label>(dialog, "uniqueNamePrefix").Text, Is.EqualTo("dx_"));

                service.OnRetrieveMultiple = query => new EntityCollection();
                LoadChoices(dialog);
                Assert.That(solution.SelectedIndex, Is.EqualTo(-1));
                Assert.That(Field<TextBox>(dialog, "uniqueName").Enabled, Is.False);
            }
        }

        private static void LoadChoices(object dialog, string method = "LoadChoices")
        {
            var task = Invoke<Task>(dialog, method);
            PumpUntil(() => task.IsCompleted);
            task.GetAwaiter().GetResult();
        }

        [Test]
        public void NewApiSuggestsAndClearsNamesWithAFixedSolutionPrefix()
        {
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService()))
            {
                var name = Field<TextBox>(dialog, "name");
                name.Text = "Review / Close_2!";
                SelectSolution(dialog, "dx");

                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo(name.Text));
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("dx_reviewclose_2"));
                name.Clear();
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.Empty);
                Assert.That(Field<TextBox>(dialog, "uniqueName").Text, Is.Empty);

                var uniqueName = Field<TextBox>(dialog, "uniqueName");
                uniqueName.SelectAll();
                uniqueName.SelectedText = "replacement";
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("dx_replacement"));
                uniqueName.Clear();
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("dx_"));
            }
        }

        [Test]
        public void NameSuggestionsPreserveEachManualOverrideIndependently()
        {
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService()))
            {
                SelectSolution(dialog, "dx");
                var name = Field<TextBox>(dialog, "name");
                name.Text = "First API";
                Field<TextBox>(dialog, "displayName").Text = "Custom label";
                name.Text = "Second API";
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo("Custom label"));
                Assert.That(Field<TextBox>(dialog, "uniqueName").Text, Is.EqualTo("secondapi"));
                Field<TextBox>(dialog, "uniqueName").Text = "CustomContract";
                name.Text = "Third API";
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo("Custom label"));
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("dx_CustomContract"));
            }
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService()))
            {
                SelectSolution(dialog, "dx");
                Field<TextBox>(dialog, "uniqueName").Text = "MyContract";
                Field<TextBox>(dialog, "name").Text = "Updated name";
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo("Updated name"));
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("dx_MyContract"));
            }
        }

        [Test]
        public void ChangingSolutionChangesOnlyTheFixedPrefix()
        {
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService()))
            {
                SelectSolution(dialog, "dx");
                Field<TextBox>(dialog, "name").Text = "First API";
                Field<TextBox>(dialog, "uniqueName").Text = "dx_customSuffix";
                SelectSolution(dialog, "other");

                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").UniqueName, Is.EqualTo("other_dx_customSuffix"));
            }
        }

        [Test]
        public void RenamingAnExistingApiPreservesItsOtherNames()
        {
            var original = SavedApi();
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), original))
            {
                Field<TextBox>(dialog, "name").Text = "Changed name";
                var candidate = Invoke<CustomApiDefinition>(dialog, "CaptureDraft");
                Assert.That(candidate.UniqueName, Is.EqualTo(original.UniqueName));
                Assert.That(candidate.DisplayName, Is.EqualTo(original.DisplayName));
            }
        }

        private static void SelectSolution(CustomApiEditorForm dialog, string prefix)
        {
            var solutions = Field<ComboBox>(dialog, "solution");
            solutions.Items.Add(new CustomApiSolutionOption { UniqueName = prefix + "Solution", PublisherPrefix = prefix });
            solutions.SelectedIndex = solutions.Items.Count - 1;
        }

        [TestCase(true, CustomApiParameterType.Entity, true)]
        [TestCase(true, CustomApiParameterType.EntityReference, true)]
        [TestCase(true, CustomApiParameterType.EntityCollection, true)]
        [TestCase(false, CustomApiParameterType.Entity, true)]
        [TestCase(false, CustomApiParameterType.EntityReference, true)]
        [TestCase(false, CustomApiParameterType.EntityCollection, false)]
        [TestCase(true, CustomApiParameterType.String, false)]
        [TestCase(false, CustomApiParameterType.String, false)]
        public void ChildTableEditingFollowsTheSelectedType(bool request, CustomApiParameterType selectedType, bool allowsTable)
        {
            CustomApiParameter child = request ? (CustomApiParameter)new CustomApiRequestParameter() : new CustomApiResponseProperty();
            using (var dialog = new CustomApiParameterForm(SavedApi(), null, child, new[] { "account" }))
            {
                var type = Field<ComboBox>(dialog, "type");
                var logicalName = Field<TextBox>(dialog, "logicalName");
                type.SelectedItem = CustomApiParameterType.Entity;
                logicalName.Text = "account";
                type.SelectedItem = selectedType;
                Assert.That(logicalName.Enabled, Is.EqualTo(allowsTable));
                Assert.That(logicalName.ReadOnly, Is.EqualTo(!allowsTable));
                Assert.That(logicalName.AutoCompleteCustomSource.Cast<string>(), Is.EqualTo(new[] { "account" }));
                Assert.That(logicalName.Text, Is.EqualTo(allowsTable ? "account" : ""));
                Assert.That(Field<Label>(dialog, "tableHint").Text,
                    Does.Contain(allowsTable ? "Leave blank for any table" : "does not support"));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ChildTableChoiceIsCapturedInTheDraftAndCanBeCleared(bool request)
        {
            CustomApiParameter child = request ? (CustomApiParameter)new CustomApiRequestParameter() : new CustomApiResponseProperty();
            child.Name = child.UniqueName = child.DisplayName = "TableInput";
            child.Type = CustomApiParameterType.EntityReference;
            foreach (var table in new[] { "account", "" })
            {
                using (var dialog = new CustomApiParameterForm(SavedApi(), null, child))
                {
                    Field<TextBox>(dialog, "logicalName").Text = table;
                    Invoke<object>(dialog, "AcceptClicked", null, EventArgs.Empty);
                    Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.OK));
                    Assert.That(dialog.Parameter.LogicalEntityName, Is.EqualTo(table));
                    Assert.That(child.LogicalEntityName, Is.Null, "Editing must not mutate the original child.");
                }
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SavedChildTablesRemainVisibleAndCannotBeChanged(bool request)
        {
            CustomApiParameter child = request ? (CustomApiParameter)new CustomApiRequestParameter() : new CustomApiResponseProperty();
            child.Id = Guid.NewGuid();
            child.Type = CustomApiParameterType.Entity;
            child.LogicalEntityName = "account";
            using (var dialog = new CustomApiParameterForm(SavedApi(), SavedApi(), child))
            {
                Assert.That(Field<TextBox>(dialog, "logicalName").Text, Is.EqualTo("account"));
                Assert.That(Field<TextBox>(dialog, "logicalName").ReadOnly, Is.True);
                Assert.That(Field<Label>(dialog, "tableHint").Text, Does.Contain("fixed after creation"));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NewChildSuggestsNamesAndPreservesIndependentOverrides(bool request)
        {
            CustomApiParameter child = request ? (CustomApiParameter)new CustomApiRequestParameter() : new CustomApiResponseProperty();
            using (var dialog = new CustomApiParameterForm(SavedApi(), null, child))
            {
                var name = Field<TextBox>(dialog, "name");
                var display = Field<TextBox>(dialog, "displayName");
                var unique = Field<TextBox>(dialog, "uniqueName");
                name.Text = "Review / Close_2!";
                Assert.That(display.Text, Is.EqualTo(name.Text));
                Assert.That(unique.Text, Is.EqualTo("reviewclose_2"));
                name.Clear();
                Assert.That(display.Text, Is.Empty);
                Assert.That(unique.Text, Is.Empty);
                unique.Text = "CustomContract";
                name.Text = "Second name";
                Assert.That(display.Text, Is.EqualTo(name.Text));
                Assert.That(unique.Text, Is.EqualTo("CustomContract"));
            }
            using (var dialog = new CustomApiParameterForm(SavedApi(), null, child))
            {
                Field<TextBox>(dialog, "displayName").Text = "Custom label";
                Field<TextBox>(dialog, "name").Text = "Third name";
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo("Custom label"));
                Assert.That(Field<TextBox>(dialog, "uniqueName").Text, Is.EqualTo("thirdname"));
            }
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void RenamingExistingChildPreservesSuppliedNames(bool request, bool saved)
        {
            CustomApiParameter child = request ? (CustomApiParameter)new CustomApiRequestParameter() : new CustomApiResponseProperty();
            child.Id = saved ? Guid.NewGuid() : Guid.Empty;
            child.Name = "Original";
            child.DisplayName = "Custom label";
            child.UniqueName = "CustomContract";
            using (var dialog = new CustomApiParameterForm(SavedApi(), null, child))
            {
                Field<TextBox>(dialog, "name").Text = "Changed name";
                Assert.That(Field<TextBox>(dialog, "displayName").Text, Is.EqualTo(child.DisplayName));
                Assert.That(Field<TextBox>(dialog, "uniqueName").Text, Is.EqualTo(child.UniqueName));
                Assert.That(child.Name, Is.EqualTo("Original"));
            }
        }

        [Test]
        public void EditingAParameterDoesNotTreatItsUnchangedNameAsADuplicate()
        {
            var original = SavedApi();
            var draft = original.Clone();
            var parameter = draft.RequestParameters[0];
            using (var dialog = new CustomApiParameterForm(draft, original, parameter))
            {
                var candidate = parameter.Clone();
                candidate.DisplayName = "Changed display name";
                var errors = Invoke<IReadOnlyList<string>>(dialog, "ValidateCandidate", candidate);
                Assert.That(errors, Is.Empty);
                Assert.That(parameter.DisplayName, Is.EqualTo("Input"));
            }
        }

        [Test]
        public void ANewParameterCannotReuseASiblingName()
        {
            var parent = SavedApi();
            var candidate = new CustomApiRequestParameter
            {
                UniqueName = "Input", Name = "Input", DisplayName = "Duplicate", Type = CustomApiParameterType.String
            };
            using (var dialog = new CustomApiParameterForm(parent, parent, candidate))
            {
                var errors = Invoke<IReadOnlyList<string>>(dialog, "ValidateCandidate", candidate);
                Assert.That(errors, Has.Some.Contains("already exists"));
            }
        }

        [Test]
        public void SavedContractsAreReadOnlyButMutableFieldsRemainEditable()
        {
            var original = SavedApi();
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), original))
            {
                Assert.That(Field<TextBox>(dialog, "uniqueName").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "boundTable").ReadOnly, Is.True);
                Assert.That(Field<ComboBox>(dialog, "binding").Enabled, Is.False);
                Assert.That(Field<ComboBox>(dialog, "processing").Enabled, Is.False);
                Assert.That(Field<CheckBox>(dialog, "function").Enabled, Is.False);
                Assert.That(Field<TextBox>(dialog, "displayName").ReadOnly, Is.False);
            }
            using (var dialog = new CustomApiParameterForm(original, original, original.RequestParameters[0]))
            {
                Assert.That(Field<TextBox>(dialog, "uniqueName").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "logicalName").ReadOnly, Is.True);
                Assert.That(Field<ComboBox>(dialog, "type").Enabled, Is.False);
                Assert.That(Field<CheckBox>(dialog, "optional").Enabled, Is.False);
                Assert.That(Field<TextBox>(dialog, "displayName").ReadOnly, Is.False);
            }
        }

        [Test]
        public void ProtectedManagedFieldsAreReadOnly()
        {
            var original = SavedApi();
            original.IsManaged = true;
            original.IsCustomizable = false;
            original.RequestParameters[0].IsManaged = true;
            original.RequestParameters[0].IsCustomizable = false;
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), original))
            {
                Assert.That(Field<TextBox>(dialog, "name").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "displayName").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "description").ReadOnly, Is.True);
                Assert.That(Field<ComboBox>(dialog, "pluginType").Enabled, Is.False);
                Assert.That(Field<CheckBox>(dialog, "isPrivate").Enabled, Is.False);
            }
            using (var dialog = new CustomApiParameterForm(original, original, original.RequestParameters[0]))
            {
                Assert.That(Field<TextBox>(dialog, "name").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "displayName").ReadOnly, Is.True);
                Assert.That(Field<TextBox>(dialog, "description").ReadOnly, Is.True);
            }
        }

        [Test]
        public void CancelingDraftEditsLeavesCallerSnapshotAndServerUntouched()
        {
            var original = SavedApi();
            var service = new RecordingOrganizationService();
            using (var dialog = new CustomApiEditorForm(service, original))
            {
                Field<TextBox>(dialog, "displayName").Text = "Discarded";
                var draft = Field<CustomApiDefinition>(dialog, "draft");
                draft.RequestParameters[0].DisplayName = "Discarded child edit";
                draft.ResponseProperties.Add(new CustomApiResponseProperty { UniqueName = "Discarded" });
                dialog.DialogResult = DialogResult.Cancel;
                Assert.That(dialog.Saved, Is.False);
                Assert.That(dialog.SavedApiId, Is.Null);
            }
            Assert.That(original.DisplayName, Is.EqualTo("Example API"));
            Assert.That(original.RequestParameters[0].DisplayName, Is.EqualTo("Input"));
            Assert.That(original.ResponseProperties, Is.Empty);
            Assert.That(service.Requests, Is.Empty);
            Assert.That(service.DeletedRecords, Is.Empty);
        }

        [Test]
        public void BrowsingAnotherAssemblyRetainsHandlerUntilExplicitlyChanged()
        {
            var original = SavedApi();
            Guid originalHandler = Guid.NewGuid();
            original.PluginTypeId = originalHandler;
            using (var dialog = new CustomApiEditorForm(new RecordingOrganizationService(), original))
            {
                var types = Field<List<CustomApiPluginTypeOption>>(dialog, "pluginTypes");
                types.Add(new CustomApiPluginTypeOption { Id = originalHandler, Name = "First.Plugin", AssemblyName = "First" });
                types.Add(new CustomApiPluginTypeOption { Id = Guid.NewGuid(), Name = "Second.Plugin", AssemblyName = "Second" });
                var assemblies = Field<ComboBox>(dialog, "assembly");
                assemblies.DataSource = new List<string> { "(All assemblies)", "First", "Second" };
                Invoke<object>(dialog, "PopulatePluginTypes", (Guid?)originalHandler);

                assemblies.SelectedIndex = 2;

                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.EqualTo(originalHandler));
                var handlers = Field<ComboBox>(dialog, "pluginType");
                Assert.That(handlers.GetItemText(handlers.SelectedItem), Does.Contain("another assembly"));
                handlers.SelectedIndex = 0; // None is an explicit choice, unlike changing the assembly filter.
                Assert.That(Invoke<CustomApiDefinition>(dialog, "CaptureDraft").PluginTypeId, Is.Null);
            }
        }

        private static CustomApiDefinition SavedApi()
        {
            var definition = new CustomApiDefinition
            {
                Id = Guid.NewGuid(), UniqueName = "test_Example", Name = "Example", DisplayName = "Example API",
                Description = "Example description", BindingType = CustomApiBindingType.Global
            };
            definition.RequestParameters.Add(new CustomApiRequestParameter
            {
                Id = Guid.NewGuid(), UniqueName = "Input", Name = "Input", DisplayName = "Input", Type = CustomApiParameterType.String
            });
            return definition;
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

namespace Xrm.Sdk.PluginRegistration.Tests.CustomApis
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Xrm.Sdk.PluginRegistration.Helpers;
    using Xrm.Sdk.PluginRegistration.Models;

    [TestFixture]
    public class CustomApiValidatorTests
    {
        [Test]
        public void NullDefinitionThrows()
        {
            Assert.Throws<ArgumentNullException>(() => CustomApiValidator.Validate(null));
        }

        [Test]
        public void PrivateActionWithoutHandlerOrChildrenIsValid()
        {
            var api = NewApi();
            api.IsPrivate = true;

            Assert.That(CustomApiValidator.Validate(api, publisherPrefix: "sample"), Is.Empty);
        }

        [TestCase("sample_DoWork", true)]
        [TestCase("other_DoWork", false)]
        [TestCase("sample_", false)]
        [TestCase("sample_Do-Work", false)]
        [TestCase("sample_DoWork\n", false)]
        public void CreationChecksUniqueNameAndPublisherPrefix(string uniqueName, bool valid)
        {
            var api = NewApi();
            api.UniqueName = uniqueName;

            Assert.That(CustomApiValidator.Validate(api, publisherPrefix: "sample").Count == 0, Is.EqualTo(valid));
        }

        [Test]
        public void UpdatingDoesNotRequireTheNewSolutionsPublisherPrefix()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var draft = NewApi();
            draft.Id = original.Id;

            Assert.That(CustomApiValidator.Validate(draft, original, "another"), Is.Empty);
        }

        [Test]
        public void RequiredApiTextFieldsAreReportedTogether()
        {
            var errors = CustomApiValidator.Validate(new CustomApiDefinition());

            Assert.That(errors.Where(e => e.Contains("is required")), Is.EquivalentTo(new[]
            {
                "Custom API name is required.", "Custom API unique name is required.",
                "Custom API display name is required.", "Custom API description is required."
            }));
        }

        [Test]
        public void TextLimitsRespectDifferentDescriptionColumns()
        {
            var api = NewApi();
            api.Name = new string('N', 100);
            api.DisplayName = new string('D', 100);
            api.UniqueName = "sample_" + new string('U', 121);
            api.Description = new string('D', 300);
            api.ExecutePrivilegeName = new string('P', 100);
            var input = NewRequest();
            input.Description = new string('D', 300);
            var output = NewResponse();
            output.Description = new string('D', 100);
            api.RequestParameters.Add(input);
            api.ResponseProperties.Add(output);
            Assert.That(CustomApiValidator.Validate(api), Is.Empty);

            api.Name += "N";
            api.DisplayName += "D";
            api.UniqueName += "U";
            api.Description += "D";
            api.ExecutePrivilegeName += "P";
            input.Description += "D";
            output.Description += "D";
            Assert.That(CustomApiValidator.Validate(api).Count, Is.EqualTo(7));
        }

        [TestCase(CustomApiBindingType.Global, null, true)]
        [TestCase(CustomApiBindingType.Global, "account", false)]
        [TestCase(CustomApiBindingType.Entity, null, false)]
        [TestCase(CustomApiBindingType.Entity, "account", true)]
        [TestCase(CustomApiBindingType.EntityCollection, null, false)]
        [TestCase(CustomApiBindingType.EntityCollection, "account", true)]
        public void BindingControlsWhetherTableNameIsRequired(CustomApiBindingType binding, string table, bool valid)
        {
            var api = NewApi();
            api.BindingType = binding;
            api.BoundEntityLogicalName = table;

            Assert.That(CustomApiValidator.Validate(api).Count == 0, Is.EqualTo(valid));
        }

        [Test]
        public void UnknownEnumsAndEmptyPluginIdAreRejected()
        {
            var api = NewApi();
            api.BindingType = (CustomApiBindingType)99;
            api.AllowedCustomProcessingStepType = (CustomApiProcessingStepType)99;
            api.PluginTypeId = Guid.Empty;
            var parameter = NewRequest();
            parameter.Type = (CustomApiParameterType)99;
            api.RequestParameters.Add(parameter);

            Assert.That(CustomApiValidator.Validate(api).Count, Is.EqualTo(4));
        }

        [Test]
        public void FunctionRequiresARealResponseProperty()
        {
            var api = NewApi();
            api.IsFunction = true;
            Assert.That(CustomApiValidator.Validate(api), Has.Some.Contains("at least one response"));
            api.ResponseProperties.Add(null);
            Assert.That(CustomApiValidator.Validate(api), Has.Some.Contains("at least one response"));
            api.ResponseProperties.Clear();
            api.ResponseProperties.Add(NewResponse());
            Assert.That(CustomApiValidator.Validate(api), Is.Empty);
        }

        [TestCase(CustomApiBindingType.Entity, false)]
        [TestCase(CustomApiBindingType.EntityCollection, true)]
        public void OnlyEntityBindingReservesTarget(CustomApiBindingType binding, bool valid)
        {
            var api = NewApi();
            api.BindingType = binding;
            api.BoundEntityLogicalName = "account";
            api.RequestParameters.Add(NewRequest("Target"));

            Assert.That(CustomApiValidator.Validate(api).Count == 0, Is.EqualTo(valid));
        }

        [Test]
        public void EntityCollectionTableConstraintIsAllowedOnlyOnRequests()
        {
            var api = NewApi();
            var request = NewRequest();
            request.Type = CustomApiParameterType.EntityCollection;
            request.LogicalEntityName = "account";
            api.RequestParameters.Add(request);
            Assert.That(CustomApiValidator.Validate(api), Is.Empty);

            var response = NewResponse();
            response.Type = CustomApiParameterType.EntityCollection;
            response.LogicalEntityName = "account";
            api.ResponseProperties.Add(response);
            Assert.That(CustomApiValidator.Validate(api), Has.Exactly(1).Contains("cannot specify a table"));
        }

        [Test]
        public void DuplicateNamesHaveSeparateRequestAndResponseScopes()
        {
            var api = NewApi();
            api.RequestParameters.Add(NewRequest("Value"));
            api.ResponseProperties.Add(NewResponse("Value"));
            Assert.That(CustomApiValidator.Validate(api), Is.Empty);

            api.RequestParameters.Add(NewRequest("value"));
            Assert.That(CustomApiValidator.Validate(api), Has.Exactly(1).Contains("duplicate unique name"));
        }

        [Test]
        public void ApiContractCannotChangeAfterCreation()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var draft = NewApi();
            draft.Id = original.Id;
            draft.UniqueName = "sample_Changed";
            draft.BindingType = CustomApiBindingType.Entity;
            draft.BoundEntityLogicalName = "account";
            draft.AllowedCustomProcessingStepType = CustomApiProcessingStepType.AsyncOnly;
            draft.IsFunction = true;
            draft.WorkflowSdkStepEnabled = true;
            draft.ResponseProperties.Add(NewResponse());

            Assert.That(CustomApiValidator.Validate(draft, original).Count(e => e.Contains("cannot be changed")), Is.EqualTo(6));
        }

        [Test]
        public void SavedRequestContractCannotChange()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var saved = NewRequest();
            saved.Id = Guid.NewGuid();
            original.RequestParameters.Add(saved);
            var draft = NewApi();
            draft.Id = original.Id;
            var changed = NewRequest("Changed");
            changed.Id = saved.Id;
            changed.Type = CustomApiParameterType.Entity;
            changed.LogicalEntityName = "account";
            changed.IsOptional = true;
            draft.RequestParameters.Add(changed);

            Assert.That(CustomApiValidator.Validate(draft, original).Count(e => e.Contains("cannot be changed")), Is.EqualTo(4));
        }

        [Test]
        public void NewRecordsCannotBorrowExistingIds()
        {
            var api = NewApi();
            api.Id = Guid.NewGuid();
            var request = NewRequest();
            request.Id = Guid.NewGuid();
            api.RequestParameters.Add(request);
            Assert.That(CustomApiValidator.Validate(api).Count, Is.EqualTo(2));

            var original = NewApi();
            original.Id = api.Id;
            Assert.That(CustomApiValidator.Validate(api, original), Has.Exactly(1).Contains("empty ID unless"));
            api.Id = Guid.NewGuid();
            Assert.That(CustomApiValidator.Validate(api, original), Has.Some.Contains("ID must match"));
        }

        [Test]
        public void DuplicateIdsAreRejectedAcrossRequestAndResponseLists()
        {
            var api = NewApi();
            var request = NewRequest();
            request.Id = Guid.NewGuid();
            var response = NewResponse();
            response.Id = request.Id;
            api.RequestParameters.Add(request);
            api.ResponseProperties.Add(response);

            Assert.That(CustomApiValidator.Validate(api), Has.Some.Contains("duplicate ID"));
        }

        [Test]
        public void IncompleteSavedSnapshotCannotBeUsedForUpdates()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            original.RequestParameters.Add(null);
            original.RequestParameters.Add(NewRequest());
            var request = NewRequest("Saved");
            request.Id = Guid.NewGuid();
            original.RequestParameters.Add(request);
            var response = NewResponse();
            response.Id = request.Id;
            original.ResponseProperties.Add(response);
            var draft = NewApi();
            draft.Id = original.Id;

            Assert.That(CustomApiValidator.Validate(draft, original).Count(e => e.Contains("Reload the Custom API")), Is.EqualTo(3));
        }

        [Test]
        public void ProtectedApiAllowsOptionalExtensionsAndIgnoresDraftManagedFlags()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            original.IsManaged = true;
            original.IsCustomizable = false;
            var draft = NewApi();
            draft.Id = original.Id;
            var request = NewRequest();
            request.IsOptional = true;
            draft.RequestParameters.Add(request);
            draft.ResponseProperties.Add(NewResponse());
            Assert.That(CustomApiValidator.Validate(draft, original), Is.Empty);

            request.IsOptional = false;
            Assert.That(CustomApiValidator.Validate(draft, original), Has.Some.Contains("must be optional"));
            draft.DisplayName = "Changed";
            Assert.That(CustomApiValidator.Validate(draft, original), Has.Some.Contains("does not allow customization"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ManagedChildrenCannotBeDeleted(bool customizable)
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var response = NewResponse();
            response.Id = Guid.NewGuid();
            response.IsManaged = true;
            response.IsCustomizable = customizable;
            original.ResponseProperties.Add(response);
            var draft = NewApi();
            draft.Id = original.Id;

            Assert.That(CustomApiValidator.Validate(draft, original), Has.Some.Contains("removed through its managed solution"));
        }

        [Test]
        public void ProtectedChildAllowsUnrelatedApiEditButRejectsItsOwnEdit()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var saved = NewResponse();
            saved.Id = Guid.NewGuid();
            saved.IsManaged = true;
            saved.IsCustomizable = false;
            original.ResponseProperties.Add(saved);
            var draft = NewApi();
            draft.Id = original.Id;
            draft.Name = "Changed API name";
            var response = NewResponse();
            response.Id = saved.Id;
            draft.ResponseProperties.Add(response);
            Assert.That(CustomApiValidator.Validate(draft, original), Is.Empty);

            response.DisplayName = "Changed response";
            Assert.That(CustomApiValidator.Validate(draft, original), Has.Some.Contains("does not allow customization"));
        }

        [Test]
        public void UnchangedBlankNamesOnSavedRecordsDoNotBlockUpdates()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            original.IsManaged = true;
            original.IsCustomizable = false;
            original.Name = null;
            original.DisplayName = null;
            var saved = NewResponse();
            saved.Id = Guid.NewGuid();
            saved.DisplayName = null;
            original.ResponseProperties.Add(saved);
            var draft = original.Clone();
            draft.Name = draft.DisplayName = string.Empty;
            var request = NewRequest();
            request.IsOptional = true;
            draft.RequestParameters.Add(request);
            Assert.That(CustomApiValidator.Validate(draft, original), Is.Empty);

            var added = NewResponse("Added");
            added.DisplayName = null;
            draft.ResponseProperties.Add(added);
            Assert.That(CustomApiValidator.Validate(draft, original),
                Is.EqualTo(new[] { "Response property 'Added' display name is required." }));
        }

        [Test]
        public void ClearingASavedNameIsStillRejected()
        {
            var original = NewApi();
            original.Id = Guid.NewGuid();
            var draft = original.Clone();
            draft.DisplayName = string.Empty;

            Assert.That(CustomApiValidator.Validate(draft, original), Is.EqualTo(new[] { "Custom API display name is required." }));
        }

        [TestCase(CustomApiParameterType.Entity)]
        [TestCase(CustomApiParameterType.EntityCollection)]
        [TestCase(CustomApiParameterType.StringArray)]
        [TestCase(CustomApiParameterType.EntityReference)]
        public void GlobalWorkflowRejectsUnsupportedParameterTypes(CustomApiParameterType type)
        {
            var api = NewApi();
            api.WorkflowSdkStepEnabled = true;
            var request = NewRequest();
            request.Type = type;
            api.RequestParameters.Add(request);

            Assert.That(CustomApiValidator.Validate(api), Has.Some.Contains("not supported by workflow"));
        }

        [Test]
        public void EntityBoundWorkflowAllowsEntityReferences()
        {
            var api = NewApi();
            api.WorkflowSdkStepEnabled = true;
            api.BindingType = CustomApiBindingType.Entity;
            api.BoundEntityLogicalName = "account";
            var response = NewResponse();
            response.Type = CustomApiParameterType.EntityReference;
            api.ResponseProperties.Add(response);

            Assert.That(CustomApiValidator.Validate(api), Is.Empty);
        }

        private static CustomApiDefinition NewApi()
        {
            return new CustomApiDefinition
            {
                Name = "sample_DoWork", UniqueName = "sample_DoWork", DisplayName = "Do work",
                Description = "Does the sample work."
            };
        }

        private static CustomApiRequestParameter NewRequest(string name = "Input")
        {
            return new CustomApiRequestParameter { Name = name, UniqueName = name, DisplayName = name, Type = CustomApiParameterType.String };
        }

        private static CustomApiResponseProperty NewResponse(string name = "Result")
        {
            return new CustomApiResponseProperty { Name = name, UniqueName = name, DisplayName = name, Type = CustomApiParameterType.String };
        }
    }
}

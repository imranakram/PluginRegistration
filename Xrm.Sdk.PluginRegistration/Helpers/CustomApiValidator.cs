namespace Xrm.Sdk.PluginRegistration.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Xrm.Sdk.PluginRegistration.Models;

    /// <summary>
    /// Validates a complete draft before persistence. The original must be the definition read
    /// from Dataverse, since its IDs and managed properties determine which edits are permitted.
    /// </summary>
    public static class CustomApiValidator
    {
        internal static readonly Regex Identifier = new Regex(@"\A[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> Validate(CustomApiDefinition definition,
            CustomApiDefinition original = null, string publisherPrefix = null)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var errors = new List<string>();
            ValidateNames(definition.Name, definition.UniqueName, definition.DisplayName, "Custom API", errors,
                original != null, original?.Name, original?.DisplayName);
            // API descriptions are SystemRequired; child descriptions are only ApplicationRequired.
            // Preserve an unchanged blank value from an existing API instead of blocking unrelated edits.
            if (original == null || !Same(definition.Description, original.Description))
            {
                ValidateRequired(definition.Description, "Custom API description", errors);
            }
            ValidateLength(definition.Description, 300, "Custom API description", errors);
            ValidateLength(definition.BoundEntityLogicalName, 100, "Bound table logical name", errors);
            ValidateLength(definition.ExecutePrivilegeName, 100, "Execute privilege name", errors);

            if (!Enum.IsDefined(typeof(CustomApiBindingType), definition.BindingType))
            {
                errors.Add("Select a valid binding type.");
            }
            if (!Enum.IsDefined(typeof(CustomApiProcessingStepType), definition.AllowedCustomProcessingStepType))
            {
                errors.Add("Select a valid allowed custom processing step type.");
            }
            if (definition.BindingType == CustomApiBindingType.Global)
            {
                if (!string.IsNullOrEmpty(definition.BoundEntityLogicalName))
                {
                    errors.Add("A global Custom API cannot specify a bound table logical name.");
                }
            }
            else if (definition.BindingType == CustomApiBindingType.Entity || definition.BindingType == CustomApiBindingType.EntityCollection)
            {
                ValidateRequired(definition.BoundEntityLogicalName, "Bound table logical name", errors);
            }

            if (definition.PluginTypeId == Guid.Empty)
            {
                errors.Add("The plug-in type ID must be a nonempty ID or null.");
            }
            if (definition.IsFunction && !definition.ResponseProperties.Any(p => p != null))
            {
                errors.Add("A function must have at least one response property.");
            }
            if (definition.WorkflowSdkStepEnabled && definition.IsFunction)
            {
                errors.Add("A Custom API enabled for workflow cannot be a function.");
            }

            if (original == null)
            {
                if (definition.Id != Guid.Empty)
                {
                    errors.Add("A new Custom API must have an empty ID.");
                }
                if (!string.IsNullOrEmpty(publisherPrefix) &&
                    (definition.UniqueName == null || !definition.UniqueName.StartsWith(publisherPrefix + "_", StringComparison.Ordinal) ||
                     definition.UniqueName.Length == publisherPrefix.Length + 1))
                {
                    errors.Add("Custom API unique name must start with the solution publisher prefix '" + publisherPrefix + "_' followed by a name.");
                }
            }
            else
            {
                ValidateOriginalParameters(original, errors);
                ValidateApiChanges(definition, original, errors);
            }

            // Requests and responses have separate name scopes, but an ID must identify only one draft row.
            var ids = new HashSet<Guid>();
            ValidateParameters(definition.RequestParameters, original?.RequestParameters, definition, original, "Request parameter", ids, errors);
            ValidateParameters(definition.ResponseProperties, original?.ResponseProperties, definition, original, "Response property", ids, errors);
            return errors.AsReadOnly();
        }

        private static void ValidateOriginalParameters(CustomApiDefinition original, List<string> errors)
        {
            var ids = new HashSet<Guid>();
            foreach (var parameter in original.RequestParameters.Cast<CustomApiParameter>().Concat(original.ResponseProperties))
            {
                // Snapshot IDs drive updates and deletes. An incomplete snapshot cannot be safely compared.
                if (parameter == null || parameter.Id == Guid.Empty)
                {
                    errors.Add("The saved definition contains a missing parameter or parameter ID. Reload the Custom API before saving.");
                }
                else if (!ids.Add(parameter.Id))
                {
                    errors.Add("The saved definition contains a duplicate parameter ID. Reload the Custom API before saving.");
                }
            }
        }

        private static void ValidateApiChanges(CustomApiDefinition definition, CustomApiDefinition original, List<string> errors)
        {
            if (original.Id == Guid.Empty || definition.Id != original.Id)
            {
                errors.Add("The Custom API ID must match the saved definition.");
            }

            // Dataverse fixes the message contract at creation, even in unmanaged solutions.
            ValidateImmutable(Same(definition.UniqueName, original.UniqueName), "Custom API unique name", errors);
            ValidateImmutable(definition.BindingType == original.BindingType, "Binding type", errors);
            ValidateImmutable(Same(definition.BoundEntityLogicalName, original.BoundEntityLogicalName), "Bound table logical name", errors);
            ValidateImmutable(definition.AllowedCustomProcessingStepType == original.AllowedCustomProcessingStepType, "Allowed custom processing step type", errors);
            ValidateImmutable(definition.IsFunction == original.IsFunction, "Is Function", errors);
            ValidateImmutable(definition.WorkflowSdkStepEnabled == original.WorkflowSdkStepEnabled, "Enabled for Workflow", errors);

            if (original.IsManaged && !original.IsCustomizable &&
                (!Same(definition.Name, original.Name) || !Same(definition.DisplayName, original.DisplayName) ||
                 !Same(definition.Description, original.Description) || !Same(definition.ExecutePrivilegeName, original.ExecutePrivilegeName) ||
                 definition.PluginTypeId != original.PluginTypeId || definition.IsPrivate != original.IsPrivate))
            {
                errors.Add("This managed Custom API does not allow customization.");
            }
        }

        private static void ValidateParameters<T>(IEnumerable<T> parameters, IEnumerable<T> originalParameters,
            CustomApiDefinition definition, CustomApiDefinition original, string kind, HashSet<Guid> ids, List<string> errors)
            where T : CustomApiParameter
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var savedParameters = (originalParameters ?? Enumerable.Empty<T>()).Where(p => p != null).ToList();
            var retainedIds = new HashSet<Guid>();
            foreach (var parameter in parameters)
            {
                if (parameter == null)
                {
                    errors.Add(kind + " list contains an empty entry.");
                    continue;
                }

                string label = kind + " '" + parameter.UniqueName + "'";
                T saved = parameter.Id == Guid.Empty ? null : savedParameters.FirstOrDefault(p => p.Id == parameter.Id);
                ValidateNames(parameter.Name, parameter.UniqueName, parameter.DisplayName, label, errors,
                    saved != null, saved?.Name, saved?.DisplayName);
                // The response table has a smaller description column than the API and request tables.
                ValidateLength(parameter.Description, parameter is CustomApiResponseProperty ? 100 : 300, label + " description", errors);
                ValidateLength(parameter.LogicalEntityName, 100, label + " table logical name", errors);
                if (!string.IsNullOrEmpty(parameter.UniqueName) && !names.Add(parameter.UniqueName))
                {
                    errors.Add(label + " has a duplicate unique name.");
                }
                if (!Enum.IsDefined(typeof(CustomApiParameterType), parameter.Type))
                {
                    errors.Add(label + " must have a valid type.");
                }

                ValidateParameterShape(parameter, definition, label, errors);
                if (parameter.Id != Guid.Empty)
                {
                    retainedIds.Add(parameter.Id);
                    if (!ids.Add(parameter.Id))
                    {
                        errors.Add(label + " has a duplicate ID.");
                    }
                    if (saved == null)
                    {
                        errors.Add(label + " must have an empty ID unless it belongs to the saved definition.");
                    }
                }
                if (saved != null)
                {
                    ValidateParameterChanges(parameter, saved, label, errors);
                }
                else if (original != null && original.IsManaged && !original.IsCustomizable &&
                    parameter is CustomApiRequestParameter request && !request.IsOptional)
                {
                    // Protected APIs may be extended without changing the contract existing callers use.
                    errors.Add(label + " must be optional when added to a managed Custom API that disallows customization.");
                }
            }

            foreach (var removed in savedParameters.Where(p => !retainedIds.Contains(p.Id) && p.IsManaged))
            {
                errors.Add(kind + " '" + removed.UniqueName + "' is managed and must be removed through its managed solution.");
            }
        }

        private static void ValidateParameterShape(CustomApiParameter parameter, CustomApiDefinition definition,
            string label, List<string> errors)
        {
            bool canSpecifyTable = parameter.Type == CustomApiParameterType.Entity ||
                parameter.Type == CustomApiParameterType.EntityReference ||
                (parameter is CustomApiRequestParameter && parameter.Type == CustomApiParameterType.EntityCollection);
            if (!canSpecifyTable && !string.IsNullOrEmpty(parameter.LogicalEntityName))
            {
                errors.Add(label + " cannot specify a table logical name for this type.");
            }
            if (parameter is CustomApiRequestParameter && definition.BindingType == CustomApiBindingType.Entity &&
                string.Equals(parameter.UniqueName, "Target", StringComparison.OrdinalIgnoreCase))
            {
                // Entity binding supplies Target automatically; collection binding does not.
                errors.Add(label + " conflicts with the implicit Target parameter for an entity-bound Custom API.");
            }
            if (definition.WorkflowSdkStepEnabled &&
                (parameter.Type == CustomApiParameterType.Entity || parameter.Type == CustomApiParameterType.EntityCollection ||
                 parameter.Type == CustomApiParameterType.StringArray ||
                 (parameter.Type == CustomApiParameterType.EntityReference && definition.BindingType != CustomApiBindingType.Entity)))
            {
                errors.Add(label + " has a type that is not supported by workflow for this binding.");
            }
        }

        private static void ValidateParameterChanges(CustomApiParameter parameter, CustomApiParameter saved,
            string label, List<string> errors)
        {
            ValidateImmutable(Same(parameter.UniqueName, saved.UniqueName), label + " unique name", errors);
            ValidateImmutable(parameter.Type == saved.Type, label + " type", errors);
            ValidateImmutable(Same(parameter.LogicalEntityName, saved.LogicalEntityName), label + " table logical name", errors);
            if (parameter is CustomApiRequestParameter request && saved is CustomApiRequestParameter savedRequest)
            {
                ValidateImmutable(request.IsOptional == savedRequest.IsOptional, label + " Is Optional", errors);
            }
            if (saved.IsManaged && !saved.IsCustomizable &&
                (!Same(parameter.Name, saved.Name) || !Same(parameter.DisplayName, saved.DisplayName) || !Same(parameter.Description, saved.Description)))
            {
                errors.Add(label + " is managed and does not allow customization.");
            }
        }

        private static void ValidateNames(string name, string uniqueName, string displayName, string label, List<string> errors,
            bool saved = false, string savedName = null, string savedDisplayName = null)
        {
            // Records created elsewhere may lack these names, and protected managed records make them read-only.
            // Updates write only changed fields, so an unchanged blank value must not block unrelated edits.
            if (!saved || !Same(name, savedName)) ValidateRequired(name, label + " name", errors);
            ValidateRequired(uniqueName, label + " unique name", errors);
            if (!saved || !Same(displayName, savedDisplayName)) ValidateRequired(displayName, label + " display name", errors);
            ValidateLength(name, 100, label + " name", errors);
            ValidateLength(uniqueName, 128, label + " unique name", errors);
            ValidateLength(displayName, 100, label + " display name", errors);
            if (!string.IsNullOrWhiteSpace(uniqueName) && !Identifier.IsMatch(uniqueName))
            {
                errors.Add(label + " unique name must start with a letter or underscore and contain only letters, digits, and underscores.");
            }
        }

        private static void ValidateRequired(string value, string label, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(label + " is required.");
            }
        }

        private static void ValidateLength(string value, int maximum, string label, List<string> errors)
        {
            if (value != null && value.Length > maximum)
            {
                errors.Add(label + " cannot exceed " + maximum + " characters.");
            }
        }

        private static void ValidateImmutable(bool unchanged, string label, List<string> errors)
        {
            if (!unchanged)
            {
                errors.Add(label + " cannot be changed after creation.");
            }
        }

        private static bool Same(string first, string second)
        {
            // Optional text is represented as null by Dataverse and as empty text by an editor.
            return string.Equals(first ?? string.Empty, second ?? string.Empty, StringComparison.Ordinal);
        }
    }
}

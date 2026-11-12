using Microsoft.Xrm.Sdk;
using System;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>
    /// Maps the designer's supported fields. Keeping this list explicit prevents an edit
    /// from writing generated message references or overwriting unexposed managed properties.
    /// </summary>
    internal static class CustomApiMapping
    {
        internal const string ApiTable = "customapi";
        internal const string RequestTable = "customapirequestparameter";
        internal const string ResponseTable = "customapiresponseproperty";
        internal const string RequestRelationship = "customapi_customapirequestparameter";
        internal const string ResponseRelationship = "customapi_customapiresponseproperty";

        internal static readonly string[] ApiColumns =
        {
            "customapiid", "name", "uniquename", "displayname", "description", "bindingtype",
            "boundentitylogicalname", "allowedcustomprocessingsteptype", "plugintypeid",
            "sdkmessageid", "executeprivilegename", "isfunction", "isprivate",
            "workflowsdkstepenabled", "ismanaged", "iscustomizable"
        };

        internal static readonly string[] RequestColumns =
        {
            "customapirequestparameterid", "customapiid", "name", "uniquename", "displayname",
            "description", "type", "logicalentityname", "isoptional", "ismanaged", "iscustomizable"
        };

        internal static readonly string[] ResponseColumns =
        {
            "customapiresponsepropertyid", "customapiid", "name", "uniquename", "displayname",
            "description", "type", "logicalentityname", "ismanaged", "iscustomizable"
        };

        internal static CustomApiDefinition ReadApi(Entity entity)
        {
            return new CustomApiDefinition
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>("name"),
                UniqueName = entity.GetAttributeValue<string>("uniquename"),
                DisplayName = entity.GetAttributeValue<string>("displayname"),
                Description = entity.GetAttributeValue<string>("description"),
                BindingType = (CustomApiBindingType)ReadChoice(entity, "bindingtype"),
                BoundEntityLogicalName = entity.GetAttributeValue<string>("boundentitylogicalname"),
                AllowedCustomProcessingStepType = (CustomApiProcessingStepType)ReadChoice(entity, "allowedcustomprocessingsteptype"),
                PluginTypeId = entity.GetAttributeValue<EntityReference>("plugintypeid")?.Id,
                SdkMessageId = entity.GetAttributeValue<EntityReference>("sdkmessageid")?.Id,
                ExecutePrivilegeName = entity.GetAttributeValue<string>("executeprivilegename"),
                IsFunction = entity.GetAttributeValue<bool>("isfunction"),
                IsPrivate = entity.GetAttributeValue<bool>("isprivate"),
                WorkflowSdkStepEnabled = entity.GetAttributeValue<bool>("workflowsdkstepenabled"),
                IsManaged = entity.GetAttributeValue<bool>("ismanaged"),
                IsCustomizable = entity.GetAttributeValue<BooleanManagedProperty>("iscustomizable")?.Value ?? true
            };
        }

        internal static CustomApiRequestParameter ReadRequest(Entity entity)
        {
            var parameter = new CustomApiRequestParameter
            {
                IsOptional = entity.GetAttributeValue<bool>("isoptional")
            };
            ReadParameter(entity, parameter);
            return parameter;
        }

        internal static CustomApiResponseProperty ReadResponse(Entity entity)
        {
            var property = new CustomApiResponseProperty();
            ReadParameter(entity, property);
            return property;
        }

        internal static Entity CreateApi(CustomApiDefinition definition)
        {
            var entity = MutableApi(definition);
            entity["uniquename"] = definition.UniqueName;
            entity["bindingtype"] = new OptionSetValue((int)definition.BindingType);
            entity["boundentitylogicalname"] = OptionalText(definition.BoundEntityLogicalName);
            entity["allowedcustomprocessingsteptype"] = new OptionSetValue((int)definition.AllowedCustomProcessingStepType);
            entity["isfunction"] = definition.IsFunction;
            entity["workflowsdkstepenabled"] = definition.WorkflowSdkStepEnabled;
            return entity;
        }

        internal static Entity MutableApi(CustomApiDefinition definition)
        {
            return new Entity(ApiTable, definition.Id)
            {
                ["name"] = definition.Name,
                ["displayname"] = definition.DisplayName,
                ["description"] = OptionalText(definition.Description),
                ["executeprivilegename"] = OptionalText(definition.ExecutePrivilegeName),
                ["isprivate"] = definition.IsPrivate,
                ["plugintypeid"] = definition.PluginTypeId.HasValue
                    ? new EntityReference("plugintype", definition.PluginTypeId.Value)
                    : null
            };
        }

        internal static Entity CreateParameter(CustomApiParameter parameter, string table)
        {
            var entity = MutableParameter(parameter, table);
            entity["uniquename"] = parameter.UniqueName;
            entity["type"] = new OptionSetValue((int)parameter.Type);
            entity["logicalentityname"] = OptionalText(parameter.LogicalEntityName);
            if (parameter is CustomApiRequestParameter request)
            {
                entity["isoptional"] = request.IsOptional;
            }
            return entity;
        }

        internal static Entity MutableParameter(CustomApiParameter parameter, string table)
        {
            return new Entity(table, parameter.Id)
            {
                ["name"] = parameter.Name,
                ["displayname"] = parameter.DisplayName,
                ["description"] = OptionalText(parameter.Description)
            };
        }

        internal static Entity Changes(Entity original, Entity updated)
        {
            var changes = new Entity(updated.LogicalName, updated.Id);
            foreach (var attribute in updated.Attributes)
            {
                original.Attributes.TryGetValue(attribute.Key, out var previous);
                if (!SameValue(previous, attribute.Value))
                {
                    // A null attribute is intentional: omitting it would leave the old value in Dataverse.
                    changes[attribute.Key] = attribute.Value;
                }
            }
            return changes;
        }

        private static bool SameValue(object left, object right)
        {
            if (left is EntityReference leftReference && right is EntityReference rightReference)
            {
                return leftReference.Id == rightReference.Id && leftReference.LogicalName == rightReference.LogicalName;
            }
            if (left is string || right is string)
            {
                // Match the validator: Dataverse stores blank text as null, while editors return empty text.
                return string.Equals(left as string ?? string.Empty, right as string ?? string.Empty, StringComparison.Ordinal);
            }
            return Equals(left, right);
        }

        private static void ReadParameter(Entity entity, CustomApiParameter parameter)
        {
            parameter.Id = entity.Id;
            parameter.Name = entity.GetAttributeValue<string>("name");
            parameter.UniqueName = entity.GetAttributeValue<string>("uniquename");
            parameter.DisplayName = entity.GetAttributeValue<string>("displayname");
            parameter.Description = entity.GetAttributeValue<string>("description");
            parameter.Type = (CustomApiParameterType)ReadChoice(entity, "type");
            parameter.LogicalEntityName = entity.GetAttributeValue<string>("logicalentityname");
            parameter.IsManaged = entity.GetAttributeValue<bool>("ismanaged");
            parameter.IsCustomizable = entity.GetAttributeValue<BooleanManagedProperty>("iscustomizable")?.Value ?? true;
        }

        private static int ReadChoice(Entity entity, string attribute)
        {
            // An absent choice must not silently become Boolean/Global/None and change the contract on save.
            return entity.GetAttributeValue<OptionSetValue>(attribute)?.Value ?? -1;
        }

        private static string OptionalText(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}

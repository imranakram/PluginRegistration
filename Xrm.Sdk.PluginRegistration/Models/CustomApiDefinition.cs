namespace Xrm.Sdk.PluginRegistration.Models
{
    using System;
    using System.Collections.Generic;

    // These values match the Dataverse option sets used by the three Custom API tables.
    public enum CustomApiBindingType
    {
        Global = 0,
        Entity = 1,
        EntityCollection = 2
    }

    public enum CustomApiProcessingStepType
    {
        None = 0,
        AsyncOnly = 1,
        SyncAndAsync = 2
    }

    public enum CustomApiParameterType
    {
        Boolean = 0,
        DateTime = 1,
        Decimal = 2,
        Entity = 3,
        EntityCollection = 4,
        EntityReference = 5,
        Float = 6,
        Integer = 7,
        Money = 8,
        Picklist = 9,
        String = 10,
        StringArray = 11,
        Guid = 12
    }

    /// <summary>
    /// A complete API definition or an editable draft. Empty record IDs identify new records.
    /// </summary>
    public class CustomApiDefinition
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public CustomApiBindingType BindingType { get; set; }
        public string BoundEntityLogicalName { get; set; }
        public CustomApiProcessingStepType AllowedCustomProcessingStepType { get; set; }
        public string ExecutePrivilegeName { get; set; }
        public Guid? PluginTypeId { get; set; }

        // The generated message has its own identity; it is not the Custom API record ID.
        public Guid? SdkMessageId { get; set; }

        public bool IsFunction { get; set; }
        public bool IsPrivate { get; set; }
        public bool WorkflowSdkStepEnabled { get; set; }
        public bool IsManaged { get; set; }
        public bool IsCustomizable { get; set; } = true;
        public List<CustomApiRequestParameter> RequestParameters { get; } = new List<CustomApiRequestParameter>();
        public List<CustomApiResponseProperty> ResponseProperties { get; } = new List<CustomApiResponseProperty>();

        /// <summary>Copies a server snapshot so canceling an editor never changes the original definition.</summary>
        public CustomApiDefinition Clone()
        {
            var copy = new CustomApiDefinition
            {
                Id = Id, Name = Name, UniqueName = UniqueName, DisplayName = DisplayName,
                Description = Description, BindingType = BindingType, BoundEntityLogicalName = BoundEntityLogicalName,
                AllowedCustomProcessingStepType = AllowedCustomProcessingStepType,
                ExecutePrivilegeName = ExecutePrivilegeName, PluginTypeId = PluginTypeId, SdkMessageId = SdkMessageId,
                IsFunction = IsFunction, IsPrivate = IsPrivate, WorkflowSdkStepEnabled = WorkflowSdkStepEnabled,
                IsManaged = IsManaged, IsCustomizable = IsCustomizable
            };
            copy.RequestParameters.AddRange(RequestParameters.ConvertAll(item => (CustomApiRequestParameter)item.Clone()));
            copy.ResponseProperties.AddRange(ResponseProperties.ConvertAll(item => (CustomApiResponseProperty)item.Clone()));
            return copy;
        }
    }

    public abstract class CustomApiParameter
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string LogicalEntityName { get; set; }
        public CustomApiParameterType Type { get; set; }
        public bool IsManaged { get; set; }
        public bool IsCustomizable { get; set; } = true;

        // All parameter fields are scalar values; MemberwiseClone also preserves request optionality.
        public CustomApiParameter Clone() => (CustomApiParameter)MemberwiseClone();
    }

    public class CustomApiRequestParameter : CustomApiParameter
    {
        public bool IsOptional { get; set; }
    }

    public class CustomApiResponseProperty : CustomApiParameter
    {
    }

    /// <summary>
    /// List data that does not require retrieving an API's request and response definitions.
    /// </summary>
    public class CustomApiSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public Guid? SdkMessageId { get; set; }
        public bool IsManaged { get; set; }
    }
}

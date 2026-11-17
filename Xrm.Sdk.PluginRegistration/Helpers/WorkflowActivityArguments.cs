using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Xrm.Sdk.PluginRegistration.Wrappers;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    public enum ActivityArgumentDirection
    {
        Input,
        Output
    }

    /// <summary>An input or output argument of a custom workflow activity.</summary>
    [Serializable]
    public sealed class ActivityArgument
    {
        /// <summary>The property in code, which processes refer to.</summary>
        public string PropertyName { get; set; }

        /// <summary>The name the process designer shows.</summary>
        public string Label { get; set; }

        public ActivityArgumentDirection Direction { get; set; }

        /// <summary>Dataverse's name for the type, such as Microsoft.Crm.Sdk.CrmNumber; null when not known.</summary>
        public string TypeName { get; set; }

        public bool Required { get; set; }
    }

    /// <summary>A change to an activity's arguments that can break the processes using it.</summary>
    public sealed class ActivityArgumentChange
    {
        public ActivityArgumentChange(string activity, string description)
        {
            Activity = activity;
            Description = description;
        }

        public string Activity { get; }

        public string Description { get; }
    }

    /// <summary>
    /// Reads a custom workflow activity's arguments from a build and from Dataverse, and finds the changes
    /// between them that existing processes may not survive: a removed argument, a changed type, or an
    /// input that has to be set now. Added optional arguments are safe and not reported.
    /// </summary>
    public static class WorkflowActivityArguments
    {
        /// <summary>Microsoft's advice on changing custom workflow activities.</summary>
        public const string DocsUrl =
            "https://learn.microsoft.com/power-apps/developer/data-platform/workflow/workflow-extensions#manage-changes-to-custom-workflow-activities";

        // The .NET types an argument can have, with the name Dataverse stores for each and a short name to show
        private static readonly (string Clr, string Dataverse, string Shown)[] Types =
        {
            ("System.String", "System.String", "string"),
            ("System.Int32", "Microsoft.Crm.Sdk.CrmNumber", "int"),
            ("System.Boolean", "Microsoft.Crm.Sdk.CrmBoolean", "bool"),
            ("System.DateTime", "Microsoft.Crm.Sdk.CrmDateTime", "DateTime"),
            ("System.Decimal", "Microsoft.Crm.Sdk.CrmDecimal", "decimal"),
            ("System.Double", "Microsoft.Crm.Sdk.CrmFloat", "double"),
            ("Microsoft.Xrm.Sdk.Money", "Microsoft.Crm.Sdk.CrmMoney", "Money"),
            ("Microsoft.Xrm.Sdk.EntityReference", "Microsoft.Crm.Sdk.Lookup", "EntityReference"),
            ("Microsoft.Xrm.Sdk.OptionSetValue", "Microsoft.Crm.Sdk.Picklist", "OptionSetValue"),
        };

        /// <summary>Dataverse's name for an argument of the .NET type, or null for a type it doesn't take.</summary>
        public static string DataverseTypeName(string clrTypeName) =>
            Types.FirstOrDefault(t => t.Clr == clrTypeName).Dataverse;

        private static string Shown(string dataverseTypeName) =>
            Types.FirstOrDefault(t => t.Dataverse == dataverseTypeName).Shown ?? dataverseTypeName;

        /// <summary>
        /// The arguments of an activity class, from its InArgument, OutArgument and InOutArgument properties
        /// and their Input, Output and RequiredArgument attributes. Attributes are matched by name, so a
        /// build against any SDK version is read.
        /// </summary>
        public static List<ActivityArgument> Read(Type activity)
        {
            var arguments = new List<ActivityArgument>();
            foreach (var property in activity.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var type = property.PropertyType;
                if (!type.IsGenericType) continue;
                string kind = type.GetGenericTypeDefinition().FullName;
                if (kind != "System.Activities.InArgument`1" && kind != "System.Activities.OutArgument`1" &&
                    kind != "System.Activities.InOutArgument`1")
                {
                    continue;
                }

                var attributes = property.GetCustomAttributesData();
                string input = AttributeName(attributes, "Microsoft.Xrm.Sdk.Workflow.InputAttribute");
                string output = AttributeName(attributes, "Microsoft.Xrm.Sdk.Workflow.OutputAttribute");
                bool required = attributes.Any(a => a.AttributeType.FullName == "System.Activities.RequiredArgumentAttribute");
                string typeName = DataverseTypeName(type.GetGenericArguments()[0].FullName);

                if (input != null)
                {
                    arguments.Add(new ActivityArgument { PropertyName = property.Name, Label = input, Direction = ActivityArgumentDirection.Input, TypeName = typeName, Required = required });
                }

                if (output != null)
                {
                    arguments.Add(new ActivityArgument { PropertyName = property.Name, Label = output, Direction = ActivityArgumentDirection.Output, TypeName = typeName });
                }
            }

            return arguments;
        }

        private static string AttributeName(IList<CustomAttributeData> attributes, string attributeType)
        {
            var attribute = attributes.FirstOrDefault(a => a.AttributeType.FullName == attributeType);
            if (attribute == null) return null;
            return attribute.ConstructorArguments.Count > 0 ? attribute.ConstructorArguments[0].Value as string ?? string.Empty : string.Empty;
        }

        /// <summary>The arguments Dataverse has for an activity, from its plugintype.customworkflowactivityinfo.</summary>
        public static List<ActivityArgument> Parse(string customWorkflowActivityInfo)
        {
            var arguments = new List<ActivityArgument>();
            if (string.IsNullOrWhiteSpace(customWorkflowActivityInfo)) return arguments;

            var document = XDocument.Parse(customWorkflowActivityInfo);
            foreach (var (list, direction) in new[] { ("Inputs", ActivityArgumentDirection.Input), ("Outputs", ActivityArgumentDirection.Output) })
            {
                foreach (var parameter in document.Descendants(list).Elements("CustomActivityParameterInfo"))
                {
                    string typeName = (string)parameter.Element("TypeName");
                    arguments.Add(new ActivityArgument
                    {
                        PropertyName = (string)parameter.Element("DependencyPropertyName") ?? (string)parameter.Element("Name"),
                        Label = (string)parameter.Element("Name"),
                        Direction = direction,
                        TypeName = typeName?.Split(',')[0].Trim(),
                        Required = string.Equals((string)parameter.Element("Required"), "true", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }

            return arguments;
        }

        /// <summary>The arguments of each workflow activity registered in the assembly, by type name.</summary>
        public static Dictionary<string, List<ActivityArgument>> LoadRegistered(IOrganizationService service, Guid assemblyId)
        {
            var query = new QueryExpression("plugintype") { ColumnSet = new ColumnSet("typename", "customworkflowactivityinfo") };
            query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);
            query.Criteria.AddCondition("isworkflowactivity", ConditionOperator.Equal, true);
            return service.RetrieveMultiple(query).Entities
                .Where(type => type.GetAttributeValue<string>("typename") != null)
                .ToDictionary(type => type.GetAttributeValue<string>("typename"),
                    type => Parse(type.GetAttributeValue<string>("customworkflowactivityinfo")), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The changes from the registered activities to the build's that existing processes may not
        /// survive. Activities that are new, or whose registered arguments aren't known, have none.
        /// </summary>
        public static List<ActivityArgumentChange> RiskyChanges(IDictionary<string, List<ActivityArgument>> registered, IEnumerable<CrmPlugin> build)
        {
            var changes = new List<ActivityArgumentChange>();
            foreach (var activity in (build ?? Enumerable.Empty<CrmPlugin>()).Where(p => p.PluginType == CrmPluginType.WorkflowActivity && p.ActivityArguments != null))
            {
                if (registered == null || !registered.TryGetValue(activity.TypeName, out var before) || before.Count == 0 && activity.ActivityArguments.Count == 0)
                {
                    continue;
                }

                string name = activity.TypeName;
                foreach (var old in before)
                {
                    var now = activity.ActivityArguments.FirstOrDefault(a => Same(a, old));
                    if (now == null)
                    {
                        changes.Add(new ActivityArgumentChange(name, $"{Describe(old)} is removed"));
                    }
                    else if (old.TypeName != null && now.TypeName != null && old.TypeName != now.TypeName)
                    {
                        changes.Add(new ActivityArgumentChange(name, $"{Describe(old)} changes from {Shown(old.TypeName)} to {Shown(now.TypeName)}"));
                    }
                    else if (now.Direction == ActivityArgumentDirection.Input && now.Required && !old.Required)
                    {
                        changes.Add(new ActivityArgumentChange(name, $"{Describe(now)} is now required"));
                    }
                }

                foreach (var added in activity.ActivityArguments.Where(a => a.Direction == ActivityArgumentDirection.Input && a.Required && !before.Any(old => Same(a, old))))
                {
                    changes.Add(new ActivityArgumentChange(name, $"{Describe(added)} is new and required"));
                }
            }

            return changes;
        }

        private static bool Same(ActivityArgument a, ActivityArgument b) =>
            a.Direction == b.Direction && string.Equals(a.PropertyName, b.PropertyName, StringComparison.Ordinal);

        private static string Describe(ActivityArgument argument)
        {
            string kind = argument.Direction == ActivityArgumentDirection.Input ? "Input" : "Output";
            return string.IsNullOrEmpty(argument.Label) || argument.Label == argument.PropertyName
                ? $"{kind} {argument.PropertyName}"
                : $"{kind} \"{argument.Label}\" ({argument.PropertyName})";
        }

        /// <summary>The warning shown before an update with risky changes, listed by activity.</summary>
        public static string Warning(IEnumerable<ActivityArgumentChange> changes)
        {
            var text = new StringBuilder("This update changes workflow activity arguments that existing processes may use:");
            text.AppendLine();
            foreach (var activity in changes.GroupBy(c => c.Activity))
            {
                text.AppendLine().AppendLine(activity.Key);
                foreach (var change in activity) text.AppendLine("- " + change.Description);
            }

            text.AppendLine().Append("Processes that use these arguments may fail until they're edited. ")
                .Append("For such changes Microsoft recommends a new major or minor version, registered as a separate assembly; Help opens its guidance.")
                .AppendLine().AppendLine().Append("Update anyway?");
            return text.ToString();
        }
    }
}

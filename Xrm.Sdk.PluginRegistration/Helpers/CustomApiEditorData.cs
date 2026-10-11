using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>Loads editor choices independently of the assembly tree's user-selected filters.</summary>
    public sealed class CustomApiEditorData
    {
        public IReadOnlyList<CustomApiSolutionOption> Solutions { get; private set; }
        public IReadOnlyList<CustomApiPluginTypeOption> PluginTypes { get; private set; }

        public static CustomApiEditorData Load(IOrganizationService service, Guid? currentPluginTypeId, bool includeSolutions = true)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var solutionOptions = includeSolutions ? LoadSolutions(service) : new List<CustomApiSolutionOption>();

            var plugins = Query("plugintype", "typename", "name", "pluginassemblyid");
            plugins.Criteria.FilterOperator = LogicalOperator.Or;
            var eligible = plugins.Criteria.AddFilter(LogicalOperator.And);
            eligible.AddCondition("isworkflowactivity", ConditionOperator.Equal, false);
            eligible.AddCondition("pluginassemblyid", ConditionOperator.NotNull);
            var custom = eligible.AddFilter(LogicalOperator.Or);
            custom.AddCondition("customizationlevel", ConditionOperator.Null);
            custom.AddCondition("customizationlevel", ConditionOperator.NotEqual, 0);
            if (currentPluginTypeId.HasValue)
                plugins.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, currentPluginTypeId.Value);
            var assembly = plugins.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid", JoinOperator.LeftOuter);
            assembly.EntityAlias = "assembly";
            assembly.Columns = new ColumnSet("name");
            plugins.AddOrder("typename", OrderType.Ascending);
            var pluginOptions = service.RetrieveMultipleAllPages(plugins).Entities.Select(entity => new CustomApiPluginTypeOption
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>("typename") ?? entity.GetAttributeValue<string>("name") ?? entity.Id.ToString(),
                AssemblyName = entity.GetAttributeValue<AliasedValue>("assembly.name")?.Value as string ?? "Unknown assembly"
            }).ToList();
            if (currentPluginTypeId.HasValue && pluginOptions.All(option => option.Id != currentPluginTypeId.Value))
            {
                // Retain an inaccessible current reference. Choosing another handler or None is explicit.
                pluginOptions.Add(new CustomApiPluginTypeOption
                {
                    Id = currentPluginTypeId.Value, Name = "Current handler (" + currentPluginTypeId.Value + ")",
                    AssemblyName = "Unavailable assembly"
                });
            }
            return new CustomApiEditorData { Solutions = solutionOptions, PluginTypes = pluginOptions };
        }

        private static List<CustomApiSolutionOption> LoadSolutions(IOrganizationService service)
        {
            var solutions = Query("solution", "uniquename", "friendlyname");
            solutions.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
            solutions.Criteria.AddCondition("isvisible", ConditionOperator.Equal, true);
            solutions.Criteria.AddCondition("uniquename", ConditionOperator.NotEqual, "Default");
            var publisher = solutions.AddLink("publisher", "publisherid", "publisherid");
            publisher.EntityAlias = "publisher";
            publisher.Columns = new ColumnSet("customizationprefix");
            solutions.AddOrder("friendlyname", OrderType.Ascending);
            return service.RetrieveMultipleAllPages(solutions).Entities.Select(entity => new CustomApiSolutionOption
            {
                Id = entity.Id, UniqueName = entity.GetAttributeValue<string>("uniquename"),
                Name = entity.GetAttributeValue<string>("friendlyname"),
                PublisherPrefix = entity.GetAttributeValue<AliasedValue>("publisher.customizationprefix")?.Value as string
            }).Where(option => !string.IsNullOrWhiteSpace(option.PublisherPrefix)).ToList();
        }

        public static IReadOnlyList<CustomApiPublisherOption> LoadPublishers(IOrganizationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var query = Query("publisher", "friendlyname", "customizationprefix");
            query.AddOrder("friendlyname", OrderType.Ascending);
            return service.RetrieveMultipleAllPages(query).Entities.Select(entity => new CustomApiPublisherOption
            {
                Id = entity.Id, Name = entity.GetAttributeValue<string>("friendlyname"),
                Prefix = entity.GetAttributeValue<string>("customizationprefix")
            }).Where(option => !string.IsNullOrWhiteSpace(option.Prefix)).ToList();
        }

        public static CustomApiSolutionOption CreateSolution(IOrganizationService service, string uniqueName,
            string displayName, string version, CustomApiPublisherOption publisher)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (string.IsNullOrWhiteSpace(uniqueName) || uniqueName.Length > 65 ||
                !CustomApiValidator.Identifier.IsMatch(uniqueName))
                throw new ArgumentException("Solution unique name must start with a letter or underscore and contain only letters, digits, or underscores (65 characters maximum).");
            if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 256)
                throw new ArgumentException("Enter a solution display name of at most 256 characters.");
            if (!Version.TryParse(version, out var parsed) || parsed.Build < 0 || parsed.Revision < 0)
                throw new ArgumentException("Enter a four-part solution version, for example 1.0.0.0.");
            if (publisher == null || publisher.Id == Guid.Empty || string.IsNullOrWhiteSpace(publisher.Prefix))
                throw new ArgumentException("Select a solution publisher.");

            // Creating a solution is separate from saving the API. Do not retry a lost create response.
            var response = (CreateResponse)service.Execute(new CreateRequest { Target = new Entity("solution")
            {
                ["uniquename"] = uniqueName, ["friendlyname"] = displayName, ["version"] = parsed.ToString(),
                ["publisherid"] = new EntityReference("publisher", publisher.Id)
            } });
            return new CustomApiSolutionOption
            {
                Id = response.id, UniqueName = uniqueName, Name = displayName, PublisherPrefix = publisher.Prefix
            };
        }

        private static QueryExpression Query(string table, params string[] columns) => new QueryExpression(table)
        {
            ColumnSet = new ColumnSet(columns), PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
        };
    }

    public sealed class CustomApiSolutionOption
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public string Name { get; set; }
        public string PublisherPrefix { get; set; }
    }

    public sealed class CustomApiPluginTypeOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string AssemblyName { get; set; }
    }

    public sealed class CustomApiPublisherOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Prefix { get; set; }
    }
}

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xrm.Sdk.PluginRegistration.Controls;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>The tables and privileges offered by the Custom API editor's pickers.</summary>
    public sealed class CustomApiMetadata
    {
        public IReadOnlyList<SearchChoice> Tables { get; private set; } = new SearchChoice[0];
        public IReadOnlyList<SearchChoice> Privileges { get; private set; } = new SearchChoice[0];
        public Exception TableError { get; private set; }
        public Exception PrivilegeError { get; private set; }
        public bool HasErrors => TableError != null || PrivilegeError != null;

        /// <summary>Loads both lists. A failure in one is recorded and leaves the other usable.</summary>
        public static CustomApiMetadata Load(IOrganizationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var metadata = new CustomApiMetadata();
            try { metadata.Tables = LoadTables(service); }
            catch (Exception error) { metadata.TableError = error; }
            try { metadata.Privileges = LoadPrivileges(service); }
            catch (Exception error) { metadata.PrivilegeError = error; }
            return metadata;
        }

        /// <summary>Only names and display names, without columns, so it stays quick on large environments.</summary>
        public static RetrieveMetadataChangesRequest TableRequest()
        {
            var criteria = new MetadataFilterExpression(LogicalOperator.And);
            criteria.Conditions.Add(new MetadataConditionExpression("IsPrivate", MetadataConditionOperator.Equals, false));
            return new RetrieveMetadataChangesRequest
            {
                Query = new EntityQueryExpression
                {
                    Properties = new MetadataPropertiesExpression("LogicalName", "DisplayName"),
                    Criteria = criteria
                }
            };
        }

        private static IReadOnlyList<SearchChoice> LoadTables(IOrganizationService service)
        {
            var response = (RetrieveMetadataChangesResponse)service.Execute(TableRequest());
            return response.EntityMetadata
                .Where(table => !string.IsNullOrEmpty(table.LogicalName))
                .Select(table => new SearchChoice(table.LogicalName, table.DisplayName?.UserLocalizedLabel?.Label))
                .OrderBy(choice => choice.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<SearchChoice> LoadPrivileges(IOrganizationService service)
        {
            var query = new QueryExpression("privilege")
            {
                ColumnSet = new ColumnSet("name"),
                PageInfo = new PagingInfo { Count = 5000, PageNumber = 1 }
            };
            query.AddOrder("name", OrderType.Ascending);
            return service.RetrieveMultipleAllPages(query).Entities
                .Select(privilege => privilege.GetAttributeValue<string>("name"))
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(name => new SearchChoice(name))
                .ToList();
        }
    }

    /// <summary>
    /// Loads <see cref="CustomApiMetadata"/> once for a connection and shares it between editors.
    /// A reload, or a previous load that failed, reads it again.
    /// </summary>
    public sealed class CustomApiMetadataCache
    {
        private readonly object gate = new object();
        private readonly Func<CustomApiMetadata> load;
        private Task<CustomApiMetadata> current;

        public CustomApiMetadataCache(IOrganizationService service)
        {
            Service = service ?? throw new ArgumentNullException(nameof(service));
            load = () => CustomApiMetadata.Load(service);
        }

        public CustomApiMetadataCache(Func<CustomApiMetadata> load)
        {
            this.load = load ?? throw new ArgumentNullException(nameof(load));
        }

        public IOrganizationService Service { get; }

        public Task<CustomApiMetadata> GetAsync(bool reload = false)
        {
            lock (gate)
            {
                bool failed = current != null && current.IsCompleted &&
                    (current.IsFaulted || current.IsCanceled || current.Result.HasErrors);
                if (reload || current == null || failed)
                    current = Task.Run(load);
                return current;
            }
        }
    }
}

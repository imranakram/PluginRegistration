using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Xrm.Sdk.PluginRegistration.Models;

namespace Xrm.Sdk.PluginRegistration.Helpers
{
    /// <summary>
    /// Persists Custom API definitions through the current organization's service.
    /// Call from background work; this class neither owns the connection nor updates UI state.
    /// </summary>
    public sealed class CustomApiService
    {
        private readonly IOrganizationService service;

        public CustomApiService(IOrganizationService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static bool IsSaveOutcomeUnknown(Exception error)
        {
            if (error == null) throw new ArgumentNullException(nameof(error));
            if (error is ArgumentException) return false;
            if (error is AggregateException aggregate)
                return aggregate.InnerExceptions.Count == 0 || aggregate.InnerExceptions.Any(IsSaveOutcomeUnknown);
            // Clients can wrap a server rejection. A typed fault still proves rejection;
            // an unrecognized exception does not prove that the write failed.
            for (var current = error; current != null; current = current.InnerException)
                if (current is FaultException<OrganizationServiceFault> || current is PreSaveException) return false;
            return true;
        }

        /// <summary>
        /// Checks the schema used by the editor. Access and transport faults deliberately
        /// propagate so the caller can distinguish an unavailable feature from a failed request.
        /// </summary>
        public bool IsSupported()
        {
            var required = new Dictionary<string, string[]>
            {
                [CustomApiMapping.ApiTable] = CustomApiMapping.ApiColumns,
                [CustomApiMapping.RequestTable] = CustomApiMapping.RequestColumns,
                [CustomApiMapping.ResponseTable] = CustomApiMapping.ResponseColumns
            };
            var query = new EntityQueryExpression
            {
                // Dataverse requires the parent Attributes property when an AttributeQuery is supplied.
                Properties = new MetadataPropertiesExpression("LogicalName", "Attributes"),
                Criteria = new MetadataFilterExpression(LogicalOperator.Or),
                AttributeQuery = new AttributeQueryExpression
                {
                    Properties = new MetadataPropertiesExpression("LogicalName")
                }
            };
            foreach (var table in required.Keys)
            {
                query.Criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, table));
            }

            var response = (RetrieveMetadataChangesResponse)service.Execute(new RetrieveMetadataChangesRequest { Query = query });
            return required.All(table => response.EntityMetadata.Any(entity =>
                entity.LogicalName == table.Key && entity.Attributes != null &&
                table.Value.All(column => entity.Attributes.Any(attribute => attribute.LogicalName == column))));
        }

        /// <summary>
        /// Lists definitions independently of SDK message filters and ordinary registered steps.
        /// Summaries cannot accidentally be saved as complete definitions with missing children.
        /// </summary>
        public IReadOnlyList<CustomApiSummary> List()
        {
            var query = new QueryExpression(CustomApiMapping.ApiTable)
            {
                ColumnSet = new ColumnSet("customapiid", "name", "uniquename", "sdkmessageid", "ismanaged"),
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            query.AddOrder("uniquename", OrderType.Ascending);
            return service.RetrieveMultipleAllPages(query).Entities.Select(entity => new CustomApiSummary
            {
                Id = entity.Id,
                Name = entity.GetAttributeValue<string>("name"),
                UniqueName = entity.GetAttributeValue<string>("uniquename"),
                SdkMessageId = entity.GetAttributeValue<EntityReference>("sdkmessageid")?.Id,
                IsManaged = entity.GetAttributeValue<bool>("ismanaged")
            }).ToList();
        }

        public CustomApiDefinition Retrieve(Guid id)
        {
            RequireId(id);
            var definition = CustomApiMapping.ReadApi(service.Retrieve(CustomApiMapping.ApiTable, id, new ColumnSet(CustomApiMapping.ApiColumns)));
            definition.RequestParameters.AddRange(RetrieveChildren(CustomApiMapping.RequestTable, CustomApiMapping.RequestColumns, id)
                .Select(CustomApiMapping.ReadRequest));
            definition.ResponseProperties.AddRange(RetrieveChildren(CustomApiMapping.ResponseTable, CustomApiMapping.ResponseColumns, id)
                .Select(CustomApiMapping.ReadResponse));
            return definition;
        }

        /// <summary>
        /// Creates the API and its children in one request. The returned ID is the saved API;
        /// retrieve it separately so a failed refresh cannot be mistaken for a failed create.
        /// </summary>
        public Guid Create(CustomApiDefinition definition, string solutionUniqueName)
        {
            Validate(definition);
            if (string.IsNullOrWhiteSpace(solutionUniqueName))
            {
                throw new ArgumentException("Select an unmanaged solution.", nameof(solutionUniqueName));
            }

            string prefix;
            try { prefix = RetrievePublisherPrefix(solutionUniqueName); }
            catch (Exception error)
            {
                // Even a transport failure during this read cannot have submitted the create.
                throw new PreSaveException(error.Message, error);
            }
            Validate(definition, publisherPrefix: prefix);
            var target = CustomApiMapping.CreateApi(definition);
            AddRelated(target, CustomApiMapping.RequestRelationship, definition.RequestParameters
                .Select(parameter => CustomApiMapping.CreateParameter(parameter, CustomApiMapping.RequestTable)));
            AddRelated(target, CustomApiMapping.ResponseRelationship, definition.ResponseProperties
                .Select(property => CustomApiMapping.CreateParameter(property, CustomApiMapping.ResponseTable)));

            var request = new CreateRequest { Target = target };
            request["SolutionUniqueName"] = solutionUniqueName;
            return ((CreateResponse)service.Execute(request)).id;
        }

        /// <summary>
        /// Saves changes against the complete snapshot originally loaded by the editor.
        /// The caller must keep that snapshot separate from the editable draft and reload after success.
        /// </summary>
        public bool Update(CustomApiDefinition original, CustomApiDefinition definition)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            RequireId(original.Id);
            Validate(definition, original);

            var requests = new OrganizationRequestCollection();
            AddUpdate(requests, CustomApiMapping.Changes(CustomApiMapping.MutableApi(original), CustomApiMapping.MutableApi(definition)));
            AddChildChanges(requests, original.RequestParameters, definition.RequestParameters, CustomApiMapping.RequestTable, original.Id);
            AddChildChanges(requests, original.ResponseProperties, definition.ResponseProperties, CustomApiMapping.ResponseTable, original.Id);
            if (requests.Count == 0) return false;

            // ExecuteMultiple commits each request separately. A transaction keeps a failed
            // child edit from leaving a partially updated API signature on the server.
            service.Execute(new ExecuteTransactionRequest { Requests = requests, ReturnResponses = false });
            return true;
        }

        public void Delete(Guid id)
        {
            RequireId(id);
            var entity = service.Retrieve(CustomApiMapping.ApiTable, id, new ColumnSet("ismanaged"));
            if (entity.GetAttributeValue<bool>("ismanaged"))
            {
                throw new InvalidOperationException("A managed Custom API must be removed through its managed solution.");
            }

            // Dataverse owns the generated message and child dependencies. Do not reproduce
            // the assembly unregistration cascade or delete the SDK message directly.
            service.Delete(CustomApiMapping.ApiTable, id);
        }

        public void AddToSolution(Guid id, string solutionUniqueName)
        {
            RequireId(id);
            if (string.IsNullOrWhiteSpace(solutionUniqueName))
                throw new ArgumentException("Select a solution.", nameof(solutionUniqueName));

            // Resolve the complete contract before adding anything to the solution.
            var components = new List<EntityReference> { new EntityReference(CustomApiMapping.ApiTable, id) };
            foreach (var table in new[] { CustomApiMapping.RequestTable, CustomApiMapping.ResponseTable })
                components.AddRange(RetrieveChildren(table, new[] { table + "id" }, id).Select(child => child.ToEntityReference()));
            var types = GetSolutionComponentTypes(components.Select(component => component.LogicalName).Distinct().ToArray());
            var requests = new OrganizationRequestCollection();
            foreach (var component in components)
                requests.Add(new AddSolutionComponentRequest
                {
                    AddRequiredComponents = false,
                    ComponentId = component.Id,
                    ComponentType = types[component.LogicalName],
                    SolutionUniqueName = solutionUniqueName
                });

            // Keep solution membership atomic if adding any part of the contract fails.
            service.Execute(new ExecuteTransactionRequest { Requests = requests, ReturnResponses = false });
        }

        private IReadOnlyDictionary<string, int> GetSolutionComponentTypes(params string[] tables)
        {
            // Discover component codes from the organization rather than using table IDs.
            var query = new QueryExpression("solutioncomponentdefinition")
            {
                ColumnSet = new ColumnSet("primaryentityname", "solutioncomponenttype")
            };
            query.Criteria.AddCondition("primaryentityname", ConditionOperator.In, tables.Cast<object>().ToArray());
            var types = service.RetrieveMultiple(query).Entities.ToDictionary(
                definition => definition.GetAttributeValue<string>("primaryentityname"),
                definition => definition.GetAttributeValue<int>("solutioncomponenttype"));
            var missing = tables.FirstOrDefault(table => !types.ContainsKey(table));
            if (missing != null)
                throw new InvalidOperationException($"Unable to find the solution component type for table {missing}.");
            return types;
        }

        private sealed class PreSaveException : InvalidOperationException
        {
            public PreSaveException(string message, Exception innerException) : base(message, innerException) { }
        }

        private IEnumerable<Entity> RetrieveChildren(string table, string[] columns, Guid apiId)
        {
            var query = new QueryExpression(table)
            {
                ColumnSet = new ColumnSet(columns),
                PageInfo = new PagingInfo { PageNumber = 1, Count = 5000 }
            };
            query.Criteria.AddCondition("customapiid", ConditionOperator.Equal, apiId);
            query.AddOrder("uniquename", OrderType.Ascending);
            return service.RetrieveMultipleAllPages(query).Entities;
        }

        private string RetrievePublisherPrefix(string solutionUniqueName)
        {
            var query = new QueryExpression("solution") { ColumnSet = new ColumnSet("uniquename") };
            query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);
            query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
            var publisher = query.AddLink("publisher", "publisherid", "publisherid");
            publisher.EntityAlias = "publisher";
            publisher.Columns = new ColumnSet("customizationprefix");
            var solutions = service.RetrieveMultiple(query).Entities;
            var prefix = solutions.Count == 1
                ? solutions[0].GetAttributeValue<AliasedValue>("publisher.customizationprefix")?.Value as string
                : null;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new InvalidOperationException("The unmanaged solution or its publisher prefix could not be found. Refresh the solution list.");
            }
            return prefix;
        }

        private static void AddRelated(Entity target, string relationship, IEnumerable<Entity> children)
        {
            var collection = new EntityCollection(children.ToList());
            if (collection.Entities.Count > 0)
            {
                target.RelatedEntities.Add(new Relationship(relationship), collection);
            }
        }

        private static void AddChildChanges<T>(OrganizationRequestCollection requests, IList<T> original, IList<T> updated,
            string table, Guid apiId) where T : CustomApiParameter
        {
            var previousById = original.ToDictionary(parameter => parameter.Id);
            var retainedIds = new HashSet<Guid>(updated.Where(parameter => parameter.Id != Guid.Empty).Select(parameter => parameter.Id));
            // Remove old names before inserting replacements with those names. All operations
            // remain in the same transaction, including an intentional delete-and-recreate.
            foreach (var removed in original.Where(parameter => !retainedIds.Contains(parameter.Id)))
            {
                requests.Add(new DeleteRequest { Target = new EntityReference(table, removed.Id) });
            }
            foreach (var parameter in updated)
            {
                if (parameter.Id == Guid.Empty)
                {
                    var target = CustomApiMapping.CreateParameter(parameter, table);
                    target["customapiid"] = new EntityReference(CustomApiMapping.ApiTable, apiId);
                    requests.Add(new CreateRequest { Target = target });
                }
                else
                {
                    AddUpdate(requests, CustomApiMapping.Changes(
                        CustomApiMapping.MutableParameter(previousById[parameter.Id], table),
                        CustomApiMapping.MutableParameter(parameter, table)));
                }
            }
        }

        private static void AddUpdate(OrganizationRequestCollection requests, Entity changes)
        {
            if (changes.Attributes.Count > 0) requests.Add(new UpdateRequest { Target = changes });
        }

        private static void Validate(CustomApiDefinition definition, CustomApiDefinition original = null, string publisherPrefix = null)
        {
            var errors = CustomApiValidator.Validate(definition, original, publisherPrefix);
            if (errors.Count > 0) throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(definition));
        }

        private static void RequireId(Guid id)
        {
            if (id == Guid.Empty) throw new ArgumentException("A saved Custom API ID is required.", nameof(id));
        }
    }
}
